using System;
using Microsoft.UI.Xaml.Controls;
using PDFStudioLight.Views;

namespace PDFStudioLight;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void NavView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Navegar a la primera herramienta por defecto
        if (NavView.MenuItems.Count > 0 && NavView.MenuItems[0] is NavigationViewItem firstItem)
        {
            NavView.SelectedItem = firstItem;
            NavigateTo("EditPdf");
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        Type targetType = tag switch
        {
            "EditPdf" => typeof(EditPdfPage),
            "MergePdf" => typeof(MergePdfPage),
            "SplitPdf" => typeof(SplitPdfPage),
            "AddImage" => typeof(AddImagePage),
            "SignPdf" => typeof(SignPdfPage),
            "Compress" => typeof(CompressPage),
            "ImagesToPdf" => typeof(ImagesToPdfPage),
            "Help" => typeof(HelpPage),
            _ => typeof(EditPdfPage)
        };

        if (ContentFrame.CurrentSourcePageType != targetType)
        {
            ContentFrame.Navigate(targetType);
        }
    }
}
