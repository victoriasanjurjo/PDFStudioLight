using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class SplitPdfPage : Page
{
    public SplitPdfViewModel ViewModel { get; }

    public SplitPdfPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SplitPdfViewModel>();
    }
}

