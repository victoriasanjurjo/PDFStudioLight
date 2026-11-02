pub mod commands;
pub mod models;
pub mod services;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_opener::init())
        .invoke_handler(tauri::generate_handler![
            commands::get_file_info,
            commands::get_page_count,
            commands::merge_pdfs,
            commands::split_pdf,
            commands::save_edited_pdf,
            commands::compress_pdf,
            commands::convert_images_to_pdf,
            commands::add_image_to_pdf,
            commands::stamp_signature,
            commands::create_zip_archive,
            commands::extract_zip_archive,
            commands::load_settings,
            commands::save_settings,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
