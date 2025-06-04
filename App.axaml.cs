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
            
            // Keep the first window as main window to prevent app shutdown
            if (desktop.MainWindow == null)
            {
                desktop.MainWindow = loginWindow;
            }
            
            loginWindow.Show();

            // Wait for login completion
            loginWindow.Closed += (sender, args) =>
            {
                if (loginWindow.AuthenticatedUser != null)
                {
                    // Authentication successful, show main window
                    var mainWindow = new MainWindow(loginWindow.AuthenticatedUser, loginWindow.SelectedGameMode);
                    
                    // Set up logout handler to return to login instead of shutting down
                    mainWindow.Closed += (mainSender, mainArgs) =>
                    {
                        // When main window closes, show login again instead of shutting down
                        Task.Run(async () =>
                        {
                            await Task.Delay(100); // Small delay to ensure cleanup
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ShowLoginWindow(desktop);
                            });
                        });
                    };
                    
                    mainWindow.Show();
                }
                else
                {
                    // Authentication failed or cancelled - show login again instead of exit
                    // Only shutdown if this is the first time (no previous windows)
                    if (desktop.Windows.Count <= 1)
                    {
                        desktop.Shutdown();
                    }
                    else
                    {
                        // There are other windows, just show login again
                        Task.Run(async () =>
                        {
                            await Task.Delay(500);
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ShowLoginWindow(desktop);
                            });
                        });
                    }
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
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = loginWindow;
                }
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