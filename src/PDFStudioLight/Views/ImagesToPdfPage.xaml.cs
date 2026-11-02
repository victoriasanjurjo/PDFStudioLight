using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class ImagesToPdfPage : Page
{
    public ImagesToPdfViewModel ViewModel { get; }

    public ImagesToPdfPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ImagesToPdfViewModel>();
    }
}

