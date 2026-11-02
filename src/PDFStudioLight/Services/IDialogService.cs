using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDFStudioLight.Services;

public interface IDialogService
{
    Task<string?> PickSingleFileAsync(string pickerTitle, params string[] extensions);
    Task<IReadOnlyList<string>> PickMultipleFilesAsync(string pickerTitle, params string[] extensions);
    Task<string?> PickSaveFileAsync(string defaultFileName, string extensionDescription, string extension);
    Task<string?> PickFolderAsync();
    Task ShowMessageAsync(string title, string content);
    Task<bool> ShowConfirmationAsync(string title, string content, string primaryButtonText = "Aceptar", string cancelButtonText = "Cancelar");
}

