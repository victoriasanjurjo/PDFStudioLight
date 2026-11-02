using System;
using System.IO;
using System.IO.Compression;
using System.Security;
using PDFStudioLight.Helpers;
using PDFStudioLight.Services;
using Xunit;

namespace PDFStudioLight.Tests;

public class SecurityTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfService _service;

    public SecurityTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PDFStudioLightSecurityTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new PdfService();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void ValidatePdfFile_RejectsNonPdfExtensions()
    {
        var fakePath = Path.Combine(_tempDir, "malware.exe");
        Assert.Throws<SecurityException>(() => SecurityValidator.ValidatePdfFile(fakePath, mustExist: false));
    }

    [Fact]
    public void ValidatePdfFile_RejectsReservedDeviceNames()
    {
        var conPath = Path.Combine(_tempDir, "CON.pdf");
        Assert.Throws<SecurityException>(() => SecurityValidator.ValidatePdfFile(conPath, mustExist: false));
    }

    [Fact]
    public void ValidateImageFile_RejectsExecutableAsImage()
    {
        var scriptPath = Path.Combine(_tempDir, "trojan.bat");
        Assert.Throws<SecurityException>(() => SecurityValidator.ValidateImageFile(scriptPath, mustExist: false));
    }

    [Fact]
    public void SanitizeFileName_RemovesIllegalCharacters()
    {
        string maliciousName = "test/..\\:*?\"<>|file.pdf";
        string sanitized = SecurityValidator.SanitizeFileName(maliciousName);

        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain("\\", sanitized);
        Assert.DoesNotContain(":", sanitized);
        Assert.DoesNotContain("*", sanitized);
        Assert.DoesNotContain("?", sanitized);
        Assert.DoesNotContain("\"", sanitized);
        Assert.DoesNotContain("<", sanitized);
        Assert.DoesNotContain(">", sanitized);
        Assert.DoesNotContain("|", sanitized);
    }

    [Fact]
    public void SafeExtractZip_ThrowsOnZipSlipAttempt()
    {
        // Crear un archivo ZIP malicioso con una entrada que intenta escapar hacia el directorio padre
        string zipPath = Path.Combine(_tempDir, "zipslip.zip");
        string extractDest = Path.Combine(_tempDir, "target_extract");
        Directory.CreateDirectory(extractDest);

        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("../evil_payload.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.WriteLine("Contenido de prueba");
        }

        Assert.Throws<SecurityException>(() => SecurityValidator.SafeExtractZip(zipPath, extractDest));
    }

    [Fact]
    public void ParsePageRanges_SafelyLimitsLargeOrMaliciousExpressions()
    {
        var result = PdfService.ParsePageRanges("1-1000000", 5);
        Assert.Single(result);
        Assert.Equal(5, result[0].Count); // Acotado a maxPage
    }
}

