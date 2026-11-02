using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using PDFStudioLight.Services;
using PDFStudioLight.ViewModels;

namespace PDFStudioLight;

public partial class App : Application
{
    public static Window Window { get; private set; } = null!;
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;
    public static nint WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var logFile = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
                System.IO.File.AppendAllText(logFile, $"[AppDomain Unhandled] {DateTime.Now}: {e.ExceptionObject}\n");
            }
            catch { }
        };

        UnhandledException += (s, e) =>
        {
            try
            {
                var logFile = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
                System.IO.File.AppendAllText(logFile, $"[XAML Unhandled] {DateTime.Now}: {e.Message} \n {e.Exception}\n");
            }
            catch { }
            e.Handled = true;
        };

        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core Services
        services.AddSingleton<IPdfService, PdfService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddTransient<EditPdfViewModel>();
        services.AddTransient<MergePdfViewModel>();
        services.AddTransient<SplitPdfViewModel>();
        services.AddTransient<AddImageViewModel>();
        services.AddTransient<SignPdfViewModel>();
        services.AddTransient<CompressPdfViewModel>();
        services.AddTransient<ImagesToPdfViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<HelpViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            Window = new MainWindow();
            DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            var settingsService = Services.GetRequiredService<ISettingsService>();
            try
            {
                settingsService.LoadSettingsAsync().GetAwaiter().GetResult();
            }
            catch { }

            if (Window.Content is FrameworkElement root)
            {
                root.RequestedTheme = settingsService.Settings.Theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            Window.Activate();
        }
        catch (Exception ex)
        {
            try
            {
                var logFile = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
                System.IO.File.AppendAllText(logFile, $"[OnLaunched Exception] {DateTime.Now}: {ex}\n");
            }
            catch { }
        }
    }
}
