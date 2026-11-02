using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class EditPdfPage : Page
{
    public EditPdfViewModel ViewModel { get; }

    public EditPdfPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<EditPdfViewModel>();
    }
}

