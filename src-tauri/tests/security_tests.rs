use pdf_studio_light_lib::services::pdf_ops::parse_page_ranges;
use pdf_studio_light_lib::services::security::{
    safe_extract_zip, sanitize_file_name, validate_image_file, validate_pdf_file,
};
use std::fs::File;
use std::io::Write;
use zip::write::SimpleFileOptions;

#[test]
fn test_validate_pdf_file_rejects_non_pdf_extensions() {
    let fake_path = "malware.exe";
    let res = validate_pdf_file(fake_path, false);
    assert!(res.is_err());
    assert!(res.unwrap_err().contains("Extensión no permitida"));
}

#[test]
fn test_validate_pdf_file_rejects_reserved_device_names() {
    let con_path = "CON.pdf";
    let res = validate_pdf_file(con_path, false);
    assert!(res.is_err());
    assert!(res.unwrap_err().contains("reservado"));
}

#[test]
fn test_validate_image_file_rejects_executable() {
    let script_path = "trojan.bat";
    let res = validate_image_file(script_path, false);
    assert!(res.is_err());
    assert!(res.unwrap_err().contains("Formato de imagen no permitido"));
}

#[test]
fn test_sanitize_file_name_removes_illegal_characters() {
    let malicious = "test/..\\:*?\"<>|file.pdf";
    let sanitized = sanitize_file_name(malicious);

    for ch in &['/', '\\', ':', '*', '?', '"', '<', '>', '|'] {
        assert!(!sanitized.contains(*ch));
    }
}

#[test]
fn test_safe_extract_zip_prevents_zip_slip() {
    let temp_dir = std::env::temp_dir().join(format!("test_zipslip_{}", uuid::Uuid::new_v4()));
    std::fs::create_dir_all(&temp_dir).unwrap();
    let zip_path = temp_dir.join("malicious.zip");
    let extract_dest = temp_dir.join("extracted");
    std::fs::create_dir_all(&extract_dest).unwrap();

    // Create a zip with ../ traversal in entry name
    let file = File::create(&zip_path).unwrap();
    let mut zip = zip::ZipWriter::new(file);
    let options = SimpleFileOptions::default();

    // In zip crate, start_file checks standard names or raw names
    let _ = zip.start_file("../evil_payload.txt", options);
    let _ = zip.write_all(b"payload");
    let _ = zip.finish();

    let res = safe_extract_zip(zip_path.to_str().unwrap(), extract_dest.to_str().unwrap());
    assert!(res.is_err());
    assert!(res.unwrap_err().contains("Zip Slip"));

    let _ = std::fs::remove_dir_all(temp_dir);
}

#[test]
fn test_parse_page_ranges_safely_limits_large_expressions() {
    let result = parse_page_ranges("1-1000000", 5);
    assert_eq!(result.len(), 1);
    assert_eq!(result[0], vec![1, 2, 3, 4, 5]);
}
