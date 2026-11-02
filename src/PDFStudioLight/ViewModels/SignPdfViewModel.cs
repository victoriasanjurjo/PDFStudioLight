using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDFStudioLight.Services;

namespace PDFStudioLight.ViewModels;

public partial class SignPdfViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _signatureImagePath = string.Empty;

    [ObservableProperty]
    private string _targetPdfPath = string.Empty;

    [ObservableProperty]
    private string _pdfFileName = "Ningún PDF seleccionado";

    [ObservableProperty]
    private int _targetPage = 1;

    [ObservableProperty]
    private int _maxPages = 1;

    [ObservableProperty]
    private double _strokeThickness = 3.0;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Dibuja tu firma digital o carga una firma previa en formato transparente.";

    public SignPdfViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task SelectPdfAsync()
    {
        var file = await _dialogService.PickSingleFileAsync("Seleccionar PDF para firmar", ".pdf");
        if (string.IsNullOrEmpty(file)) return;

        TargetPdfPath = file;
        PdfFileName = Path.GetFileName(file);
        try
        {
            MaxPages = _pdfService.GetPageCount(file);
            TargetPage = 1;
            StatusMessage = $"PDF cargado: {MaxPages} página(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadExistingSignatureAsync()
    {
        var file = await _dialogService.PickSingleFileAsync("Cargar Firma (PNG)", ".png", ".jpg");
        if (string.IsNullOrEmpty(file)) return;

        SignatureImagePath = file;
        StatusMessage = $"Firma cargada: {Path.GetFileName(file)}";
    }

    [RelayCommand]
    public async Task StampSignatureOnPdfAsync(string tempSignaturePath)
    {
        string signFile = !string.IsNullOrEmpty(SignatureImagePath) && File.Exists(SignatureImagePath)
            ? SignatureImagePath
            : tempSignaturePath;

        if (string.IsNullOrEmpty(signFile) || !File.Exists(signFile))
        {
            await _dialogService.ShowMessageAsync("Aviso", "Primero dibuja o carga una firma.");
            return;
        }

        if (string.IsNullOrEmpty(TargetPdfPath) || !File.Exists(TargetPdfPath))
        {
            await _dialogService.ShowMessageAsync("Aviso", "Selecciona un documento PDF para estampar la firma.");
            return;
        }

        var defaultName = $"{Path.GetFileNameWithoutExtension(TargetPdfPath)}_firmado.pdf";
        var outPath = await _dialogService.PickSaveFileAsync(defaultName, "Documento PDF", ".pdf");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Estampando firma en el documento...";

            // Por defecto en la parte inferior derecha: x=380, y=680, w=160, h=80
            await _pdfService.AddImageToPdfAsync(TargetPdfPath, signFile, outPath, TargetPage, 380, 680, 160, 80);

            StatusMessage = "¡Documento firmado exitosamente!";
            await _dialogService.ShowMessageAsync("Éxito", $"Documento firmado guardado en:\n{outPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al firmar documento", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

