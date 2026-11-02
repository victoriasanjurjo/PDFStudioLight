using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class AddImagePage : Page
{
    public AddImageViewModel ViewModel { get; }

    public AddImagePage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AddImageViewModel>();
    }
}

