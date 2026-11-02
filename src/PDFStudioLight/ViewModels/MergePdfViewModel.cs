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

public partial class MergePdfViewModel : ObservableObject
{
    private readonly IPdfService _pdfService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private DocumentItem? _selectedItem;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Agrega documentos PDF para combinarlos en uno solo.";

    public ObservableCollection<DocumentItem> Documents { get; } = new();

    public bool HasDocuments => Documents.Count > 0;
    public bool HasNoDocuments => Documents.Count == 0;

    public MergePdfViewModel(IPdfService pdfService, IDialogService dialogService, ISettingsService settingsService)
    {
        _pdfService = pdfService;
        _dialogService = dialogService;
        _settingsService = settingsService;
        Documents.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasDocuments));
            OnPropertyChanged(nameof(HasNoDocuments));
        };
    }

    [RelayCommand]
    public async Task AddFilesAsync()
    {
        var files = await _dialogService.PickMultipleFilesAsync("Seleccionar archivos PDF", ".pdf");
        if (files == null || files.Count == 0) return;

        foreach (var file in files)
        {
            if (Documents.Any(d => d.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                continue;

            int count = 0;
            try
            {
                count = _pdfService.GetPageCount(file);
            }
            catch { }

            Documents.Add(new DocumentItem(file, count));
        }

        StatusMessage = $"{Documents.Count} archivo(s) en cola para unir.";
    }

    [RelayCommand]
    public void MoveUp()
    {
        if (SelectedItem == null) return;
        int index = Documents.IndexOf(SelectedItem);
        if (index > 0)
        {
            Documents.Move(index, index - 1);
        }
    }

    [RelayCommand]
    public void MoveDown()
    {
        if (SelectedItem == null) return;
        int index = Documents.IndexOf(SelectedItem);
        if (index >= 0 && index < Documents.Count - 1)
        {
            Documents.Move(index, index + 1);
        }
    }

    [RelayCommand]
    public void RemoveSelected()
    {
        if (SelectedItem == null) return;
        Documents.Remove(SelectedItem);
        StatusMessage = $"{Documents.Count} archivo(s) en cola.";
    }

    [RelayCommand]
    public void ClearAll()
    {
        Documents.Clear();
        StatusMessage = "Cola vacía. Agrega archivos PDF para comenzar.";
    }

    [RelayCommand]
    public async Task MergeAsync()
    {
        if (Documents.Count < 2)
        {
            await _dialogService.ShowMessageAsync("Aviso", "Se requieren al menos 2 archivos PDF para realizar la unión.");
            return;
        }

        var defaultName = "documento_unido.pdf";
        var outPath = await _dialogService.PickSaveFileAsync(defaultName, "Documento PDF", ".pdf");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Uniendo documentos PDF...";

            var fileList = Documents.Select(d => d.FilePath).ToList();
            await _pdfService.MergePdfsAsync(fileList, outPath);

            StatusMessage = "¡Unión completada exitosamente!";
            await _dialogService.ShowMessageAsync("Éxito", $"Se unieron {Documents.Count} documentos en:\n{outPath}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            await _dialogService.ShowMessageAsync("Error al unir documentos", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
