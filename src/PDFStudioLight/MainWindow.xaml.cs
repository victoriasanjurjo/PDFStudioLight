using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace PDFStudioLight;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Title = "PDF Studio Light";

        try
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }
        catch { }

        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
        catch { }

        try
        {
            AppWindow.Title = "PDF Studio Light";
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
        }
        catch { }

        RootFrame.Navigate(typeof(MainPage));
    }
}
