using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using miniJogo.Models.Auth;
using miniJogo.Services;
using miniJogo.Models;

namespace miniJogo.Views
{
    public partial class VictoryWindow : Window
    {
        private System.Threading.Timer? _autoReturnTimer;
        private System.Threading.Timer? _fullscreenWatchdog;
        private volatile bool _isReturning = false;
        private readonly object _returnLock = new object();
        
        public event EventHandler? OnReturnToLogin;

        public VictoryWindow()
        {
            InitializeComponent();
            
            var msg = "VictoryWindow inicializada";
            System.Diagnostics.Debug.WriteLine(msg);
            Console.WriteLine(msg);
            
            // Set initial window properties for proper fullscreen display
            WindowState = WindowState.FullScreen;
            Topmost = true;
            CanResize = false;
            ShowInTaskbar = false;
            
            // Force fullscreen immediately
            ForceFullscreenMode();
            
            // No auto-return timer - victory should stay until user clicks
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

        public void SetVictoryDetails(GameMode gameMode, int finalScore, string playerName, string challenge)
        {
            try
            {
                var msg = $"Configurando detalhes de vitória para jogador: {playerName}, Jogo: {gameMode}, Pontuação: {finalScore}";
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
                
                PlayerNameText.Text = $"👤 Jogador: {playerName}";
                
                VictoryDetailsPanel.Children.Clear();
                
                // Game completed info
                var gameCompletedText = new TextBlock
                {
                    Text = $"🎮 {GetGameDisplayName(gameMode)}",
                    FontSize = 24,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 0, 15)
                };
                VictoryDetailsPanel.Children.Add(gameCompletedText);
                
                // Challenge completed
                var challengeText = new TextBlock
                {
                    Text = $"✅ {challenge}",
                    FontSize = 20,
                    FontWeight = FontWeight.Medium,
                    Foreground = new SolidColorBrush(Colors.White),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 0, 0, 15)
                };
                VictoryDetailsPanel.Children.Add(challengeText);
                
                // Final score
                var scoreText = new TextBlock
                {
                    Text = $"🏆 Pontuação Final: {finalScore}",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 0, 20)
                };
                VictoryDetailsPanel.Children.Add(scoreText);
                
                // Congratulations message
                var congratsText = new TextBlock
                {
                    Text = "🎉 Parabéns! Você conquistou este desafio com sucesso!",
                    FontSize = 18,
                    FontWeight = FontWeight.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                };
                VictoryDetailsPanel.Children.Add(congratsText);
                
            }
            catch (Exception ex)
            {
                var errorMsg = $"Erro ao configurar detalhes de vitória: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine(errorMsg);
            }
        }

        private string GetGameDisplayName(GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.PegaLuz => "🎯 Pega-Luz",
                GameMode.SequenciaMaluca => "🧠 Sequência Maluca",
                GameMode.GatoRato => "🐱 Gato e Rato",
                GameMode.EsquivaMeteoros => "☄️ Esquiva Meteoros",
                GameMode.GuitarHero => "🎸 Guitar Hero",
                GameMode.LightningStrike => "⚡ Lightning Strike",
                GameMode.SniperMode => "🎯 Sniper Mode",
                _ => "🎮 Jogo Desconhecido"
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
                
                // Dispose timer to prevent multiple calls
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
                            System.Diagnostics.Debug.WriteLine("Fechando VictoryWindow...");
                            Close();
                        }
                        catch (Exception closeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erro ao fechar VictoryWindow: {closeEx.Message}");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                var errorMsg = $"Erro ao retornar ao login: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                Console.WriteLine(errorMsg);
                
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
