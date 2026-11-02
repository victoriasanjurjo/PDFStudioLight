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

public partial class ImagesToPdfViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private DocumentItem? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Agrega imágenes para ensamblarlas en un único documento PDF.";

    public ObservableCollection<DocumentItem> Images { get; } = new();

    public ImagesToPdfViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task AddImagesAsync()
    {
        var files = await _dialogService.PickMultipleFilesAsync("Seleccionar Imágenes", ".png", ".jpg", ".jpeg", ".bmp");
        if (files == null || files.Count == 0) return;

        foreach (var file in files)
        {
            if (Images.Any(i => i.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase))) continue;
            Images.Add(new DocumentItem(file));
        }

        StatusMessage = $"{Images.Count} imagen(es) en lista.";
    }

    [RelayCommand]
    public void MoveUp()
    {
        if (SelectedItem == null) return;
        int index = Images.IndexOf(SelectedItem);
        if (index > 0)
        {
            Images.Move(index, index - 1);
        }
    }

    [RelayCommand]
    public void MoveDown()
    {
        if (SelectedItem == null) return;
        int index = Images.IndexOf(SelectedItem);
        if (index >= 0 && index < Images.Count - 1)
        {
            Images.Move(index, index + 1);
        }
    }

    [RelayCommand]
    public void RemoveSelected()
    {
        if (SelectedItem == null) return;
        Images.Remove(SelectedItem);
        StatusMessage = $"{Images.Count} imagen(es) en lista.";
    }

    [RelayCommand]
    public void ClearAll()
    {
        Images.Clear();
        StatusMessage = "Lista vacía. Agrega imágenes para comenzar.";
    }

    [RelayCommand]
    public async Task ConvertToPdfAsync()
    {
        if (Images.Count == 0)
        {
            await _dialogService.ShowMessageAsync("Aviso", "Agrega al menos una imagen para generar el PDF.");
            return;
        }

        var defaultName = "imagenes_convertidas.pdf";
        var outPath = await _dialogService.PickSaveFileAsync(defaultName, "Documento PDF", ".pdf");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Generando PDF desde imágenes...";

            var paths = Images.Select(i => i.FilePath).ToList();
            await _pdfService.ConvertImagesToPdfAsync(paths, outPath);

            StatusMessage = "¡PDF generado con éxito!";
            await _dialogService.ShowMessageAsync("Éxito", $"Se generó el documento PDF con {Images.Count} página(s) en:\n{outPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al convertir imágenes", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

