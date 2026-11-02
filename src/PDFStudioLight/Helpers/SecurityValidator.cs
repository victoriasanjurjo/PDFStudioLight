using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;

namespace PDFStudioLight.Helpers;

/// <summary>
/// Módulo central de seguridad y hardening.
/// Proporciona validación estricta de rutas, mitigación de Zip Slip, protección contra Zip Bombs y saneamiento de nombres de archivo.
/// </summary>
public static class SecurityValidator
{
    public const long MaxPdfSizeBytes = 500L * 1024 * 1024;      // 500 MB
    public const long MaxImageSizeBytes = 100L * 1024 * 1024;    // 100 MB
    public const long MaxZipUncompressedBytes = 1024L * 1024 * 1024; // 1 GB
    public const int MaxZipEntryCount = 5000;
    public const int MaxPageGroups = 2000;

    private static readonly HashSet<string> AllowedPdfExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp"
    };

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// Valida que la ruta de un PDF sea segura, con extensión válida y tamaño controlado.
    /// </summary>
    public static void ValidatePdfFile(string filePath, bool mustExist = true)
    {
        ValidatePathBasics(filePath);

        var ext = Path.GetExtension(filePath);
        if (!AllowedPdfExtensions.Contains(ext))
        {
            throw new SecurityException($"Extensión no permitida para archivo PDF: '{ext}'.");
        }

        if (mustExist)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("El archivo PDF especificado no existe.", filePath);

            var info = new FileInfo(filePath);
            if (info.Length > MaxPdfSizeBytes)
            {
                throw new SecurityException($"El archivo PDF supera el límite de seguridad permitido ({MaxPdfSizeBytes / (1024 * 1024)} MB).");
            }
        }
    }

    /// <summary>
    /// Valida que la ruta de una imagen sea segura y con formato permitido.
    /// </summary>
    public static void ValidateImageFile(string imagePath, bool mustExist = true)
    {
        ValidatePathBasics(imagePath);

        var ext = Path.GetExtension(imagePath);
        if (!AllowedImageExtensions.Contains(ext))
        {
            throw new SecurityException($"Formato de imagen no permitido: '{ext}'. Formatos soportados: PNG, JPG, JPEG, BMP.");
        }

        if (mustExist)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("El archivo de imagen especificado no existe.", imagePath);

            var info = new FileInfo(imagePath);
            if (info.Length > MaxImageSizeBytes)
            {
                throw new SecurityException($"La imagen supera el límite de seguridad de tamaño ({MaxImageSizeBytes / (1024 * 1024)} MB).");
            }
        }
    }

    /// <summary>
    /// Valida rutas básicas contra nombres de dispositivo reservados y caracteres nulos.
    /// </summary>
    public static void ValidatePathBasics(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("La ruta especificada no puede estar vacía.");

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || path.Contains('\0'))
            throw new SecurityException("La ruta contiene caracteres no válidos o potencialmente maliciosos.");

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        if (ReservedDeviceNames.Contains(fileNameWithoutExt))
            throw new SecurityException($"Nombre de archivo reservado por el sistema no permitido: '{fileNameWithoutExt}'.");
    }

    /// <summary>
    /// Sanea un nombre de archivo eliminando caracteres ilegales o intentos de escape.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "archivo";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(ch => !invalidChars.Contains(ch) && ch != '\0').ToArray());

        if (string.IsNullOrWhiteSpace(sanitized) || ReservedDeviceNames.Contains(sanitized))
        {
            return "archivo_seguro";
        }

        return sanitized;
    }

    /// <summary>
    /// Extrae un archivo ZIP aplicando defensas estrictas contra Zip Slip y Zip Bombs.
    /// </summary>
    public static void SafeExtractZip(string zipPath, string destinationDirectory)
    {
        ValidatePathBasics(zipPath);
        if (!File.Exists(zipPath))
            throw new FileNotFoundException("El archivo ZIP no existe.", zipPath);

        var canonicalDestDir = Path.GetFullPath(destinationDirectory);
        if (!canonicalDestDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            canonicalDestDir += Path.DirectorySeparatorChar;
        }

        if (!Directory.Exists(canonicalDestDir))
        {
            Directory.CreateDirectory(canonicalDestDir);
        }

        using var archive = ZipFile.OpenRead(zipPath);

        if (archive.Entries.Count > MaxZipEntryCount)
        {
            throw new SecurityException($"El archivo ZIP contiene demasiadas entradas ({archive.Entries.Count}), superando el límite seguro de {MaxZipEntryCount}.");
        }

        long totalUncompressedBytes = 0;

        foreach (var entry in archive.Entries)
        {
            // 1. Mitigación de Zip Slip / Path Traversal
            var destinationPath = Path.GetFullPath(Path.Combine(canonicalDestDir, entry.FullName));
            if (!destinationPath.StartsWith(canonicalDestDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Intento de Path Traversal / Zip Slip detectado en la entrada: '{entry.FullName}'.");
            }

            // 2. Mitigación de Zip Bomb (Monitoreo acumulativo de tamaño descomprimido)
            totalUncompressedBytes += entry.Length;
            if (totalUncompressedBytes > MaxZipUncompressedBytes)
            {
                throw new SecurityException($"El archivo ZIP supera el límite de descompresión seguro ({MaxZipUncompressedBytes / (1024 * 1024)} MB). Posible Zip Bomb detectado.");
            }

            // Si es un directorio, crearlo
            if (string.IsNullOrEmpty(entry.Name) && destinationPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            // Asegurar que el directorio contenedor existe
            var entryDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(entryDir) && !Directory.Exists(entryDir))
            {
                Directory.CreateDirectory(entryDir);
            }

            // Extraer el archivo con búfer seguro
            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}

