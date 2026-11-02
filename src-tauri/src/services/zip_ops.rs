use super::security::{safe_extract_zip, sanitize_file_name, validate_path_basics};
use std::fs::File;
use std::io::{Read, Write};
use std::path::Path;
use zip::write::SimpleFileOptions;

pub fn create_zip_archive(files: &[String], output_zip_path: &str) -> Result<(), String> {
    validate_path_basics(output_zip_path)?;

    if let Some(parent) = Path::new(output_zip_path).parent() {
        if !parent.exists() {
            std::fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando directorio destino: {}", e))?;
        }
    }

    if Path::new(output_zip_path).exists() {
        let _ = std::fs::remove_file(output_zip_path);
    }

    let file = File::create(output_zip_path)
        .map_err(|e| format!("No se pudo crear el archivo ZIP: {}", e))?;
    let mut zip = zip::ZipWriter::new(file);

    let options = SimpleFileOptions::default()
        .compression_method(zip::CompressionMethod::Deflated)
        .unix_permissions(0o755);

    for file_path in files {
        validate_path_basics(file_path)?;
        let p = Path::new(file_path);
        if !p.exists() {
            continue;
        }

        let raw_name = p
            .file_name()
            .and_then(|n| n.to_str())
            .unwrap_or("archivo");
        let safe_name = sanitize_file_name(raw_name);

        zip.start_file(safe_name, options)
            .map_err(|e| format!("Error agregando archivo a ZIP: {}", e))?;

        let mut source = File::open(p)
            .map_err(|e| format!("Error abriendo archivo {}: {}", file_path, e))?;
        let mut buffer = [0u8; 8192];
        loop {
            let n = source
                .read(&mut buffer)
                .map_err(|e| format!("Error leyendo {}: {}", file_path, e))?;
            if n == 0 {
                break;
            }
            zip.write_all(&buffer[..n])
                .map_err(|e| format!("Error escribiendo en ZIP: {}", e))?;
        }
    }

    zip.finish()
        .map_err(|e| format!("Error finalizando archivo ZIP: {}", e))?;

    Ok(())
}

pub fn extract_zip_archive(zip_path: &str, destination_dir: &str) -> Result<Vec<String>, String> {
    safe_extract_zip(zip_path, destination_dir)
}

