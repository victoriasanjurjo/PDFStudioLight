using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using PDFStudioLight.Services;

namespace PDFStudioLight.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private int _themeIndex; // 0 = Default, 1 = Light, 2 = Dark

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;

        OutputDirectory = _settingsService.Settings.OutputDirectory;
        ThemeIndex = _settingsService.Settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
    }

    [RelayCommand]
    public async Task ChangeOutputDirectoryAsync()
    {
        var folder = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            OutputDirectory = folder;
            _settingsService.Settings.OutputDirectory = folder;
            await _settingsService.SaveSettingsAsync();
            StatusMessage = "Carpeta de salida actualizada.";
        }
    }

    [RelayCommand]
    public void OpenOutputDirectory()
    {
        if (string.IsNullOrWhiteSpace(OutputDirectory)) return;

        try
        {
            var fullPath = Path.GetFullPath(OutputDirectory);
            if (Directory.Exists(fullPath))
            {
                // Invocación segura explícita de explorer.exe sin ShellExecute arbitrario
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al abrir directorio seguro: {ex.Message}");
        }
    }

    public async Task SetThemeAsync(int index)
    {
        ThemeIndex = index;
        _settingsService.Settings.Theme = index switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "Default"
        };

        await _settingsService.SaveSettingsAsync();

        if (App.Window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = index switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }

        StatusMessage = "Tema visual actualizado.";
    }
}

