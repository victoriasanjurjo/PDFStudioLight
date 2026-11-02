using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDFStudioLight.Models;
using PDFStudioLight.Services;

namespace PDFStudioLight.ViewModels;

public partial class EditPdfViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly Dictionary<int, int> _rotations = new();

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _fileName = "Ningún archivo cargado";

    [ObservableProperty]
    private bool _hasFile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private PdfPageThumbnail? _selectedPage;

    public ObservableCollection<PdfPageThumbnail> Pages { get; } = new();

    public EditPdfViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task OpenFileAsync()
    {
        var path = await _dialogService.PickSingleFileAsync("Abrir Documento PDF", ".pdf");
        if (!string.IsNullOrEmpty(path))
        {
            await LoadDocumentAsync(path);
        }
    }

    public async Task LoadDocumentAsync(string path)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando páginas y generando miniaturas...";
            CurrentFilePath = path;
            FileName = Path.GetFileName(path);
            Pages.Clear();
            _rotations.Clear();

            var thumbnails = await _pdfService.RenderThumbnailsAsync(path);
            foreach (var t in thumbnails)
            {
                Pages.Add(t);
            }

            HasFile = Pages.Count > 0;
            if (HasFile)
            {
                SelectedPage = Pages.FirstOrDefault();
                StatusMessage = $"Documento cargado: {Pages.Count} página(s).";
            }
            else
            {
                StatusMessage = "No se pudieron cargar páginas del documento.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al cargar PDF", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void RotateLeft()
    {
        if (SelectedPage == null) return;
        SelectedPage.RotationAngle = (SelectedPage.RotationAngle - 90 + 360) % 360;
        _rotations[SelectedPage.PageNumber] = SelectedPage.RotationAngle;
        StatusMessage = $"Página {SelectedPage.PageNumber} rotada a {SelectedPage.RotationAngle}°.";
    }

    [RelayCommand]
    public void RotateRight()
    {
        if (SelectedPage == null) return;
        SelectedPage.RotationAngle = (SelectedPage.RotationAngle + 90) % 360;
        _rotations[SelectedPage.PageNumber] = SelectedPage.RotationAngle;
        StatusMessage = $"Página {SelectedPage.PageNumber} rotada a {SelectedPage.RotationAngle}°.";
    }

    [RelayCommand]
    public async Task DeletePageAsync()
    {
        if (SelectedPage == null) return;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Eliminar página",
            $"¿Deseas eliminar la página {SelectedPage.PageNumber} del documento?",
            "Eliminar",
            "Cancelar");

        if (!confirm) return;

        int pageNum = SelectedPage.PageNumber;
        Pages.Remove(SelectedPage);
        _rotations.Remove(pageNum);

        // Re-enumerar para mantener consistencia visual
        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }

        SelectedPage = Pages.FirstOrDefault();
        HasFile = Pages.Count > 0;
        StatusMessage = $"Página eliminada. Total restante: {Pages.Count} página(s).";
    }

    [RelayCommand]
    public async Task SaveAsAsync()
    {
        if (!HasFile || string.IsNullOrEmpty(CurrentFilePath)) return;

        var defaultName = $"{Path.GetFileNameWithoutExtension(CurrentFilePath)}_editado.pdf";
        var outPath = await _dialogService.PickSaveFileAsync(defaultName, "Documento PDF", ".pdf");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Guardando documento editado...";

            var pagesToKeep = Pages.Select(p => p.PageNumber).ToList();
            await _pdfService.SaveEditedPdfAsync(CurrentFilePath, outPath, pagesToKeep, _rotations);

            StatusMessage = "¡Documento guardado con éxito!";
            await _dialogService.ShowMessageAsync("Éxito", $"El documento ha sido guardado exitosamente en:\n{outPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al guardar", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

