use super::security::{
    sanitize_file_name, validate_image_file, validate_path_basics, validate_pdf_file,
    MAX_PAGE_GROUPS,
};
use crate::models::CompressionResult;
use lopdf::content::{Content, Operation};
use lopdf::{dictionary, Dictionary, Document, Object, Stream};
use std::collections::HashMap;
use std::path::Path;

pub fn get_page_count(file_path: &str) -> Result<u32, String> {
    validate_pdf_file(file_path, true)?;
    let doc = Document::load(file_path)
        .map_err(|e| format!("Error abriendo documento PDF: {}", e))?;
    Ok(doc.get_pages().len() as u32)
}

pub fn parse_page_ranges(expression: &str, max_page: u32) -> Vec<Vec<u32>> {
    let mut result = Vec::new();
    if expression.trim().is_empty() {
        return result;
    }

    let parts: Vec<&str> = expression
        .split(|c| c == ',' || c == ';')
        .map(|s| s.trim())
        .filter(|s| !s.is_empty())
        .collect();

    for part in parts {
        if result.len() >= MAX_PAGE_GROUPS {
            break;
        }

        if part.contains('-') {
            let sub_parts: Vec<&str> = part.split('-').map(|s| s.trim()).collect();
            if sub_parts.len() == 2 {
                if let (Ok(start), Ok(end)) = (sub_parts[0].parse::<u32>(), sub_parts[1].parse::<u32>()) {
                    let s = start.max(1).min(max_page);
                    let e = end.max(1).min(max_page);
                    if s <= e {
                        let group: Vec<u32> = (s..=e).collect();
                        if !group.is_empty() {
                            result.push(group);
                        }
                    }
                }
            }
        } else if let Ok(single) = part.parse::<u32>() {
            if single >= 1 && single <= max_page {
                result.push(vec![single]);
            }
        }
    }

    result
}

pub fn merge_pdfs(input_files: &[String], output_path: &str) -> Result<u32, String> {
    validate_pdf_file(output_path, false)?;

    if input_files.len() < 2 {
        return Err("Se requieren al menos 2 archivos PDF para realizar la unión.".to_string());
    }

    for f in input_files {
        validate_pdf_file(f, true)?;
    }

    if let Some(parent) = Path::new(output_path).parent() {
        if !parent.exists() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando directorio destino: {}", e))?;
        }
    }

    // Cargar documentos
    let mut documents = Vec::new();
    for f in input_files {
        let doc = Document::load(f)
            .map_err(|e| format!("Error al cargar '{}': {}", f, e))?;
        documents.push(doc);
    }

    // Inicializar documento combinado basado en el primero
    let mut first_doc = documents.remove(0);
    let mut total_pages = first_doc.get_pages().len() as u32;

    for doc in documents {
        let page_count = doc.get_pages().len() as u32;
        let mut doc_to_merge = doc;
        
        // Re-enumerar IDs de objetos para evitar colisiones
        doc_to_merge.renumber_objects_with(first_doc.max_id + 1);

        // Copiar páginas y objetos
        let pages = doc_to_merge.get_pages();
        for (_, page_id) in pages {
            first_doc.objects.insert(page_id, doc_to_merge.objects.get(&page_id).unwrap().clone());
        }

        for (id, object) in doc_to_merge.objects {
            first_doc.objects.entry(id).or_insert(object);
        }

        first_doc.max_id += doc_to_merge.max_id + 1;
        total_pages += page_count;
    }

    first_doc.save(output_path)
        .map_err(|e| format!("Error al guardar PDF combinado: {}", e))?;

    Ok(total_pages)
}

