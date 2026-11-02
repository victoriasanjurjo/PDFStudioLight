use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AppSettings {
    pub theme: String,
    pub output_directory: String,
    pub auto_open_output_folder: bool,
}

impl Default for AppSettings {
    fn default() -> Self {
        let default_output = dirs_next().unwrap_or_else(|| ".".to_string());
        Self {
            theme: "Default".to_string(),
            output_directory: default_output,
            auto_open_output_folder: false,
        }
    }
}

fn dirs_next() -> Option<String> {
    if let Ok(profile) = std::env::var("USERPROFILE") {
        let downloads = std::path::Path::new(&profile).join("Downloads");
        if downloads.exists() {
            return Some(downloads.to_string_lossy().to_string());
        }
        return Some(profile);
    }
    None
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DocumentItemInfo {
    pub file_path: String,
    pub file_name: String,
    pub file_size_bytes: u64,
    pub file_size_formatted: String,
    pub page_count: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CompressionResult {
    pub original_size_bytes: u64,
    pub compressed_size_bytes: u64,
    pub saved_percentage: f64,
    pub output_path: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MergeResult {
    pub output_path: String,
    pub total_pages: u32,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PageInfo {
    pub page_number: u32,
    pub rotation: i32,
}
