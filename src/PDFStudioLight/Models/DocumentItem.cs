using CommunityToolkit.Mvvm.ComponentModel;

namespace PDFStudioLight.Models;

/// <summary>
/// Elemento de lista para documentos PDF en herramientas como Unir PDF o Comprimir.
/// </summary>
public partial class DocumentItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private long _fileSizeBytes;

    [ObservableProperty]
    private string _fileSizeFormatted = string.Empty;

    [ObservableProperty]
    private int _pageCount;

    [ObservableProperty]
    private bool _isSelected;

    public string PageCountFormatted => $"{PageCount} pág(s)";

    partial void OnPageCountChanged(int value)
    {
        OnPropertyChanged(nameof(PageCountFormatted));
    }

    public DocumentItem() { }

    public DocumentItem(string path, int pageCount = 0)
    {
        FilePath = path;
        FileName = System.IO.Path.GetFileName(path);
        if (System.IO.File.Exists(path))
        {
            var info = new System.IO.FileInfo(path);
            FileSizeBytes = info.Length;
            FileSizeFormatted = FormatBytes(info.Length);
        }
        PageCount = pageCount;
    }

    public static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