pub fn split_pdf(
    input_path: &str,
    output_directory: &str,
    ranges_expr: Option<&str>,
    split_all: bool,
) -> Result<Vec<String>, String> {
    validate_pdf_file(input_path, true)?;
    validate_path_basics(output_directory)?;

    let out_dir = Path::new(output_directory);
    if !out_dir.exists() {
        std::fs::create_dir_all(out_dir)
            .map_err(|e| format!("Error creando carpeta destino: {}", e))?;
    }

    let raw_stem = Path::new(input_path)
        .file_stem()
        .and_then(|s| s.to_str())
        .unwrap_or("documento");
    let safe_stem = sanitize_file_name(raw_stem);

    let doc = Document::load(input_path)
        .map_err(|e| format!("Error abriendo PDF para división: {}", e))?;
    let total_pages = doc.get_pages().len() as u32;

    if total_pages == 0 {
        return Err("El documento no contiene páginas.".to_string());
    }

    let mut generated = Vec::new();

    if split_all {
        for page_idx in 1..=total_pages {
            let mut single_doc = doc.clone();
            let all_pages: Vec<u32> = (1..=total_pages).collect();
            let to_delete: Vec<u32> = all_pages.into_iter().filter(|p| *p != page_idx).collect();
            single_doc.delete_pages(&to_delete);

            let out_file = out_dir.join(format!("{}_pag_{:03}.pdf", safe_stem, page_idx));
            let out_str = out_file.to_string_lossy().to_string();
            single_doc.save(&out_file)
                .map_err(|e| format!("Error guardando página {}: {}", page_idx, e))?;
            generated.push(out_str);
        }
    } else {
        let expr = ranges_expr.unwrap_or("1");
        let groups = parse_page_ranges(expr, total_pages);
        if groups.is_empty() {
            return Err("No se especificaron rangos de páginas válidos.".to_string());
        }

        for (idx, group) in groups.iter().enumerate() {
            let mut sub_doc = doc.clone();
            let all_pages: Vec<u32> = (1..=total_pages).collect();
            let to_delete: Vec<u32> = all_pages
                .into_iter()
                .filter(|p| !group.contains(p))
                .collect();
            sub_doc.delete_pages(&to_delete);

            let label = if group.len() == 1 {
                format!("pag_{}", group[0])
            } else {
                format!("pags_{}-{}", group.first().unwrap(), group.last().unwrap())
            };

            let out_file = out_dir.join(format!("{}_parte_{}_{}.pdf", safe_stem, idx + 1, label));
            let out_str = out_file.to_string_lossy().to_string();
            sub_doc.save(&out_file)
                .map_err(|e| format!("Error guardando fragmento {}: {}", idx + 1, e))?;
            generated.push(out_str);
        }
    }

    Ok(generated)
}

pub fn save_edited_pdf(
    input_path: &str,
    output_path: &str,
    pages_to_keep: &[u32],
    rotations: &HashMap<u32, i32>,
) -> Result<(), String> {
    validate_pdf_file(input_path, true)?;
    validate_pdf_file(output_path, false)?;

    if pages_to_keep.is_empty() {
        return Err("Debe conservar al menos una página en el documento.".to_string());
    }

    if let Some(parent) = Path::new(output_path).parent() {
        if !parent.exists() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando carpeta destino: {}", e))?;
        }
    }

    let mut doc = Document::load(input_path)
        .map_err(|e| format!("Error cargando PDF para editar: {}", e))?;
    let total_pages = doc.get_pages().len() as u32;

    // Eliminar las páginas que no están en pages_to_keep
    let all_pages: Vec<u32> = (1..=total_pages).collect();
    let to_delete: Vec<u32> = all_pages
        .into_iter()
        .filter(|p| !pages_to_keep.contains(p))
        .collect();
    doc.delete_pages(&to_delete);

    // Aplicar rotaciones
    let remaining_pages = doc.get_pages();
    for (&orig_page_num, &angle) in rotations {
        if let Some(&page_id) = remaining_pages.get(&orig_page_num) {
            if let Ok(page_dict) = doc.get_object_mut(page_id).and_then(|o| o.as_dict_mut()) {
                let current_rot = page_dict
                    .get(b"Rotate")
                    .and_then(|r| r.as_i64())
                    .unwrap_or(0) as i32;
                let new_rot = (current_rot + angle).rem_euclid(360);
                page_dict.set("Rotate", Object::Integer(new_rot as i64));
            }
        }
    }

    doc.save(output_path)
        .map_err(|e| format!("Error guardando PDF editado: {}", e))?;

    Ok(())
}

pub fn compress_pdf(input_path: &str, output_path: &str) -> Result<CompressionResult, String> {
    validate_pdf_file(input_path, true)?;
    validate_pdf_file(output_path, false)?;

    let orig_meta = std::fs::metadata(input_path)
        .map_err(|e| format!("Error leyendo metadatos: {}", e))?;
    let original_size_bytes = orig_meta.len();

    if let Some(parent) = Path::new(output_path).parent() {
        if !parent.exists() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando carpeta destino: {}", e))?;
        }
    }

    let mut doc = Document::load(input_path)
        .map_err(|e| format!("Error cargando PDF para compresión: {}", e))?;

    // Optimizar flujos de compresión flate
    doc.compress();

    doc.save(output_path)
        .map_err(|e| format!("Error guardando PDF comprimido: {}", e))?;

    let compressed_size_bytes = std::fs::metadata(output_path)
        .map(|m| m.len())
        .unwrap_or(original_size_bytes);

    let saved_pct = if original_size_bytes > 0 && compressed_size_bytes < original_size_bytes {
        (1.0 - (compressed_size_bytes as f64 / original_size_bytes as f64)) * 100.0
    } else {
        0.0
    };

    Ok(CompressionResult {
        original_size_bytes,
        compressed_size_bytes,
        saved_percentage: saved_pct,
        output_path: output_path.to_string(),
    })
}

