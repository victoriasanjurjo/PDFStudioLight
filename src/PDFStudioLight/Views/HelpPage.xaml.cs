using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class HelpPage : Page
{
    public HelpViewModel ViewModel { get; }

    public HelpPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<HelpViewModel>();
    }
}

