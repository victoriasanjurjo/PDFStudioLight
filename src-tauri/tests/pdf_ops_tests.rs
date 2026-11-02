use pdf_studio_light_lib::services::pdf_ops::{convert_images_to_pdf, get_page_count, parse_page_ranges};
use pdf_studio_light_lib::services::zip_ops::{create_zip_archive, extract_zip_archive};

#[test]
fn test_parse_page_ranges_syntax() {
    let ranges = parse_page_ranges("1-3, 5, 8-10", 12);
    assert_eq!(ranges.len(), 3);
    assert_eq!(ranges[0], vec![1, 2, 3]);
    assert_eq!(ranges[1], vec![5]);
    assert_eq!(ranges[2], vec![8, 9, 10]);
}

#[test]
fn test_zip_create_and_safe_extract() {
    let temp_dir = std::env::temp_dir().join(format!("test_zip_roundtrip_{}", uuid::Uuid::new_v4()));
    std::fs::create_dir_all(&temp_dir).unwrap();

    let file1 = temp_dir.join("prueba1.txt");
    let file2 = temp_dir.join("prueba2.txt");
    std::fs::write(&file1, b"Contenido de archivo 1").unwrap();
    std::fs::write(&file2, b"Contenido de archivo 2").unwrap();

    let zip_out = temp_dir.join("salida.zip");
    let files = vec![file1.to_str().unwrap().to_string(), file2.to_str().unwrap().to_string()];

    let res = create_zip_archive(&files, zip_out.to_str().unwrap());
    assert!(res.is_ok());
    assert!(zip_out.exists());

    let dest_dir = temp_dir.join("extraido");
    let ext_res = extract_zip_archive(zip_out.to_str().unwrap(), dest_dir.to_str().unwrap());
    assert!(ext_res.is_ok());

    let extracted = ext_res.unwrap();
    assert_eq!(extracted.len(), 2);

    let _ = std::fs::remove_dir_all(temp_dir);
}

#[test]
fn test_convert_images_to_pdf_roundtrip() {
    let temp_dir = std::env::temp_dir().join(format!("test_img2pdf_{}", uuid::Uuid::new_v4()));
    std::fs::create_dir_all(&temp_dir).unwrap();

    // Create a small 50x50 PNG in memory
    let img_path = temp_dir.join("sample.png");
    let img = image::RgbImage::new(50, 50);
    img.save(&img_path).unwrap();

    let out_pdf = temp_dir.join("converted.pdf");
    let res = convert_images_to_pdf(&[img_path.to_str().unwrap().to_string()], out_pdf.to_str().unwrap());
    assert!(res.is_ok());
    assert!(out_pdf.exists());

    let page_count = get_page_count(out_pdf.to_str().unwrap()).unwrap();
    assert_eq!(page_count, 1);

    let _ = std::fs::remove_dir_all(temp_dir);
}
