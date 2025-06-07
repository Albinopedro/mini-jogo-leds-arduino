using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using miniJogo.Models.Auth;
using miniJogo.Services;
using miniJogo.Models;

namespace miniJogo.Views
{
    public partial class LoginWindow : Window
    {
        private AuthService _authService;
        private ScoreService _scoreService;
        private AudioService _audioService;
        private User? _currentUser;
        private int _selectedGameMode = 1;
        private bool _isFullScreen = true;

        public User? AuthenticatedUser => _currentUser;
        public int SelectedGameMode => _selectedGameMode;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
            _scoreService = new ScoreService();
            _audioService = new AudioService();
            InitializeGameModeSelector();
            LoadRankings();

            // Subscribe to score saved events to refresh rankings
            _scoreService.ScoreSaved += OnScoreSaved;

            // Start in fullscreen
            WindowState = WindowState.FullScreen;
            _isFullScreen = true;

            // Focus on name field initially
            NameTextBox.Focus();

            // Subscribe to closing event to stop music
            Closing += OnWindowClosing;

            // Start background music playlist when login window opens
            _ = Task.Run(async () =>
            {
                Console.WriteLine("🎵 LoginWindow criada - iniciando playlist de música de fundo...");
                await _audioService.StartBackgroundMusicAsync();
                Console.WriteLine("🎵 Playlist de música de fundo iniciada no LoginWindow!");
            });
        }

