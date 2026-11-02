use crate::models::AppSettings;
use std::fs;
use std::path::{Path, PathBuf};

fn get_settings_path() -> PathBuf {
    if let Ok(exe) = std::env::current_exe() {
        if let Some(parent) = exe.parent() {
            let config_dir = parent.join("config");
            return config_dir.join("settings.json");
        }
    }
    PathBuf::from("config/settings.json")
}

pub fn load_settings() -> AppSettings {
    let path = get_settings_path();
    if path.exists() {
        if let Ok(content) = fs::read_to_string(&path) {
            if let Ok(mut settings) = serde_json::from_str::<AppSettings>(&content) {
                if settings.theme != "Light" && settings.theme != "Dark" {
                    settings.theme = "Default".to_string();
                }
                if settings.output_directory.is_empty() || !Path::new(&settings.output_directory).exists() {
                    let default_app = AppSettings::default();
                    settings.output_directory = default_app.output_directory;
                }
                return settings;
            }
        }
    }
    AppSettings::default()
}

pub fn save_settings(settings: &AppSettings) -> Result<(), String> {
    let path = get_settings_path();
    if let Some(parent) = path.parent() {
        if !parent.exists() {
            fs::create_dir_all(parent)
                .map_err(|e| format!("Error creando carpeta config: {}", e))?;
        }
    }

    let json = serde_json::to_string_pretty(settings)
        .map_err(|e| format!("Error serializando configuraciones: {}", e))?;

    fs::write(&path, json)
        .map_err(|e| format!("Error escribiendo archivo settings.json: {}", e))?;

    Ok(())
}
