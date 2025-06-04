using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using miniJogo.Models;

namespace miniJogo.Views
{
    public partial class RoundsCompletedWindow : Window
    {
        private System.Threading.Timer? _autoReturnTimer;
        private volatile bool _isReturning = false;
        private readonly object _returnLock = new object();
        
        public event EventHandler? OnReturnToLogin;

        public RoundsCompletedWindow()
        {
            InitializeComponent();
            
            var msg = "RoundsCompletedWindow inicializada";
            System.Diagnostics.Debug.WriteLine(msg);
            Console.WriteLine(msg);
            
            // Set initial window properties
            WindowState = WindowState.FullScreen;
            Topmost = true;
            CanResize = false;
            
            // Start auto-return timer (8 seconds - increased for better UX)
            _autoReturnTimer = new System.Threading.Timer(
                callback: _ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => TriggerReturnToLogin()),
                state: null,
                dueTime: TimeSpan.FromSeconds(8),
                period: System.Threading.Timeout.InfiniteTimeSpan
            );
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
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(30, 20),
                Margin = new Avalonia.Thickness(0, 10)
            };

            var mainStack = new StackPanel
            {
                Spacing = 15
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
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            // Errors info with max errors
            var maxErrors = GetMaxErrorsForGame(gameMode);
            var errorsText = new TextBlock
            {
                Text = $"❌ Erros cometidos: {errorsCommitted}/{maxErrors}",
                FontSize = 18,
                Foreground = errorsCommitted >= maxErrors ? new SolidColorBrush(Color.FromRgb(229, 62, 62)) : new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = FontWeight.Medium
            };

            // Session result
            var resultText = new TextBlock
            {
                Text = errorsCommitted >= maxErrors ? "🔴 Sessão finalizada - Limite atingido" : "🟢 Sessão completa",
                FontSize = 16,
                Foreground = errorsCommitted >= maxErrors ? new SolidColorBrush(Color.FromRgb(229, 62, 62)) : new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = FontWeight.Medium
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
                
                // Dispose timer first to prevent multiple calls
                if (_autoReturnTimer != null)
                {
                    _autoReturnTimer.Dispose();
                    _autoReturnTimer = null;
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

                // Cleanup timer
                if (_autoReturnTimer != null)
                {
                    _autoReturnTimer.Dispose();
                    _autoReturnTimer = null;
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
            
            // Force fullscreen state consistency
            try
            {
                WindowState = WindowState.FullScreen;
                Topmost = true;
                
                // Focus this window to ensure it's in front
                Focus();
                Activate();
                
                var msg = "RoundsCompletedWindow aberta em fullscreen";
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao configurar fullscreen: {ex.Message}");
            }
        }
    }
}
