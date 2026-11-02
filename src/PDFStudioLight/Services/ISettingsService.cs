using System.Threading.Tasks;
using PDFStudioLight.Models;

namespace PDFStudioLight.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
}

