using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class CompressPage : Page
{
    public CompressPdfViewModel ViewModel { get; }

    public CompressPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CompressPdfViewModel>();
    }
}

