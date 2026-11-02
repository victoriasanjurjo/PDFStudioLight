using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using PDFStudioLight.Services;

namespace PDFStudioLight.ViewModels;

public partial class AddImageViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _pdfPath = string.Empty;

    [ObservableProperty]
    private string _pdfFileName = "Ningún PDF seleccionado";

    [ObservableProperty]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    private string _imageFileName = "Ninguna imagen seleccionada";

    [ObservableProperty]
    private BitmapImage? _imagePreview;

    [ObservableProperty]
    private int _targetPage = 1;

    [ObservableProperty]
    private int _maxPages = 1;

    [ObservableProperty]
    private double _posX = 50;

    [ObservableProperty]
    private double _posY = 50;

    [ObservableProperty]
    private double _imageWidth = 150;

    [ObservableProperty]
    private double _imageHeight = 100;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Selecciona un PDF y una imagen (sello, logo o firma) para incrustar.";

    public AddImageViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task SelectPdfAsync()
    {
        var file = await _dialogService.PickSingleFileAsync("Seleccionar PDF", ".pdf");
        if (string.IsNullOrEmpty(file)) return;

        PdfPath = file;
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
    public async Task SelectImageAsync()
    {
        var file = await _dialogService.PickSingleFileAsync("Seleccionar Imagen", ".png", ".jpg", ".jpeg", ".bmp");
        if (string.IsNullOrEmpty(file)) return;

        ImagePath = file;
        ImageFileName = Path.GetFileName(file);
        try
        {
            ImagePreview = new BitmapImage(new Uri(file));
            StatusMessage = $"Imagen seleccionada: {ImageFileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error con la imagen: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ApplyPreset(string preset)
    {
        // Dimensiones estándar A4 ~ 595 x 842 pt
        switch (preset)
        {
            case "BottomRight":
                PosX = 380;
                PosY = 680;
                ImageWidth = 160;
                ImageHeight = 100;
                break;
            case "BottomLeft":
                PosX = 50;
                PosY = 680;
                ImageWidth = 160;
                ImageHeight = 100;
                break;
            case "Center":
                PosX = 220;
                PosY = 370;
                ImageWidth = 160;
                ImageHeight = 100;
                break;
            case "TopRight":
                PosX = 380;
                PosY = 50;
                ImageWidth = 160;
                ImageHeight = 100;
                break;
        }
        StatusMessage = $"Posición ajustada a {preset}.";
    }

    [RelayCommand]
    public async Task ApplyAndSaveAsync()
    {
        if (string.IsNullOrEmpty(PdfPath) || !File.Exists(PdfPath))
        {
            await _dialogService.ShowMessageAsync("Aviso", "Primero selecciona un archivo PDF válido.");
            return;
        }

        if (string.IsNullOrEmpty(ImagePath) || !File.Exists(ImagePath))
        {
            await _dialogService.ShowMessageAsync("Aviso", "Selecciona una imagen (PNG/JPG) para incrustar.");
            return;
        }

        var defaultName = $"{Path.GetFileNameWithoutExtension(PdfPath)}_con_imagen.pdf";
        var outPath = await _dialogService.PickSaveFileAsync(defaultName, "Documento PDF", ".pdf");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Incrustando imagen en el PDF...";

            await _pdfService.AddImageToPdfAsync(PdfPath, ImagePath, outPath, TargetPage, PosX, PosY, ImageWidth, ImageHeight);

            StatusMessage = "¡Imagen incrustada con éxito!";
            await _dialogService.ShowMessageAsync("Éxito", $"Se guardó el documento con la imagen incrustada en:\n{outPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al incrustar imagen", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

