using CommunityToolkit.Mvvm.ComponentModel;

namespace PDFStudioLight.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    [ObservableProperty]
    private string _version = "2.0 (C# WinUI 3)";

    [ObservableProperty]
    private string _privacyStatement =
        "PDF Studio Light opera con el 100% de procesamiento en tu equipo local (Offline). " +
        "No recopilamos telemetría, no enviamos tus documentos a la nube ni realizamos conexiones externas no autorizadas. " +
        "Tu información y tus documentos PDF permanecen siempre bajo tu total custodia y privacidad.";
}

