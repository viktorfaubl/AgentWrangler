using AgentWrangler.Services;
using System.Threading.Tasks;

using System;

using Avalonia;

namespace AgentWrangler.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Logger.LogError(ex, "UnhandledException");
            else
                Logger.LogError(e.ExceptionObject?.ToString() ?? "Unknown error", "UnhandledException");
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Logger.LogError(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        };
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
