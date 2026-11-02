using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }

    private async void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is RadioButtons rb && rb.SelectedIndex >= 0)
        {
            await ViewModel.SetThemeAsync(rb.SelectedIndex);
        }
    }
}

