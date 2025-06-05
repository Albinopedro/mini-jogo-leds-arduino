using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using miniJogo.Models;

namespace miniJogo.Views
{
    public partial class RoundsCompletedWindow : Window
    {
        private System.Threading.Timer? _autoReturnTimer;
        private System.Threading.Timer? _fullscreenWatchdog;
        private volatile bool _isReturning = false;
        private readonly object _returnLock = new object();
        
        public event EventHandler? OnReturnToLogin;

        public RoundsCompletedWindow()
        {
            InitializeComponent();
            
            var msg = "RoundsCompletedWindow inicializada";
            System.Diagnostics.Debug.WriteLine(msg);
            Console.WriteLine(msg);
            
            // Set initial window properties for proper fullscreen display
            WindowState = WindowState.FullScreen;
            Topmost = true;
            CanResize = false;
            ShowInTaskbar = false;
            
            // Force fullscreen immediately
            ForceFullscreenMode();
            
            // Start auto-return timer (10 seconds - increased for better UX)
            _autoReturnTimer = new System.Threading.Timer(
                callback: _ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => TriggerReturnToLogin()),
                state: null,
                dueTime: TimeSpan.FromSeconds(10),
                period: System.Threading.Timeout.InfiniteTimeSpan
            );
            
            // Start fullscreen watchdog timer (checks every 2 seconds)
            _fullscreenWatchdog = new System.Threading.Timer(
                callback: _ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => EnsureFullscreen()),
                state: null,
                dueTime: TimeSpan.FromSeconds(1),
                period: TimeSpan.FromSeconds(1)
            );
            