pub fn convert_images_to_pdf(image_paths: &[String], output_path: &str) -> Result<(), String> {
    validate_pdf_file(output_path, false)?;

    if image_paths.is_empty() {
        return Err("Se requiere al menos una imagen para generar el PDF.".to_string());
    }

    for img in image_paths {
        validate_image_file(img, true)?;
    }

    if let Some(parent) = Path::new(output_path).parent() {
        if !parent.exists() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando carpeta de salida: {}", e))?;
        }
    }

    let mut doc = Document::with_version("1.5");
    let pages_id = doc.new_object_id();
    let mut page_ids = Vec::new();

    for img_path in image_paths {
        let img = image::open(img_path)
            .map_err(|e| format!("Error abriendo imagen '{}': {}", img_path, e))?
            .to_rgb8();
        let (width, height) = img.dimensions();

        // Convertir imagen RGB a JPEG comprimido en memoria para incrustación directa
        let mut jpeg_bytes = Vec::new();
        let mut cursor = std::io::Cursor::new(&mut jpeg_bytes);
        img.write_to(&mut cursor, image::ImageFormat::Jpeg)
            .map_err(|e| format!("Error codificando imagen: {}", e))?;

        let image_stream = Stream::new(
            lopdf::dictionary! {
                "Type" => "XObject",
                "Subtype" => "Image",
                "Width" => width as i64,
                "Height" => height as i64,
                "ColorSpace" => "DeviceRGB",
                "BitsPerComponent" => 8,
                "Filter" => "DCTDecode",
            },
            jpeg_bytes,
        );
        let img_id = doc.add_object(image_stream);

        // Operaciones de dibujo en el contenido de la página: escala y pinta la imagen
        let content = Content {
            operations: vec![
                Operation::new("q", vec![]),
                Operation::new(
                    "cm",
                    vec![
                        (width as f64).into(),
                        0.into(),
                        0.into(),
                        (height as f64).into(),
                        0.into(),
                        0.into(),
                    ],
                ),
                Operation::new("Do", vec!["/Im0".into()]),
                Operation::new("Q", vec![]),
            ],
        };
        let content_id = doc.add_object(Stream::new(dictionary! {}, content.encode().unwrap()));

        let page_dict = dictionary! {
            "Type" => "Page",
            "Parent" => pages_id,
            "MediaBox" => vec![0.into(), 0.into(), (width as f64).into(), (height as f64).into()],
            "Contents" => content_id,
            "Resources" => dictionary! {
                "XObject" => dictionary! {
                    "Im0" => img_id,
                },
            },
        };
        let page_id = doc.add_object(page_dict);
        page_ids.push(page_id);
    }

    let pages_dict = dictionary! {
        "Type" => "Pages",
        "Kids" => page_ids.iter().map(|&id| id.into()).collect::<Vec<Object>>(),
        "Count" => page_ids.len() as i64,
    };
    doc.objects.insert(pages_id, Object::Dictionary(pages_dict));

    let catalog_id = doc.add_object(dictionary! {
        "Type" => "Catalog",
        "Pages" => pages_id,
    });
    doc.trailer.set("Root", catalog_id);

    doc.save(output_path)
        .map_err(|e| format!("Error guardando PDF de imágenes: {}", e))?;

    Ok(())
}

