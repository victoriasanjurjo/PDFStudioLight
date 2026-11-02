using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDFStudioLight.Models;
using PDFStudioLight.Services;

namespace PDFStudioLight.ViewModels;

public partial class CompressPdfViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _compressFilePath = string.Empty;

    [ObservableProperty]
    private string _compressFileName = "Ningún PDF seleccionado";

    [ObservableProperty]
    private string _originalSizeText = string.Empty;

    [ObservableProperty]
    private string _compressedResultText = string.Empty;

    [ObservableProperty]
    private bool _hasFileToCompress;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Optimiza documentos PDF reduciendo peso o empaquétalos en formato ZIP.";

    // Para el gestor de ZIP
    public ObservableCollection<DocumentItem> ZipItems { get; } = new();

    public CompressPdfViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task SelectPdfToCompressAsync()
    {
        var file = await _dialogService.PickSingleFileAsync("Seleccionar PDF a comprimir", ".pdf");
        if (string.IsNullOrEmpty(file)) return;

        CompressFilePath = file;
        CompressFileName = Path.GetFileName(file);
        var info = new FileInfo(file);
        OriginalSizeText = $"Tamaño original: {DocumentItem.FormatBytes(info.Length)}";
        CompressedResultText = string.Empty;
        HasFileToCompress = true;
        StatusMessage = "Archivo listo para optimización.";
    }

    [RelayCommand]
    public async Task CompressPdfAsync()
    {
        if (string.IsNullOrEmpty(CompressFilePath) || !File.Exists(CompressFilePath))
        {
            await _dialogService.ShowMessageAsync("Aviso", "Selecciona un archivo PDF primero.");
            return;
        }

        var defaultName = $"{Path.GetFileNameWithoutExtension(CompressFilePath)}_comprimido.pdf";
        var outPath = await _dialogService.PickSaveFileAsync(defaultName, "Documento PDF", ".pdf");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Comprimiendo y optimizando flujos internos...";

            var origLen = new FileInfo(CompressFilePath).Length;
            await _pdfService.CompressPdfAsync(CompressFilePath, outPath);

            var newLen = new FileInfo(outPath).Length;
            double savedPct = origLen > 0 ? (1.0 - (double)newLen / origLen) * 100 : 0;
            if (savedPct < 0) savedPct = 0;

            CompressedResultText = $"Nuevo tamaño: {DocumentItem.FormatBytes(newLen)} (Ahorro del {savedPct:0.0}%)";
            StatusMessage = "¡Compresión finalizada exitosamente!";
            await _dialogService.ShowMessageAsync("Éxito", $"Documento optimizado con éxito:\n{outPath}\n\n{CompressedResultText}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al comprimir", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddFilesToZipAsync()
    {
        var files = await _dialogService.PickMultipleFilesAsync("Archivos para comprimir en ZIP", "*");
        if (files == null || files.Count == 0) return;

        foreach (var f in files)
        {
            if (ZipItems.Any(i => i.FilePath.Equals(f, StringComparison.OrdinalIgnoreCase))) continue;
            ZipItems.Add(new DocumentItem(f));
        }

        StatusMessage = $"{ZipItems.Count} archivo(s) para empaquetar en ZIP.";
    }

    [RelayCommand]
    public void RemoveZipItem(DocumentItem item)
    {
        ZipItems.Remove(item);
    }

    [RelayCommand]
    public void ClearZipItems()
    {
        ZipItems.Clear();
    }

    [RelayCommand]
    public async Task CreateZipAsync()
    {
        if (ZipItems.Count == 0)
        {
            await _dialogService.ShowMessageAsync("Aviso", "Agrega al menos un archivo para crear el ZIP.");
            return;
        }

        var outPath = await _dialogService.PickSaveFileAsync("archivo_comprimido.zip", "Archivo Comprimido ZIP", ".zip");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Creando archivo ZIP...";

            var files = ZipItems.Select(z => z.FilePath).ToList();
            await _pdfService.CreateZipArchiveAsync(files, outPath);

            StatusMessage = "¡Archivo ZIP creado con éxito!";
            await _dialogService.ShowMessageAsync("Éxito", $"Se empaquetaron {ZipItems.Count} archivos en:\n{outPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al crear ZIP", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ExtractZipAsync()
    {
        var zipFile = await _dialogService.PickSingleFileAsync("Seleccionar archivo ZIP", ".zip");
        if (string.IsNullOrEmpty(zipFile)) return;

        var folder = await _dialogService.PickFolderAsync();
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Descomprimiendo archivo ZIP...";

            await _pdfService.ExtractZipArchiveAsync(zipFile, folder);

            StatusMessage = "¡Descompresión completada!";
            await _dialogService.ShowMessageAsync("Éxito", $"Archivos extraídos en:\n{folder}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al extraer ZIP", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

