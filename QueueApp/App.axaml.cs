using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace QueueApp;

public partial class App : Application
{
    private QueueManager? queueManager;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            
            // Initialize queue manager
            queueManager = new QueueManager(mainWindow);
            
            // Handle application shutdown
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
            desktop.Exit += (sender, e) =>
            {
                queueManager?.StopAutoAdvance();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}