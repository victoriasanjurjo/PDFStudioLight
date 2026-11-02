namespace PDFStudioLight.Models;

/// <summary>
/// Opciones de configuración de la aplicación.
/// </summary>
public class AppSettings
{
    public string Theme { get; set; } = "Default"; // Default, Light, Dark
    public string OutputDirectory { get; set; } = string.Empty;
    public bool AutoOpenOutputFolder { get; set; } = false;
}

