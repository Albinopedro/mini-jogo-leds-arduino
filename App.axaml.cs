using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using miniJogo.Views;
using miniJogo.Models.Auth;
using miniJogo.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace miniJogo;

public partial class App : Application
{
    private bool _isShowingLogin = false;
    private readonly object _loginLock = new object();
    private bool _hasHadSuccessfulLogin = false;
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private bool _isShuttingDown = false;
    private LoginWindow? _currentLoginWindow = null;
    private MainWindow? _currentMainWindow = null;
    private readonly List<Window> _openWindows = new List<Window>();
    private readonly object _windowsLock = new object();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Performance configurations are now handled statically via PerformanceConfig constants
        // No need for explicit configuration method calls
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;

            // Configure shutdown mode to prevent automatic shutdown when all windows close
            desktop.ShutdownRequested += (sender, e) =>
            {
                // Only allow shutdown if explicitly requested
                if (!_isShuttingDown)
                {
                    e.Cancel = true;
                    System.Diagnostics.Debug.WriteLine("Shutdown cancelado - aplicação ainda ativa");
                }
            };

            ShowLoginWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowLoginWindow()
    {
        System.Diagnostics.Debug.WriteLine($"[APP] ShowLoginWindow iniciado. Desktop: {_desktop != null}, Shutting down: {_isShuttingDown}");

        if (_desktop == null || _isShuttingDown)
        {
            System.Diagnostics.Debug.WriteLine("[APP] ShowLoginWindow cancelado - desktop null ou shutting down");
            return;
        }

        lock (_loginLock)
        {
            // Prevent multiple login windows from being created simultaneously
            if (_isShowingLogin)
            {
                System.Diagnostics.Debug.WriteLine("[APP] ShowLoginWindow chamado mas já está mostrando login - ignorando");
                return;
            }
            _isShowingLogin = true;
            System.Diagnostics.Debug.WriteLine("[APP] ShowLoginWindow flag definido como true");
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("Criando nova LoginWindow...");

            // Clean up previous login window if exists
            if (_currentLoginWindow != null)
            {
                try
                {
                    _currentLoginWindow.Closed -= OnLoginWindowClosed;
                    _currentLoginWindow.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao fechar LoginWindow anterior: {ex.Message}");
                }
                _currentLoginWindow = null;
            }

            _currentLoginWindow = new LoginWindow();
            System.Diagnostics.Debug.WriteLine("[APP] Nova LoginWindow criada");


            // Register window for tracking
            RegisterWindow(_currentLoginWindow);

            // Set as main window only if no main window exists
            if (_desktop.MainWindow == null)
            {
                _desktop.MainWindow = _currentLoginWindow;
                System.Diagnostics.Debug.WriteLine("[APP] LoginWindow definida como MainWindow");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[APP] MainWindow já existe: {_desktop.MainWindow.GetType().Name}");
            }

            // Register event handler
            _currentLoginWindow.Closed += OnLoginWindowClosed;
            _currentLoginWindow.Show();

            System.Diagnostics.Debug.WriteLine("[APP] LoginWindow mostrada com sucesso");
        }
        catch (Exception ex)
        {
            lock (_loginLock)
            {
                _isShowingLogin = false;
            }

            System.Diagnostics.Debug.WriteLine($"Erro na inicialização da LoginWindow: {ex.Message}");

            // Fallback: try one more time or shutdown
            if (!_hasHadSuccessfulLogin)
            {
                try
                {
                    var fallbackLogin = new LoginWindow();
                    if (_desktop.MainWindow == null)
                    {
                        _desktop.MainWindow = fallbackLogin;
                    }
                    fallbackLogin.Show();
                    _currentLoginWindow = fallbackLogin;
                    _currentLoginWindow.Closed += OnLoginWindowClosed;
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro crítico no fallback: {fallbackEx.Message}");
                    _desktop.Shutdown(1);
                }
            }
            else
            {
                // If we've had successful logins before, try again in a moment
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!_isShuttingDown)
                        {
                            ShowLoginWindow();
                        }
                    });
                });
            }
        }
    }    private void OnLoginWindowClosed(object? sender, EventArgs e)
    {
        if (sender is not LoginWindow loginWindow || _desktop == null || _isShuttingDown) return;

        System.Diagnostics.Debug.WriteLine("[APP] OnLoginWindowClosed chamado");

        // Background music is now handled by the LoginWindow itself via OnWindowClosing event
        // No need to create a new AudioService instance here

        // Remove event handler to prevent multiple calls
        loginWindow.Closed -= OnLoginWindowClosed;
        
        // Unregister window
        UnregisterWindow(loginWindow);

        lock (_loginLock)
        {
            _isShowingLogin = false;
            if (_currentLoginWindow == loginWindow)
            {
                _currentLoginWindow = null;
            }
        }

        if (loginWindow.AuthenticatedUser != null)
        {
            System.Diagnostics.Debug.WriteLine($"[APP] Login bem-sucedido para usuário: {loginWindow.AuthenticatedUser.Name}, Game Mode: {loginWindow.SelectedGameMode}");
            
            // Authentication successful, show main window
            try
            {
                // Clean up previous main window if exists
                if (_currentMainWindow != null)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("[APP] Fechando MainWindow anterior");
                        _currentMainWindow.Closed -= OnMainWindowClosed;
                        _currentMainWindow.Close();
                        System.Diagnostics.Debug.WriteLine("[APP] MainWindow anterior fechado");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[APP] Erro ao fechar MainWindow anterior: {ex.Message}");
                    }
                    _currentMainWindow = null;
                }

                _currentMainWindow = new MainWindow(loginWindow.AuthenticatedUser, loginWindow.SelectedGameMode);
                System.Diagnostics.Debug.WriteLine("[APP] Nova MainWindow criada");
                
                // Register window for tracking
                RegisterWindow(_currentMainWindow);
                
                // Mark that we've had at least one successful login
                _hasHadSuccessfulLogin = true;
                System.Diagnostics.Debug.WriteLine("[APP] Flag _hasHadSuccessfulLogin definido como true");

                // Set up logout handler
                _currentMainWindow.Closed += OnMainWindowClosed;
                
                // Set as main window if needed
                if (_desktop.MainWindow == loginWindow)
                {
                    _desktop.MainWindow = _currentMainWindow;
                    System.Diagnostics.Debug.WriteLine("[APP] MainWindow definida como nova MainWindow do Desktop");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] Desktop MainWindow já é outro: {_desktop.MainWindow?.GetType().Name}");
                }
                
                _currentMainWindow.Show();
                System.Diagnostics.Debug.WriteLine("[APP] MainWindow mostrada com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP] Erro ao criar MainWindow: {ex.Message}");
                HandleLoginFailure();
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[APP] Login cancelado ou falhou - AuthenticatedUser é null");
            HandleLoginFailure();
        }
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        if (_isShuttingDown || _desktop == null) return;

        System.Diagnostics.Debug.WriteLine("[APP] OnMainWindowClosed chamado - retornando ao login...");
        
        // Remove event handler to prevent multiple calls
        if (sender is MainWindow mainWindow)
        {
            mainWindow.Closed -= OnMainWindowClosed;
            UnregisterWindow(mainWindow);
            if (_currentMainWindow == mainWindow)
            {
                _currentMainWindow = null;
            }
        }

        // Always return to login after MainWindow closes (logout/session end)
        Task.Run(async () =>
        {
            await Task.Delay(500); // Increased delay to ensure cleanup
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_isShuttingDown)
                {
                    System.Diagnostics.Debug.WriteLine("[APP] Chamando ShowLoginWindow após fechamento da MainWindow");
                    ShowLoginWindow();
                }
            });
        });
    }

    private void HandleLoginFailure()
    {
        System.Diagnostics.Debug.WriteLine($"[APP] HandleLoginFailure chamado. Desktop: {_desktop != null}, Shutting down: {_isShuttingDown}, Had successful login: {_hasHadSuccessfulLogin}, Windows count: {_desktop?.Windows.Count ?? 0}");
        
        if (_desktop == null || _isShuttingDown) 
        {
            System.Diagnostics.Debug.WriteLine("[APP] HandleLoginFailure cancelado - desktop null ou shutting down");
            return;
        }

        // Only shutdown if this is the very first login attempt and user cancels
        if (!_hasHadSuccessfulLogin && _desktop.Windows.Count <= 1)
        {
            System.Diagnostics.Debug.WriteLine("[APP] Primeira tentativa de login cancelada - encerrando aplicação");
            _isShuttingDown = true;
            _desktop.Shutdown();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[APP] Login falhou/cancelado mas já houve login antes - agendando novo login em 800ms");
            // Show login again - this covers logout scenarios and failed login attempts after first use
            Task.Run(async () =>
            {
                await Task.Delay(800); // Increased delay for better stability
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!_isShuttingDown)
                    {
                        System.Diagnostics.Debug.WriteLine("[APP] Executando ShowLoginWindow após falha de login");
                        ShowLoginWindow();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[APP] ShowLoginWindow cancelado após falha - aplicação encerrando");
                    }
                });
            });
        }
    }

    private void RegisterWindow(Window window)
    {
        lock (_windowsLock)
        {
            if (!_openWindows.Contains(window))
            {
                _openWindows.Add(window);
                System.Diagnostics.Debug.WriteLine($"[APP] Window registrada: {window.GetType().Name}");
            }
        }
    }

    private void UnregisterWindow(Window window)
    {
        lock (_windowsLock)
        {
            if (_openWindows.Remove(window))
            {
                System.Diagnostics.Debug.WriteLine($"[APP] Window removida: {window.GetType().Name}");
            }
        }
    }

    private void CloseAllWindows()
    {
        lock (_windowsLock)
        {
            var windowsToClose = _openWindows.ToList();
            foreach (var window in windowsToClose)
            {
                try
                {
                    window.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[APP] Erro ao fechar {window.GetType().Name}: {ex.Message}");
                }
            }
            _openWindows.Clear();
        }
    }

    public void ForceShutdown()
    {
        if (_isShuttingDown) 
        {
            System.Diagnostics.Debug.WriteLine("[APP] ForceShutdown já em andamento, ignorando");
            return;
        }
        
        _isShuttingDown = true;
        System.Diagnostics.Debug.WriteLine("[APP] ForceShutdown iniciado");
        
        try
        {
            // Close all windows first
            System.Diagnostics.Debug.WriteLine("[APP] Fechando todas as windows registradas");
            CloseAllWindows();
            
            // Clean up current windows
            if (_currentMainWindow != null)
            {
                _currentMainWindow.Closed -= OnMainWindowClosed;
                _currentMainWindow = null;
                System.Diagnostics.Debug.WriteLine("[APP] MainWindow cleanup realizado");
            }
            
            if (_currentLoginWindow != null)
            {
                _currentLoginWindow.Closed -= OnLoginWindowClosed;
                _currentLoginWindow = null;
                System.Diagnostics.Debug.WriteLine("[APP] LoginWindow cleanup realizado");
            }
            
            // Force shutdown
            System.Diagnostics.Debug.WriteLine("[APP] Agendando shutdown do desktop em 500ms");
            Task.Run(async () =>
            {
                await Task.Delay(500); // Give time for cleanup
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[APP] Executando desktop shutdown");
                    _desktop?.Shutdown();
                });
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[APP] Erro durante ForceShutdown: {ex.Message}");
            Environment.Exit(1);
        }
    }
}