pub fn add_image_or_signature_to_pdf(
    input_pdf: &str,
    image_input: &str,
    output_path: &str,
    page_num: u32,
    x: f64,
    y: f64,
    width: f64,
    height: f64,
) -> Result<(), String> {
    validate_pdf_file(input_pdf, true)?;
    validate_pdf_file(output_path, false)?;

    if width <= 0.0 || height <= 0.0 || width > 10000.0 || height > 10000.0 {
        return Err("Dimensiones de la imagen fuera del rango seguro.".to_string());
    }

    // Decodificar imagen ya sea desde ruta de archivo o desde data URL (Base64)
    let img_rgba = if image_input.starts_with("data:image/") {
        let base64_str = image_input
            .split(',')
            .nth(1)
            .ok_or_else(|| "Formato de firma Base64 inválido".to_string())?;
        use base64::Engine;
        let bytes = base64::engine::general_purpose::STANDARD
            .decode(base64_str.trim())
            .map_err(|e| format!("Error decodificando imagen Base64: {}", e))?;
        image::load_from_memory(&bytes)
            .map_err(|e| format!("Error cargando imagen desde Base64: {}", e))?
            .to_rgba8()
    } else {
        validate_image_file(image_input, true)?;
        image::open(image_input)
            .map_err(|e| format!("Error abriendo imagen '{}': {}", image_input, e))?
            .to_rgba8()
    };

    let (img_w, img_h) = img_rgba.dimensions();

    // Separar canal RGB y máscara Alfa para transparencia perfecta
    let mut rgb_data = Vec::with_capacity((img_w * img_h * 3) as usize);
    let mut alpha_data = Vec::with_capacity((img_w * img_h) as usize);
    let mut has_transparency = false;

    for pixel in img_rgba.pixels() {
        rgb_data.push(pixel[0]);
        rgb_data.push(pixel[1]);
        rgb_data.push(pixel[2]);
        alpha_data.push(pixel[3]);
        if pixel[3] < 255 {
            has_transparency = true;
        }
    }

    let mut doc = Document::load(input_pdf)
        .map_err(|e| format!("Error abriendo PDF: {}", e))?;
    let pages = doc.get_pages();
    let page_id = *pages
        .get(&page_num)
        .ok_or_else(|| format!("Número de página fuera de rango: {}", page_num))?;

    // Crear objeto de imagen
    let mut img_dict = lopdf::dictionary! {
        "Type" => "XObject",
        "Subtype" => "Image",
        "Width" => img_w as i64,
        "Height" => img_h as i64,
        "ColorSpace" => "DeviceRGB",
        "BitsPerComponent" => 8,
    };

    if has_transparency {
        let mask_stream = Stream::new(
            lopdf::dictionary! {
                "Type" => "XObject",
                "Subtype" => "Image",
                "Width" => img_w as i64,
                "Height" => img_h as i64,
                "ColorSpace" => "DeviceGray",
                "BitsPerComponent" => 8,
            },
            alpha_data,
        );
        let mask_id = doc.add_object(mask_stream);
        img_dict.set("SMask", mask_id);
    }

    let img_stream = Stream::new(img_dict, rgb_data);
    let img_id = doc.add_object(img_stream);

    // Obtener identificador único para el recurso
    let img_name = format!("ImgStamp{}", doc.max_id);

    // Adjuntar la imagen al diccionario de recursos de la página
    let page_obj = doc.get_object_mut(page_id)
        .and_then(|o| o.as_dict_mut())
        .map_err(|_| "Error accediendo al diccionario de página")?;

    if !page_obj.has(b"Resources") {
        page_obj.set("Resources", Dictionary::new());
    }

    let resources = page_obj
        .get_mut(b"Resources")
        .and_then(|r| r.as_dict_mut())
        .map_err(|_| "Error obteniendo recursos de página")?;

    if !resources.has(b"XObject") {
        resources.set("XObject", Dictionary::new());
    }

    let xobjects = resources
        .get_mut(b"XObject")
        .and_then(|x| x.as_dict_mut())
        .map_err(|_| "Error obteniendo XObjects")?;

    xobjects.set(img_name.as_bytes().to_vec(), img_id);

    // Agregar contenido al final de la página (append)
    let stamp_ops = Content {
        operations: vec![
            Operation::new("q", vec![]),
            Operation::new(
                "cm",
                vec![
                    width.into(),
                    0.into(),
                    0.into(),
                    height.into(),
                    x.into(),
                    y.into(),
                ],
            ),
            Operation::new("Do", vec![format!("/{}", img_name).into()]),
            Operation::new("Q", vec![]),
        ],
    };

    let stamp_content_id = doc.add_object(Stream::new(
        dictionary! {},
        stamp_ops.encode().unwrap(),
    ));

    // Agregar a Contents de la página
    let page_obj = doc.get_object_mut(page_id)
        .and_then(|o| o.as_dict_mut())
        .unwrap();

    match page_obj.get_mut(b"Contents") {
        Ok(Object::Array(ref mut arr)) => {
            arr.push(stamp_content_id.into());
        }
        Ok(Object::Reference(existing_id)) => {
            let existing_copy = *existing_id;
            page_obj.set("Contents", Object::Array(vec![existing_copy.into(), stamp_content_id.into()]));
        }
        _ => {
            page_obj.set("Contents", stamp_content_id);
        }
    }

    if let Some(parent) = Path::new(output_path).parent() {
        if !parent.exists() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando carpeta destino: {}", e))?;
        }
    }

    doc.save(output_path)
        .map_err(|e| format!("Error guardando PDF con firma/imagen: {}", e))?;

    Ok(())
}
