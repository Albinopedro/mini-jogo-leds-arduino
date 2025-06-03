using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using miniJogo.Views;
using miniJogo.Models.Auth;
using System;
using System.Threading.Tasks;

namespace miniJogo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Aplicar apenas configurações básicas de performance
        try
        {
            PerformanceConfig.Configure();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro na configuração de performance: {ex.Message}");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
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
        catch (Exception ex)
        {
            // Log erro e continuar execução
            System.Diagnostics.Debug.WriteLine($"Erro na inicialização: {ex.Message}");
            
            // Fallback: mostrar login básico
            try
            {
                var loginWindow = new LoginWindow();
                desktop.MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"Erro crítico: {fallbackEx.Message}");
                desktop.Shutdown(1);
            }
        }
    }
}