        private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            try
            {
                // Unsubscribe from events
                _scoreService.ScoreSaved -= OnScoreSaved;

                Console.WriteLine("🎵 LoginWindow fechando - parando música de fundo...");
                await _audioService.StopBackgroundMusicAsync();
                Console.WriteLine("🎵 Música de fundo parada!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao parar música: {ex.Message}");
            }
        }

        private void InitializeGameModeSelector()
        {
            // Create visual game cards
            CreateGameCards();

            // Set default game mode
            _selectedGameMode = 1;
            UpdateGameInstructions(1);
        }

        private void CreateGameCards()
        {
            var games = new[]
            {
                new { Mode = 1, Icon = "🎯", Name = "Pega-Luz", Challenge = "Alcance 400 pontos", Difficulty = "Difícil" },
                new { Mode = 2, Icon = "🧠", Name = "Sequência Maluca", Challenge = "Complete 8 rodadas", Difficulty = "Médio" },
                new { Mode = 3, Icon = "🐱", Name = "Gato e Rato", Challenge = "Capture 16 vezes", Difficulty = "Difícil" },
                new { Mode = 4, Icon = "☄️", Name = "Esquiva Meteoros", Challenge = "Sobreviva 180 segundos", Difficulty = "Médio" },
                new { Mode = 5, Icon = "🎸", Name = "Guitar Hero", Challenge = "Faça 300 pontos", Difficulty = "Difícil" },
                new { Mode = 6, Icon = "⚡", Name = "Lightning Strike", Challenge = "Complete 7 rodadas", Difficulty = "Muito Difícil" },
                new { Mode = 7, Icon = "🎯", Name = "Sniper Mode", Challenge = "Acerte 7 alvos", Difficulty = "Muito Difícil" }
            };

            GameCardsPanel.Children.Clear();

            foreach (var game in games)
            {
                var card = CreateGameCard(game.Mode, game.Icon, game.Name, game.Challenge, game.Difficulty);
                GameCardsPanel.Children.Add(card);
            }
        }

        private Border CreateGameCard(int gameMode, string icon, string name, string challenge, string difficulty)
        {
            var isSelected = gameMode == _selectedGameMode;

            var card = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(isSelected ? Color.FromRgb(6, 95, 70) : Color.FromRgb(58, 58, 107), 0),
                        new GradientStop(isSelected ? Color.FromRgb(4, 120, 87) : Color.FromRgb(74, 74, 123), 1)
                    }
                },
                BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(16, 185, 129) : Color.FromRgb(94, 96, 206)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 2, 0, 2),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = gameMode
            };

            // Add subtle shadow effect
            card.Effect = new DropShadowEffect
            {
                BlurRadius = 15,
                OffsetX = 0,
                OffsetY = 5,
                Color = Color.FromArgb(40, 0, 0, 0)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            // Icon
            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconText, 0);

            // Name and Challenge
            var infoStack = new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(12, 0, 12, 0)
            };

            var nameText = new TextBlock
            {
                Text = name,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };

            var challengeText = new TextBlock
            {
                Text = challenge,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240))
            };

            infoStack.Children.Add(nameText);
            infoStack.Children.Add(challengeText);
            Grid.SetColumn(infoStack, 1);

            // Difficulty Badge
            var difficultyBorder = new Border
            {
                Background = GetDifficultyBackgroundColor(difficulty),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 3, 7, 3)
            };

            var difficultyText = new TextBlock
            {
                Text = difficulty,
                FontSize = 12,
                FontWeight = FontWeight.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            difficultyBorder.Child = difficultyText;
            Grid.SetColumn(difficultyBorder, 2);

            grid.Children.Add(iconText);
            grid.Children.Add(infoStack);
            grid.Children.Add(difficultyBorder);

            card.Child = grid;

            // Click handler
            card.PointerPressed += (sender, e) => GameCard_Click(gameMode);
            card.PointerEntered += (sender, e) =>
            {
                _audioService.PlaySound(AudioEvent.ButtonHover);
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));

                // Add slight scale transformation on hover
                card.RenderTransform = new ScaleTransform(1.03, 1.03);
                card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            };
            card.PointerExited += (sender, e) =>
            {
                if (_selectedGameMode != gameMode)
                {
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(94, 96, 206));
                    card.Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromRgb(58, 58, 107), 0),
                            new GradientStop(Color.FromRgb(74, 74, 123), 1)
                        }
                    };
                }
                // Reset transform
                card.RenderTransform = new ScaleTransform(1, 1);
            };

            return card;
        }

        private SolidColorBrush GetDifficultyColor(string difficulty)
        {
            return difficulty switch
            {
                "Fácil" => new SolidColorBrush(Color.FromRgb(72, 187, 120)),
                "Médio" => new SolidColorBrush(Color.FromRgb(246, 224, 94)),
                "Difícil" => new SolidColorBrush(Color.FromRgb(251, 146, 60)),
                "Muito Difícil" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => new SolidColorBrush(Color.FromRgb(160, 174, 192))
            };
        }

        private SolidColorBrush GetDifficultyBackgroundColor(string difficulty)
        {
            return difficulty switch
            {
                "Fácil" => new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                "Médio" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "Difícil" => new SolidColorBrush(Color.FromRgb(221, 107, 32)),
                "Muito Difícil" => new SolidColorBrush(Color.FromRgb(224, 36, 36)),
                _ => new SolidColorBrush(Color.FromRgb(113, 128, 150))
            };
        }

        private void GameCard_Click(int gameMode)
        {
            _audioService.PlaySound(AudioEvent.ButtonClick);
            _selectedGameMode = gameMode;

            // Update visual selection
            foreach (Border card in GameCardsPanel.Children)
            {
                if (card.Tag != null && (int)card.Tag == gameMode)
                {
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    card.Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromRgb(6, 95, 70), 0),
                            new GradientStop(Color.FromRgb(16, 185, 129), 1)
                        }
                    };
                }
                else
                {
                    card.BorderBrush = new SolidColorBrush(Color.FromRgb(94, 96, 206));
                    card.Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromRgb(58, 58, 107), 0),
                            new GradientStop(Color.FromRgb(74, 74, 123), 1)
                        }
                    };
                }
            }

            UpdateGameInstructions(gameMode);
            UpdateSelectedGameDisplay(gameMode);
            UpdateLoginButtonState();
        }

        private void UpdateSelectedGameDisplay(int gameMode)
        {
            var gameInfo = GetGameInfo(gameMode);
            SelectedGameTitle.Text = $"{gameInfo.Icon} {gameInfo.Name}";
            SelectedGameChallenge.Text = $"Desafio: {gameInfo.Challenge}";
            SelectedGameBorder.IsVisible = true;
        }

        private (string Icon, string Name, string Challenge) GetGameInfo(int gameMode)
        {
            return gameMode switch
            {
                1 => ("🎯", "Pega-Luz", "Alcance 400 pontos com reflexos ultra-rápidos"),
                2 => ("🧠", "Sequência Maluca", "Complete 8 rodadas sem errar (sequência chega a 10 passos)"),
                3 => ("🐱", "Gato e Rato", "Capture o rato 16 vezes em até 2 minutos"),
                4 => ("☄️", "Esquiva Meteoros", "Sobreviva por 180 segundos sem ser atingido (1 ponto/segundo)"),
                5 => ("🎸", "Guitar Hero", "Faça 300 pontos com ritmo perfeito"),
                6 => ("⚡", "Lightning Strike", "Complete 7 sequências sem errar nenhum padrão"),
                7 => ("🎯", "Sniper Mode", "Acerte 7 alvos em sequência com o LED piscando por 300ms cada"),
                _ => ("🎮", "Jogo Desconhecido", "Desafio não definido")
            };
        }

        private async void LoadRankings()
        {
            try
            {
                var allScores = await _scoreService.GetGameScoresAsync();
                var topScores = allScores
                    .OrderByDescending(s => s.Score)
                    .Take(10)
                    .ToList();

                RankingsPanel.Children.Clear();

                if (topScores.Any())
                {
                    for (int i = 0; i < topScores.Count; i++)
                    {
                        var score = topScores[i];
                        var gameMode = GetGameModeFromString(score.GameMode);
                        var rankBorder = CreateRankingItem(i + 1, score.PlayerName, score.Score, gameMode);
                        RankingsPanel.Children.Add(rankBorder);
                    }
                }
                else
                {
                    var noDataBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                        CornerRadius = new Avalonia.CornerRadius(8),
                        Padding = new Avalonia.Thickness(15)
                    };

                    var noDataText = new TextBlock
                    {
                        Text = "Nenhum ranking disponível ainda.\nSeja o primeiro a jogar!",
                        Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 224)),
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        FontSize = 14
                    };

                    noDataBorder.Child = noDataText;
                    RankingsPanel.Children.Add(noDataBorder);
                }
            }
            catch
            {
                // If no rankings available, show placeholder
                RankingsPanel.Children.Clear();
                var errorBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Padding = new Avalonia.Thickness(15)
                };

                var errorText = new TextBlock
                {
                    Text = "Rankings serão exibidos\napós as primeiras partidas!",
                    Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 224)),
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    FontSize = 14
                };

                errorBorder.Child = errorText;
                RankingsPanel.Children.Add(errorBorder);
            }
        }

        private Border CreateRankingItem(int position, string playerName, int score, int gameMode)
        {
            // Use purple gradient background for all positions
            var backgroundBrush = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(94, 96, 206), 0),   // Purple
                    new GradientStop(Color.FromRgb(116, 0, 184), 1)    // Darker Purple
                }
            };

            var border = new Border
            {
                Background = backgroundBrush,
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            // Position emoji
            var positionEmoji = position switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"{position}°"
            };

            var positionText = new TextBlock
            {
                Text = positionEmoji,
                FontSize = 18,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Brushes.White
            };

            // Player info
            var playerStack = new StackPanel
            {
                Margin = new Avalonia.Thickness(10, 0)
            };

            var nameText = new TextBlock
            {
                Text = playerName,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Medium,
                FontSize = 14
            };

            var gameText = new TextBlock
            {
                Text = GetGameName(gameMode),
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 224)),
                FontSize = 11
            };

            playerStack.Children.Add(nameText);
            playerStack.Children.Add(gameText);

            // Score
            var scoreText = new TextBlock
            {
                Text = score.ToString("N0"),
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                FontWeight = FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 16
            };

            Grid.SetColumn(positionText, 0);
            Grid.SetColumn(playerStack, 1);
            Grid.SetColumn(scoreText, 2);

            grid.Children.Add(positionText);
            grid.Children.Add(playerStack);
            grid.Children.Add(scoreText);

            border.Child = grid;
            return border;
        }

        private string GetGameName(int gameMode)
        {
            return gameMode switch
            {
                1 => "🎯 Pega-Luz",
                2 => "🧠 Sequência Maluca",
                3 => "🐱 Gato e Rato",
                4 => "☄️ Esquiva Meteoros",
                5 => "🎸 Guitar Hero",
                6 => "⚡ Lightning Strike",
                7 => "🎯 Sniper Mode",
                _ => "Desconhecido"
            };
        }

        private int GetGameModeFromString(string gameModeString)
        {
            return gameModeString switch
            {
                "Pega-Luz" => 1,
                "Sequência Maluca" => 2,
                "Gato e Rato" => 3,
                "Esquiva Meteoros" => 4,
                "Guitar Hero" => 5,
                "Lightning Strike" => 6,
                "Sniper Mode" => 7,
                _ => 1
            };
        }

        private void OnScoreSaved(object? sender, GameScore score)
        {
            // Update rankings on UI thread when a new score is saved
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadRankings();
            });
        }

        public void RefreshRankings()
        {
            LoadRankings();
        }

        private void NameTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // Play key sound for any key press
            _audioService.PlaySound(AudioEvent.KeyPress);

            if (e.Key == Key.Enter)
            {
                CodeTextBox.Focus();
                e.Handled = true;
            }

            UpdateLoginButtonState();
        }

        private void CodeTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            var code = CodeTextBox.Text?.ToUpper() ?? "";

            // Check if it's admin code
            if (code.ToUpper() == "ADMIN2024")
            {
                NamePanel.IsVisible = false;
                // Mudança aqui - não há mais GameModePanel no novo layout
                // GameModePanel.IsVisible = false;
                AdminPanel.IsVisible = true;
                StatusText.Text = "🔧 Modo Administrador Detectado";
                StatusText.Foreground = Brushes.Orange;
            }
            else
            {
                NamePanel.IsVisible = true;
                // GameModePanel.IsVisible = true;
                AdminPanel.IsVisible = false;
                StatusText.Text = "";
            }

            UpdateLoginButtonState();
        }

        private void CodeTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            // Play key sound for any key press
            _audioService.PlaySound(AudioEvent.KeyPress);

            if (e.Key == Key.Enter && LoginButton.IsEnabled)
            {
                LoginButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void UpdateLoginButtonState()
        {
            var code = CodeTextBox.Text?.Trim() ?? "";
            var name = NameTextBox.Text?.Trim() ?? "";

            // Admin doesn't need name
            if (code.ToUpper() == "ADMIN2024")
            {
                LoginButton.IsEnabled = true;
                return;
            }

            // Clients need both name and code
            LoginButton.IsEnabled = !string.IsNullOrWhiteSpace(code) &&
                                   !string.IsNullOrWhiteSpace(name) &&
                                   code.Length >= 4;
        }

        private async void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            _audioService.PlaySound(AudioEvent.ButtonClick);
            var code = CodeTextBox.Text?.Trim() ?? "";
            var name = NameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(code))
            {
                _audioService.PlaySound(AudioEvent.LoginError);
                ShowStatus("❌ Digite um código de acesso!", Brushes.Red);
                return;
            }

            LoginButton.IsEnabled = false;
            LoginButton.Content = "🔄 Verificando...";

            try
            {
                var result = _authService.Authenticate(code, name);

                if (result.Success)
                {
                    _audioService.PlaySound(AudioEvent.LoginSuccess);
                    _currentUser = result.User;
                    ShowStatus($"✅ {result.Message}", Brushes.LimeGreen);

                    // Wait a moment to show success message
                    await Task.Delay(1000);

                    Close(true);
                }
                else
                {
                    _audioService.PlaySound(AudioEvent.LoginError);
                    ShowStatus($"❌ {result.Message}", Brushes.Red);

                    // Clear sensitive information on failure
                    if (result.Message.Contains("já foi utilizado") || result.Message.Contains("inválido"))
                    {
                        CodeTextBox.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                _audioService.PlaySound(AudioEvent.Error);
                ShowStatus($"❌ Erro na autenticação: {ex.Message}", Brushes.Red);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "🚀 Entrar no Jogo";
                UpdateLoginButtonState();
            }
        }

        private async void GenerateCodesButton_Click(object? sender, RoutedEventArgs e)
        {
            _audioService.PlaySound(AudioEvent.ButtonClick);
            var dialog = new CodeGeneratorDialog();
            var result = await dialog.ShowDialog<int?>(this);

            if (result.HasValue && result.Value > 0)
            {
                try
                {
                    var codes = _authService.GenerateClientCodes(result.Value);
                    var fileName = $"bilhetes_jogo_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    _authService.SaveCodesToFile(codes, fileName);

                    _audioService.PlaySound(AudioEvent.Notification);
                    ShowStatus($"✅ {codes.Count} códigos gerados! Arquivo: {fileName}", Brushes.LimeGreen);
                }
                catch (Exception ex)
                {
                    _audioService.PlaySound(AudioEvent.Error);
                    ShowStatus($"❌ Erro ao gerar códigos: {ex.Message}", Brushes.Red);
                }
            }
        }

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            // Play function key sound for F11
            if (e.Key == Key.F11)
            {
                _audioService.PlaySound(AudioEvent.FunctionKey);
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            try
            {
                if (_isFullScreen)
                {
                    // Exit full-screen
                    WindowState = WindowState.Normal;
                    _isFullScreen = false;
                }
                else
                {
                    // Enter full-screen
                    WindowState = WindowState.FullScreen;
                    _isFullScreen = true;
                }
            }
            catch
            {
                // Ignore fullscreen errors
            }
        }

        private void UpdateGameInstructions(int gameMode)
        {
            var (title, instructions) = gameMode switch
            {
                1 => ("🎯 Pega-Luz", "PEGA-LUZ:\n\n• Pressione as teclas quando o LED acender\n• Seja ultra-rápido! Timeout diminui com progresso\n• +10 pontos por acerto\n• Timeout mínimo: 500ms\n• Cada erro é crucial\n\n🏆 DESAFIO DE VITÓRIA:\nAlcance 400 pontos com reflexos ultra-rápidos!"),
                2 => ("🧠 Sequência Maluca", "SEQUÊNCIA MALUCA:\n\n• Observe a sequência de LEDs\n• Repita pressionando as teclas corretas\n• Cada rodada adiciona +1 LED\n• Erro = Game Over\n\n🏆 DESAFIO DE VITÓRIA:\nComplete 8 rodadas (sequência chega a 10 passos)!"),
                3 => ("🐱 Gato e Rato", "GATO E RATO:\n\n• Mova-se apenas UMA VEZ por movimento do rato\n• Capture o rato que pisca rapidamente\n• Rato fica mais rápido a cada captura\n• +20 pontos por captura\n\n🏆 DESAFIO DE VITÓRIA:\nCapture o rato 16 vezes em até 2 minutos!"),
                4 => ("☄️ Esquiva Meteoros", "ESQUIVA METEOROS:\n\n• Use as teclas para desviar\n• Meteoros caem cada vez mais rápido\n• Múltiplos meteoros simultâneos\n• +1 ponto por segundo\n\n🏆 DESAFIO DE VITÓRIA:\nSobreviva por 180 segundos (3 minutos) sem ser atingido!"),
                5 => ("🎸 Guitar Hero", "GUITAR HERO:\n\n• Pressione as teclas no ritmo\n• Notas ficam mais rápidas com progresso\n• Penalidade por erros e perdas\n• Precisão é fundamental\n\n🏆 DESAFIO DE VITÓRIA:\nFaça 300 pontos com ritmo perfeito!"),
                6 => ("⚡ Lightning Strike", "LIGHTNING STRIKE:\n\n• Padrão pisca por milissegundos\n• Memorize e reproduza rapidamente\n• Tempo diminui drasticamente por rodada\n• Erro = Game Over\n\n🏆 DESAFIO DE VITÓRIA:\nComplete 7 sequências sem errar nenhum padrão!"),
                7 => ("🎯 Sniper Mode", "SNIPER MODE:\n\n• Alvos piscam por apenas 300ms\n• Pressione a tecla exata no tempo\n• Precisão absoluta necessária\n• Sequência = vitória\n\n🏆 DESAFIO DE VITÓRIA:\nAcerte 7 alvos em sequência!"),
                _ => ("Selecione um Jogo", "Selecione um jogo na lista para ver as instruções detalhadas e o desafio específico para conquistar a vitória!")
            };

            GameTitleText.Text = title;
            GameInstructionsText.Text = instructions;
        }

        private void ShowStatus(string message, IBrush color)
        {
            StatusText.Text = message;
            StatusText.Foreground = color;
        }
    }

    public class CodeGeneratorDialog : Window
    {
        private NumericUpDown _countInput;
        private TextBlock _infoText;

        public CodeGeneratorDialog()
        {
            Title = "Gerar Códigos de Cliente";
            Width = 600;
            Height = 450;
            MinWidth = 500;
            MinHeight = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = true;
            Background = new SolidColorBrush(Color.FromRgb(26, 32, 44));

            var stack = new StackPanel
            {
                Margin = new Avalonia.Thickness(40),
                Spacing = 30
            };

            stack.Children.Add(new TextBlock
            {
                Text = "📄 Gerador de Códigos de Cliente",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Quantos códigos deseja gerar?",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 224)),
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 10)
            });

            _countInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 50,
                Increment = 10,
                ShowButtonSpinner = true,
                Padding = new Avalonia.Thickness(20),
                FontSize = 20,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Margin = new Avalonia.Thickness(50, 0, 50, 0)
            };
            stack.Children.Add(_countInput);

            _infoText = new TextBlock
            {
                Text = "💡 Os códigos serão salvos em um arquivo de texto formatado e pronto para impressão e corte em bilhetes individuais.",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192)),
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(20, 15, 20, 15)
            };
            stack.Children.Add(_infoText);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 30,
                Margin = new Avalonia.Thickness(0, 20, 0, 0)
            };

            var generateButton = new Button
            {
                Content = "✅ Gerar Códigos",
                Padding = new Avalonia.Thickness(30, 15),
                Background = new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                Foreground = Brushes.White,
                CornerRadius = new Avalonia.CornerRadius(8),
                FontWeight = FontWeight.Medium,
                FontSize = 16
            };
            generateButton.Click += (s, e) =>
            {
                Close((int)_countInput.Value);
            };

            var cancelButton = new Button
            {
                Content = "❌ Cancelar",
                Padding = new Avalonia.Thickness(30, 15),
                Background = new SolidColorBrush(Color.FromRgb(229, 62, 62)),
                Foreground = Brushes.White,
                CornerRadius = new Avalonia.CornerRadius(8),
                FontWeight = FontWeight.Medium,
                FontSize = 16
            };
            cancelButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(generateButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);

            Content = stack;
        }
    }
}
