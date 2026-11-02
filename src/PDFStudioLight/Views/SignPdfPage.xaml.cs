using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using PDFStudioLight.Services;
using PDFStudioLight.ViewModels;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace PDFStudioLight.Views;

public sealed partial class SignPdfPage : Page
{
    public SignPdfViewModel ViewModel { get; }
    private readonly IDialogService _dialogService;
    private Polyline? _currentStroke;
    private Color _strokeColor = Colors.Black;

    public SignPdfPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SignPdfViewModel>();
        _dialogService = App.Services.GetRequiredService<IDialogService>();
    }

    private void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(SignatureCanvas);
        _currentStroke = new Polyline
        {
            Stroke = new SolidColorBrush(_strokeColor),
            StrokeThickness = ViewModel.StrokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };
        _currentStroke.Points.Add(point.Position);
        SignatureCanvas.Children.Add(_currentStroke);
        SignatureCanvas.CapturePointer(e.Pointer);
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_currentStroke != null)
        {
            var point = e.GetCurrentPoint(SignatureCanvas);
            _currentStroke.Points.Add(point.Position);
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _currentStroke = null;
        SignatureCanvas.ReleasePointerCapture(e.Pointer);
    }

    private void OnClearCanvasClick(object sender, RoutedEventArgs e)
    {
        SignatureCanvas.Children.Clear();
    }

    private void OnColorBlackClick(object sender, RoutedEventArgs e) => _strokeColor = Colors.Black;
    private void OnColorBlueClick(object sender, RoutedEventArgs e) => _strokeColor = ColorHelper.FromArgb(255, 0, 70, 180);
    private void OnColorRedClick(object sender, RoutedEventArgs e) => _strokeColor = ColorHelper.FromArgb(255, 200, 20, 20);

    private async Task<string?> RenderCanvasToTempFileAsync()
    {
        if (SignatureCanvas.Children.Count == 0) return null;

        var rtb = new RenderTargetBitmap();
        await rtb.RenderAsync(SignatureCanvas);

        var pixelBuffer = await rtb.GetPixelsAsync();
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"firma_{Guid.NewGuid():N}.png");

        using (var fileStream = File.Create(tempFile))
        using (var randomStream = fileStream.AsRandomAccessStream())
        {
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)rtb.PixelWidth,
                (uint)rtb.PixelHeight,
                96,
                96,
                pixelBuffer.ToArray());

            await encoder.FlushAsync();
        }

        return tempFile;
    }

    private async void OnExportPngClick(object sender, RoutedEventArgs e)
    {
        var tempPng = await RenderCanvasToTempFileAsync();
        if (tempPng == null)
        {
            await _dialogService.ShowMessageAsync("Aviso", "Primero dibuja una firma en la pizarra para exportarla.");
            return;
        }

        var savePath = await _dialogService.PickSaveFileAsync("mi_firma.png", "Imagen PNG", ".png");
        if (!string.IsNullOrEmpty(savePath))
        {
            File.Copy(tempPng, savePath, true);
            await _dialogService.ShowMessageAsync("Éxito", $"Firma exportada correctamente en:\n{savePath}");
        }
    }

    private async void OnStampToPdfClick(object sender, RoutedEventArgs e)
    {
        var tempPng = await RenderCanvasToTempFileAsync();
        await ViewModel.StampSignatureOnPdfAsync(tempPng ?? string.Empty);
    }
}
