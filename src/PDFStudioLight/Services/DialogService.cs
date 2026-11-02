using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace PDFStudioLight.Services;

/// <summary>
/// Provee integración con selectores de archivos, carpetas y diálogos de mensaje en WinUI 3.
/// </summary>
public class DialogService : IDialogService
{
    public async Task<string?> PickSingleFileAsync(string pickerTitle, params string[] extensions)
    {
        var openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, App.WindowHandle);

        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        if (extensions != null && extensions.Length > 0)
        {
            foreach (var ext in extensions)
            {
                var formatted = ext.StartsWith('.') ? ext : "." + ext;
                openPicker.FileTypeFilter.Add(formatted);
            }
        }
        else
        {
            openPicker.FileTypeFilter.Add("*");
        }

        var file = await openPicker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<IReadOnlyList<string>> PickMultipleFilesAsync(string pickerTitle, params string[] extensions)
    {
        var openPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(openPicker, App.WindowHandle);

        openPicker.ViewMode = PickerViewMode.List;
        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        if (extensions != null && extensions.Length > 0)
        {
            foreach (var ext in extensions)
            {
                var formatted = ext.StartsWith('.') ? ext : "." + ext;
                openPicker.FileTypeFilter.Add(formatted);
            }
        }
        else
        {
            openPicker.FileTypeFilter.Add("*");
        }

        var files = await openPicker.PickMultipleFilesAsync();
        return files.Select(f => f.Path).ToList();
    }

    public async Task<string?> PickSaveFileAsync(string defaultFileName, string extensionDescription, string extension)
    {
        var savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, App.WindowHandle);

        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        var ext = extension.StartsWith('.') ? extension : "." + extension;

        savePicker.FileTypeChoices.Add(extensionDescription, new List<string>() { ext });
        savePicker.SuggestedFileName = defaultFileName;

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickFolderAsync()
    {
        var folderPicker = new FolderPicker();
        InitializeWithWindow.Initialize(folderPicker, App.WindowHandle);

        folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        folderPicker.FileTypeFilter.Add("*");

        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public async Task ShowMessageAsync(string title, string content)
    {
        if (App.Window?.Content?.XamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "Entendido",
            XamlRoot = App.Window.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string title, string content, string primaryButtonText = "Aceptar", string cancelButtonText = "Cancelar")
    {
        if (App.Window?.Content?.XamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = cancelButtonText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.Window.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}

