using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PDFStudioLight.Models;

namespace PDFStudioLight.Services;

/// <summary>
/// Gestiona la lectura y persistencia de configuración local en formato JSON.
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "config", "settings.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Settings { get; private set; } = new();

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    // Validación y saneamiento de configuraciones cargadas
                    if (loaded.Theme != "Light" && loaded.Theme != "Dark")
                    {
                        loaded.Theme = "Default";
                    }

                    if (!string.IsNullOrWhiteSpace(loaded.OutputDirectory))
                    {
                        try
                        {
                            var full = Path.GetFullPath(loaded.OutputDirectory);
                            if (Directory.Exists(full))
                            {
                                loaded.OutputDirectory = full;
                            }
                            else
                            {
                                loaded.OutputDirectory = string.Empty;
                            }
                        }
                        catch
                        {
                            loaded.OutputDirectory = string.Empty;
                        }
                    }

                    Settings = loaded;
                }
            }
        }
        catch
        {
            Settings = new AppSettings();
        }

        if (string.IsNullOrWhiteSpace(Settings.OutputDirectory))
        {
            Settings.OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar configuración: {ex.Message}");
        }
    }
}

