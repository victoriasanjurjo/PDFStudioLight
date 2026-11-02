use std::collections::HashSet;
use std::fs::File;
use std::io::Read;
use std::path::Path;

pub const MAX_PDF_SIZE_BYTES: u64 = 500 * 1024 * 1024; // 500 MB
pub const MAX_IMAGE_SIZE_BYTES: u64 = 100 * 1024 * 1024; // 100 MB
pub const MAX_ZIP_UNCOMPRESSED_BYTES: u64 = 1024 * 1024 * 1024; // 1 GB
pub const MAX_ZIP_ENTRY_COUNT: usize = 5000;
pub const MAX_PAGE_GROUPS: usize = 2000;

fn is_reserved_name(name: &str) -> bool {
    let reserved: HashSet<&str> = [
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7",
        "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    ]
    .iter()
    .cloned()
    .collect();

    let stem = Path::new(name)
        .file_stem()
        .and_then(|s| s.to_str())
        .unwrap_or(name)
        .to_uppercase();

    reserved.contains(stem.as_str())
}

pub fn validate_path_basics(path_str: &str) -> Result<(), String> {
    if path_str.trim().is_empty() {
        return Err("La ruta especificada no puede estar vacía.".to_string());
    }
    if path_str.contains('\0') {
        return Err("La ruta contiene caracteres no válidos o potencialmente maliciosos.".to_string());
    }

    let p = Path::new(path_str);
    if let Some(file_name) = p.file_name().and_then(|f| f.to_str()) {
        if is_reserved_name(file_name) {
            return Err("Nombre de archivo reservado por el sistema operativo.".to_string());
        }
    }

    Ok(())
}

pub fn validate_pdf_file(path_str: &str, must_exist: bool) -> Result<(), String> {
    validate_path_basics(path_str)?;

    let path = Path::new(path_str);
    let ext = path
        .extension()
        .and_then(|e| e.to_str())
        .map(|e| format!(".{}", e.to_lowercase()))
        .unwrap_or_default();

    if ext != ".pdf" {
        return Err(format!("Extensión no permitida para archivo PDF: '{}'.", ext));
    }

    if must_exist {
        if !path.exists() {
            return Err(format!("El archivo PDF especificado no existe: {}", path_str));
        }
        let meta = std::fs::metadata(path)
            .map_err(|e| format!("Error al inspeccionar archivo PDF: {}", e))?;
        if meta.len() > MAX_PDF_SIZE_BYTES {
            return Err(format!(
                "El archivo PDF supera el límite de seguridad permitido ({} MB).",
                MAX_PDF_SIZE_BYTES / (1024 * 1024)
            ));
        }
    }

    Ok(())
}

pub fn validate_image_file(path_str: &str, must_exist: bool) -> Result<(), String> {
    validate_path_basics(path_str)?;

    let path = Path::new(path_str);
    let ext = path
        .extension()
        .and_then(|e| e.to_str())
        .map(|e| format!(".{}", e.to_lowercase()))
        .unwrap_or_default();

    let allowed: HashSet<&str> = [".png", ".jpg", ".jpeg", ".bmp"].iter().cloned().collect();
    if !allowed.contains(ext.as_str()) {
        return Err(format!(
            "Formato de imagen no permitido: '{}'. Formatos soportados: PNG, JPG, JPEG, BMP.",
            ext
        ));
    }

    if must_exist {
        if !path.exists() {
            return Err(format!("El archivo de imagen especificado no existe: {}", path_str));
        }
        let meta = std::fs::metadata(path)
            .map_err(|e| format!("Error al inspeccionar imagen: {}", e))?;
        if meta.len() > MAX_IMAGE_SIZE_BYTES {
            return Err(format!(
                "La imagen supera el límite de seguridad de tamaño ({} MB).",
                MAX_IMAGE_SIZE_BYTES / (1024 * 1024)
            ));
        }
    }

    Ok(())
}

pub fn sanitize_file_name(name: &str) -> String {
    let invalid_chars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'];
    let mut cleaned: String = name.chars().filter(|c| !invalid_chars.contains(c)).collect();
    cleaned = cleaned.trim().to_string();
    if cleaned.is_empty() {
        return "documento".to_string();
    }
    if is_reserved_name(&cleaned) {
        cleaned = format!("safe_{}", cleaned);
    }
    cleaned
}

pub fn safe_extract_zip(zip_path: &str, dest_dir: &str) -> Result<Vec<String>, String> {
    validate_path_basics(zip_path)?;
    validate_path_basics(dest_dir)?;

    let target_dir = Path::new(dest_dir);
    if !target_dir.exists() {
        std::fs::create_dir_all(target_dir)
            .map_err(|e| format!("Error al crear carpeta de extracción: {}", e))?;
    }
    let canonical_dest = target_dir
        .canonicalize()
        .map_err(|e| format!("Error al canonizar destino: {}", e))?;

    let file = File::open(zip_path)
        .map_err(|e| format!("No se pudo abrir el archivo ZIP: {}", e))?;
    let mut archive = zip::ZipArchive::new(file)
        .map_err(|e| format!("Formato ZIP inválido: {}", e))?;

    if archive.len() > MAX_ZIP_ENTRY_COUNT {
        return Err(format!(
            "El archivo ZIP excede el límite máximo de entradas ({})",
            MAX_ZIP_ENTRY_COUNT
        ));
    }

    let mut total_uncompressed: u64 = 0;
    let mut extracted_paths = Vec::new();

    for i in 0..archive.len() {
        let mut entry = archive
            .by_index(i)
            .map_err(|e| format!("Error leyendo entrada ZIP: {}", e))?;

        let enclosed = match entry.enclosed_name() {
            Some(path) => path.to_owned(),
            None => {
                return Err("Ruta de entrada maliciosa detectada en ZIP (Zip Slip).".to_string());
            }
        };

        let outpath = canonical_dest.join(enclosed);
        if !outpath.starts_with(&canonical_dest) {
            return Err("Intento de escape de directorio detectado en ZIP (Zip Slip).".to_string());
        }

        if entry.is_dir() {
            std::fs::create_dir_all(&outpath)
                .map_err(|e| format!("Error al crear directorio: {}", e))?;
        } else {
            if let Some(parent) = outpath.parent() {
                if !parent.exists() {
                    std::fs::create_dir_all(parent)
                        .map_err(|e| format!("Error creando directorio padre: {}", e))?;
                }
            }

            let mut outfile = File::create(&outpath)
                .map_err(|e| format!("Error creando archivo de salida: {}", e))?;
            let mut buffer = [0u8; 8192];
            loop {
                let n = entry
                    .read(&mut buffer)
                    .map_err(|e| format!("Error descomprimiendo entrada: {}", e))?;
                if n == 0 {
                    break;
                }
                total_uncompressed += n as u64;
                if total_uncompressed > MAX_ZIP_UNCOMPRESSED_BYTES {
                    return Err(
                        "Se superó el límite de descompresión seguro (posible Zip Bomb).".to_string(),
                    );
                }
                std::io::Write::write_all(&mut outfile, &buffer[..n])
                    .map_err(|e| format!("Error escribiendo datos descomprimidos: {}", e))?;
            }
            extracted_paths.push(outpath.to_string_lossy().to_string());
        }
    }

    Ok(extracted_paths)
}