            // Configure window events
            Deactivated += OnWindowDeactivated;
        }

        public void SetGameSummary(Dictionary<GameMode, int> roundsPlayed, string playerName)
        {
            try
            {
                var msg = $"SetGameSummary chamado para jogador: {playerName}";
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
                
                PlayerNameText.Text = $"👤 Jogador: {playerName}";
                
                GamesSummaryPanel.Children.Clear();
                
                // For single game sessions, show the one game played
                foreach (var game in roundsPlayed)
                {
                    if (game.Key == GameMode.Menu) continue;
                    
                    var gamePanel = CreateSingleGameSummaryItem(game.Key, game.Value);
                    GamesSummaryPanel.Children.Add(gamePanel);
                    
                    // Add completion message
                    var completionText = new TextBlock
                    {
                        Text = "🏁 Sessão de jogo único completa!",
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 20, 0, 0)
                    };
                    GamesSummaryPanel.Children.Add(completionText);
                    
                    break; // Only show the first (and only) game
                }
                
                // If no games were played, show message
                if (GamesSummaryPanel.Children.Count == 0)
                {
                    var noGamesText = new TextBlock
                    {
                        Text = "Nenhum jogo foi jogado nesta sessão.",
                        FontSize = 18,
                        Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192)),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    };
                    GamesSummaryPanel.Children.Add(noGamesText);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Erro ao configurar resumo: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine(errorMsg);
            }
        }

        private Border CreateSingleGameSummaryItem(GameMode gameMode, int errorsCommitted)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                CornerRadius = new Avalonia.CornerRadius(15),
                Padding = new Avalonia.Thickness(30, 20),
                Margin = new Avalonia.Thickness(15, 10),
                MinWidth = 600,
                BorderBrush = new SolidColorBrush(Color.FromRgb(113, 128, 150)),
                BorderThickness = new Avalonia.Thickness(2)
            };

            var mainStack = new StackPanel
            {
                Spacing = 15,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Game header
            var gameHeader = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 15,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var icon = new TextBlock
            {
                Text = gameMode.GetIcon(),
                FontSize = 32,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var name = new TextBlock
            {
                Text = gameMode.GetDisplayName(),
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            gameHeader.Children.Add(icon);
            gameHeader.Children.Add(name);

            // Game description
            var description = new TextBlock
            {
                Text = gameMode.GetDescription(),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 500,
                TextAlignment = Avalonia.Media.TextAlignment.Center
            };

            // Errors info with max errors
            var maxErrors = GetMaxErrorsForGame(gameMode);
            var errorsText = new TextBlock
            {
                Text = $"❌ Erros cometidos: {errorsCommitted}/{maxErrors}",
                FontSize = 18,
                Foreground = errorsCommitted >= maxErrors ? new SolidColorBrush(Color.FromRgb(229, 62, 62)) : new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold
            };

            // Session result
            var resultText = new TextBlock
            {
                Text = errorsCommitted >= maxErrors ? "🔴 Sessão finalizada - Limite atingido" : "🟢 Sessão completa",
                FontSize = 16,
                Foreground = errorsCommitted >= maxErrors ? new SolidColorBrush(Color.FromRgb(229, 62, 62)) : new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold
            };

            mainStack.Children.Add(gameHeader);
            mainStack.Children.Add(description);
            mainStack.Children.Add(errorsText);
            mainStack.Children.Add(resultText);

            border.Child = mainStack;
            return border;
        }

        private int GetMaxErrorsForGame(GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.PegaLuz => 3,
                GameMode.GatoRato => 3,
                GameMode.SequenciaMaluca => 3,
                GameMode.EsquivaMeteoros => 3,
                GameMode.GuitarHero => 3,
                GameMode.RoletaRussa => 3,
                GameMode.LightningStrike => 3,
                GameMode.SniperMode => 3,
                _ => 3
            };
        }

        private void ReturnToLoginButton_Click(object? sender, RoutedEventArgs e)
        {
            TriggerReturnToLogin();
        }

        private void TriggerReturnToLogin()
        {
            try
            {
                // Prevent multiple calls
                lock (_returnLock)
                {
                    if (_isReturning)
                    {
                        System.Diagnostics.Debug.WriteLine("TriggerReturnToLogin já em andamento, ignorando");
                        return;
                    }
                    _isReturning = true;
                }

                var msg = "TriggerReturnToLogin chamado - retornando ao login";
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
                
                // Dispose timers first to prevent multiple calls
                if (_autoReturnTimer != null)
                {
                    _autoReturnTimer.Dispose();
                    _autoReturnTimer = null;
                }
                
                if (_fullscreenWatchdog != null)
                {
                    _fullscreenWatchdog.Dispose();
                    _fullscreenWatchdog = null;
                }
                
                // Trigger event before closing to ensure proper cleanup
                try
                {
                    OnReturnToLogin?.Invoke(this, EventArgs.Empty);
                    System.Diagnostics.Debug.WriteLine("OnReturnToLogin event disparado com sucesso");
                }
                catch (Exception eventEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao disparar OnReturnToLogin: {eventEx.Message}");
                }
                
                // Small delay to allow event processing
                _ = Task.Delay(200).ContinueWith(_ => 
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => 
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("Fechando RoundsCompletedWindow...");
                            Close();
                        }
                        catch (Exception closeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erro ao fechar RoundsCompletedWindow: {closeEx.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                var errorMsg = $"Erro ao retornar ao login: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine(errorMsg);
                
                // Ensure cleanup happens even on error
                try
                {
                    _autoReturnTimer?.Dispose();
                    _autoReturnTimer = null;
                    
                    // Force close as last resort
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            Close();
                        }
                        catch (Exception closeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erro crítico ao fechar: {closeEx.Message}");
                        }
                    });
                }
                catch (Exception finalEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro final em TriggerReturnToLogin: {finalEx.Message}");
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Mark as returning to prevent further actions
                lock (_returnLock)
                {
                    _isReturning = true;
                }

                // Cleanup timers
                if (_autoReturnTimer != null)
                {
                    _autoReturnTimer.Dispose();
                    _autoReturnTimer = null;
                }
                
                if (_fullscreenWatchdog != null)
                {
                    _fullscreenWatchdog.Dispose();
                    _fullscreenWatchdog = null;
                }
                
                System.Diagnostics.Debug.WriteLine("RoundsCompletedWindow fechada e recursos limpos");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro durante cleanup da RoundsCompletedWindow: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            
            System.Diagnostics.Debug.WriteLine("🖼️ RoundsCompletedWindow OnOpened - aplicando fullscreen");
            
            // Force fullscreen state consistency multiple times
            ForceFullscreenMode();
            
            // Additional attempts to ensure proper fullscreen
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ForceFullscreenMode());
                
                await Task.Delay(100);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ForceFullscreenMode());
                
                await Task.Delay(250);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ForceFullscreenMode());
            });
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            
            if (!_isReturning)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ RoundsCompletedWindow perdeu foco - reforçando fullscreen");
                
                // Delay slightly then refocus
                _ = Task.Run(async () =>
                {
                    await Task.Delay(100);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!_isReturning)
                        {
                            ForceFullscreenMode();
                            Focus();
                            Activate();
                        }
                    });
                });
            }
        }
        
        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (!_isReturning)
            {
                System.Diagnostics.Debug.WriteLine("⚠️ RoundsCompletedWindow foi desativada - reativando");
                
                // Delay slightly then reactivate
                _ = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (!_isReturning)
                        {
                            Activate();
                            Focus();
                        }
                    });
                });
            }
        }

        private void EnsureFullscreen()
        {
            try
            {
                if (_isReturning) return; // Don't force fullscreen if we're closing
                
                if (WindowState != WindowState.FullScreen)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ RoundsCompletedWindow saiu do fullscreen - reforçando");
                    Console.WriteLine("⚠️ RoundsCompletedWindow saiu do fullscreen - reforçando");
                    ForceFullscreenMode();
                }
                else
                {
                    // Verify position and size even if WindowState is correct
                    var screens = Screens;
                    if (screens != null && screens.All?.Count > 0)
                    {
                        var primaryScreen = screens.All.First();
                        var screenBounds = primaryScreen.Bounds;
                        
                        if (Position.X != screenBounds.X || Position.Y != screenBounds.Y ||
                            Width != screenBounds.Width || Height != screenBounds.Height)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ RoundsCompletedWindow posição/tamanho incorreto - corrigindo: {Position.X},{Position.Y} {Width}x{Height} -> {screenBounds.X},{screenBounds.Y} {screenBounds.Width}x{screenBounds.Height}");
                            ForceFullscreenMode();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no watchdog fullscreen: {ex.Message}");
            }
        }

        private void ForceFullscreenMode()
        {
            try
            {
                // Set window properties in correct order
                SystemDecorations = Avalonia.Controls.SystemDecorations.None;
                CanResize = false;
                ShowInTaskbar = false;
                Topmost = true;
                
                // Get actual screen dimensions
                var screens = Screens;
                if (screens != null && screens.All?.Count > 0)
                {
                    var primaryScreen = screens.All.First();
                    var screenBounds = primaryScreen.Bounds;
                    
                    // Set position first
                    Position = new Avalonia.PixelPoint(screenBounds.X, screenBounds.Y);
                    
                    // Then set size
                    Width = screenBounds.Width;
                    Height = screenBounds.Height;
                    
                    System.Diagnostics.Debug.WriteLine($"Tela detectada: {screenBounds.Width}x{screenBounds.Height} em ({screenBounds.X}, {screenBounds.Y})");
                }
                else
                {
                    // Fallback to common fullscreen resolution
                    Position = new Avalonia.PixelPoint(0, 0);
                    Width = 1920;
                    Height = 1080;
                    System.Diagnostics.Debug.WriteLine("Usando resolução fallback: 1920x1080");
                }
                
                // Force fullscreen state AFTER setting position and size
                WindowState = WindowState.FullScreen;
                
                // Focus this window to ensure it's in front
                Focus();
                Activate();
                
                var msg = $"RoundsCompletedWindow forçada para fullscreen - {Width}x{Height}";
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao forçar fullscreen: {ex.Message}");
                
                // Fallback configuration
                try
                {
                    Position = new Avalonia.PixelPoint(0, 0);
                    Width = 1920;
                    Height = 1080;
                    WindowState = WindowState.FullScreen;
                    Topmost = true;
                    ShowInTaskbar = false;
                    SystemDecorations = Avalonia.Controls.SystemDecorations.None;
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro no fallback fullscreen: {fallbackEx.Message}");
                }
            }
        }
    }
}
