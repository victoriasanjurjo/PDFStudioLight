using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDFStudioLight.Services;

namespace PDFStudioLight.ViewModels;

public partial class SplitPdfViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = "Ningún archivo seleccionado";

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private bool _hasFile;

    [ObservableProperty]
    private bool _splitAllPages = true;

    [ObservableProperty]
    private string _customRanges = "1-2, 3";

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Selecciona un documento PDF para dividirlo en partes o páginas individuales.";

    public SplitPdfViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        OutputDirectory = _settingsService.Settings.OutputDirectory;
    }

    [RelayCommand]
    public async Task SelectFileAsync()
    {
        var file = await _dialogService.PickSingleFileAsync("Seleccionar archivo PDF", ".pdf");
        if (string.IsNullOrEmpty(file)) return;

        FilePath = file;
        FileName = Path.GetFileName(file);
        try
        {
            PageCount = _pdfService.GetPageCount(file);
            HasFile = true;
            StatusMessage = $"Documento cargado con {PageCount} página(s).";
            if (PageCount > 1)
            {
                CustomRanges = $"1-{Math.Min(2, PageCount)}, {Math.Min(3, PageCount)}";
            }
            else
            {
                CustomRanges = "1";
            }
        }
        catch (Exception ex)
        {
            HasFile = false;
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task SelectOutputFolderAsync()
    {
        var folder = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            OutputDirectory = folder;
        }
    }

    [RelayCommand]
    public async Task ExecuteSplitAsync()
    {
        if (!HasFile || string.IsNullOrEmpty(FilePath))
        {
            await _dialogService.ShowMessageAsync("Aviso", "Primero selecciona un archivo PDF.");
            return;
        }

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            OutputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Dividiendo documento...";

            var generated = await _pdfService.SplitPdfAsync(FilePath, OutputDirectory, CustomRanges, SplitAllPages);

            StatusMessage = $"¡División completada! Se generaron {generated.Count} archivo(s).";
            await _dialogService.ShowMessageAsync("Éxito", $"Se han generado {generated.Count} archivo(s) PDF en la carpeta:\n{OutputDirectory}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al dividir PDF", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

