using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class MergePdfPage : Page
{
    public MergePdfViewModel ViewModel { get; }

    public MergePdfPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MergePdfViewModel>();
    }
}

