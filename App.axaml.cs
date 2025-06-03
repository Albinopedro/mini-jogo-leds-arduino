using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using miniJogo.Views;
using miniJogo.Models.Auth;

namespace miniJogo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var loginWindow = new LoginWindow();
        desktop.MainWindow = loginWindow;
        loginWindow.Show();

        // Wait for login completion
        loginWindow.Closed += (sender, args) =>
        {
            if (loginWindow.AuthenticatedUser != null)
            {
                // Authentication successful, show main window
                var mainWindow = new MainWindow(loginWindow.AuthenticatedUser, loginWindow.SelectedGameMode);
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                // Authentication failed or cancelled, exit application
                desktop.Shutdown();
            }
        };
    }
}