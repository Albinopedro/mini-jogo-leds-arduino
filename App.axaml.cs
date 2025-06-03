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
        
        // Aplicar configurações de performance
        PerformanceConfig.Configure();
        
        // Iniciar monitoramento de performance
        PerformanceConfig.StartPerformanceMonitoring();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Configurar responsividade da UI
            PerformanceConfig.ConfigureUIResponsiveness();
            
            ShowLoginWindow(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void ShowLoginWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            // Carregar dados de forma assíncrona para não bloquear a UI
            await Task.Run(() =>
            {
                // Pré-carregar dados necessários
                PerformanceConfig.ValidatePerformance();
            });

            var loginWindow = new LoginWindow();
            
            // Aplicar otimizações específicas para controles de dados
            PerformanceConfig.ConfigureDataControls(loginWindow);
            
            desktop.MainWindow = loginWindow;
            loginWindow.Show();

            // Wait for login completion
            loginWindow.Closed += async (sender, args) =>
            {
                if (loginWindow.AuthenticatedUser != null)
                {
                    // Authentication successful, show main window
                    var mainWindow = new MainWindow(loginWindow.AuthenticatedUser, loginWindow.SelectedGameMode);
                    
                    // Aplicar otimizações para a janela principal
                    PerformanceConfig.ConfigureDataControls(mainWindow);
                    
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    
                    // Otimizar uso de memória após carregamento
                    await Task.Delay(1000); // Aguardar carregamento completo
                    PerformanceConfig.OptimizeMemoryUsage();
                    
                    // Validar performance após inicialização
                    PerformanceConfig.ValidatePerformance();
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
            
            // Fallback: mostrar login sem otimizações
            var loginWindow = new LoginWindow();
            desktop.MainWindow = loginWindow;
            loginWindow.Show();
        }
    }
}