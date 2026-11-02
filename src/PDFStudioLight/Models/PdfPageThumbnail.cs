using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PDFStudioLight.Models;

/// <summary>
/// Representa una página individual de un PDF para el visor y editor de páginas.
/// </summary>
public partial class PdfPageThumbnail : ObservableObject
{
    [ObservableProperty]
    private int _pageNumber;

    [ObservableProperty]
    private int _rotationAngle; // 0, 90, 180, 270

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private BitmapImage? _thumbnailImage;

    [ObservableProperty]
    private double _originalWidth;

    [ObservableProperty]
    private double _originalHeight;

    public string PageDisplay => $"Página {PageNumber}";
    public string RotationDisplay => $"Rotación: {RotationAngle}°";

    partial void OnPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(PageDisplay));
    }

    partial void OnRotationAngleChanged(int value)
    {
        OnPropertyChanged(nameof(RotationDisplay));
    }
}
