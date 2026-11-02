use crate::models::{AppSettings, CompressionResult, DocumentItemInfo, MergeResult};
use crate::services::{pdf_ops, settings as settings_service, zip_ops};
use std::collections::HashMap;
use std::fs;
use std::path::Path;

fn format_bytes_helper(bytes: u64) -> String {
    let sizes = ["B", "KB", "MB", "GB"];
    let mut len = bytes as f64;
    let mut order = 0;
    while len >= 1024.0 && order < sizes.len() - 1 {
        order += 1;
        len /= 1024.0;
    }
    format!("{:.2} {}", len, sizes[order])
}

#[tauri::command]
pub fn get_file_info(file_path: String) -> Result<DocumentItemInfo, String> {
    let path = Path::new(&file_path);
    if !path.exists() {
        return Err(format!("El archivo no existe: {}", file_path));
    }

    let meta = fs::metadata(path).map_err(|e| format!("Error leyendo archivo: {}", e))?;
    let file_name = path
        .file_name()
        .and_then(|n| n.to_str())
        .unwrap_or(&file_path)
        .to_string();

    let page_count = if file_name.to_lowercase().ends_with(".pdf") {
        pdf_ops::get_page_count(&file_path).unwrap_or(0)
    } else {
        0
    };

    Ok(DocumentItemInfo {
        file_path,
        file_name,
        file_size_bytes: meta.len(),
        file_size_formatted: format_bytes_helper(meta.len()),
        page_count,
    })
}

#[tauri::command]
pub fn get_page_count(file_path: String) -> Result<u32, String> {
    pdf_ops::get_page_count(&file_path)
}

#[tauri::command]
pub fn merge_pdfs(files: Vec<String>, output_path: String) -> Result<MergeResult, String> {
    let total_pages = pdf_ops::merge_pdfs(&files, &output_path)?;
    Ok(MergeResult {
        output_path,
        total_pages,
    })
}

#[tauri::command]
pub fn split_pdf(
    input_path: String,
    output_dir: String,
    ranges: Option<String>,
    split_all: bool,
) -> Result<Vec<String>, String> {
    pdf_ops::split_pdf(&input_path, &output_dir, ranges.as_deref(), split_all)
}

#[tauri::command]
pub fn save_edited_pdf(
    input_path: String,
    output_path: String,
    pages_to_keep: Vec<u32>,
    rotations: HashMap<u32, i32>,
) -> Result<(), String> {
    pdf_ops::save_edited_pdf(&input_path, &output_path, &pages_to_keep, &rotations)
}

#[tauri::command]
pub fn compress_pdf(input_path: String, output_path: String) -> Result<CompressionResult, String> {
    pdf_ops::compress_pdf(&input_path, &output_path)
}

#[tauri::command]
pub fn convert_images_to_pdf(images: Vec<String>, output_path: String) -> Result<(), String> {
    pdf_ops::convert_images_to_pdf(&images, &output_path)
}

#[tauri::command]
pub fn add_image_to_pdf(
    input_pdf: String,
    input_image: String,
    output_path: String,
    page: u32,
    x: f64,
    y: f64,
    width: f64,
    height: f64,
) -> Result<(), String> {
    pdf_ops::add_image_or_signature_to_pdf(
        &input_pdf,
        &input_image,
        &output_path,
        page,
        x,
        y,
        width,
        height,
    )
}

#[tauri::command]
pub fn stamp_signature(
    input_pdf: String,
    signature_data: String,
    output_path: String,
    page: u32,
    x: f64,
    y: f64,
    width: f64,
    height: f64,
) -> Result<(), String> {
    pdf_ops::add_image_or_signature_to_pdf(
        &input_pdf,
        &signature_data,
        &output_path,
        page,
        x,
        y,
        width,
        height,
    )
}

#[tauri::command]
pub fn create_zip_archive(files: Vec<String>, output_zip_path: String) -> Result<(), String> {
    zip_ops::create_zip_archive(&files, &output_zip_path)
}

#[tauri::command]
pub fn extract_zip_archive(zip_path: String, destination_dir: String) -> Result<Vec<String>, String> {
    zip_ops::extract_zip_archive(&zip_path, &destination_dir)
}

#[tauri::command]
pub fn load_settings() -> Result<AppSettings, String> {
    Ok(settings_service::load_settings())
}

#[tauri::command]
pub fn save_settings(settings: AppSettings) -> Result<(), String> {
    settings_service::save_settings(&settings)
}
