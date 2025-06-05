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
            // Set default game mode
            _selectedGameMode = 1;
            GameModeComboBox.SelectedIndex = 0;
            UpdateGameInstructions(1);
        }

        private void LoadRankings()
        {
            try
            {
                var allScores = _scoreService.GetAllScores();
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
                        var rankBorder = CreateRankingItem(i + 1, score.PlayerName, score.Score, 1);
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
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
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
                6 => "🎲 Roleta Russa",
                7 => "⚡ Lightning Strike",
                8 => "🎯 Sniper Mode",
                _ => "Desconhecido"
            };
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
                GameModePanel.IsVisible = false;
                AdminPanel.IsVisible = true;
                StatusText.Text = "🔧 Modo Administrador Detectado";
                StatusText.Foreground = Brushes.Orange;
            }
            else
            {
                NamePanel.IsVisible = true;
                GameModePanel.IsVisible = true;
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

        private void GameModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _audioService.PlaySound(AudioEvent.ButtonHover);
            if (GameModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _selectedGameMode = int.Parse(tag);
                UpdateGameInstructions(_selectedGameMode);
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
                1 => ("🎯 Pega-Luz", "🎯 PEGA-LUZ:\n\n• Pressione W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L quando o LED acender\n• Seja rápido! LEDs apagam sozinhos\n• +10 pontos por acerto\n• +5 pontos por velocidade\n\n⌨️ Controles:\nTeclas W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L = LEDs da matriz\n\n🎯 Objetivo:\nConseguir a maior pontuação possível!"),
                2 => ("🧠 Sequência Maluca", "🧠 SEQUÊNCIA MALUCA:\n\n• Observe a sequência de LEDs\n• Repita pressionando W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L\n• Cada nível adiciona +1 LED\n• Erro = Game Over\n\n⌨️ Controles:\nTeclas W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L = LEDs da matriz\n\n🎯 Objetivo:\nMemoizar sequências cada vez maiores!"),
                3 => ("🐱 Gato e Rato", "🐱 GATO E RATO:\n\n• Use setas para mover o gato\n• Capture o rato vermelho\n• Evite as armadilhas azuis\n• +20 pontos por captura\n\n⌨️ Controles:\nSetas ↑↓←→ = Movimento\n\n🎯 Objetivo:\nCapturar o máximo de ratos possível!"),
                4 => ("☄️ Esquiva Meteoros", "☄️ ESQUIVA METEOROS:\n\n• Use ↑↓←→ para desviar\n• Meteoros caem aleatoriamente\n• Sobreviva o máximo possível\n• +1 ponto por segundo\n\n⌨️ Controles:\nSetas ↑↓←→ = Movimento\n\n🎯 Objetivo:\nSobreviver o máximo de tempo!"),
                5 => ("🎸 Guitar Hero", "🎸 GUITAR HERO:\n\n• Pressione W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L no ritmo\n• Siga as batidas musicais\n• Combo = pontos multiplicados\n• Precisão é fundamental\n\n⌨️ Controles:\nTeclas W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L = Notas musicais\n\n🎯 Objetivo:\nTocar no ritmo perfeito!"),
                6 => ("🎲 Roleta Russa", "🎲 ROLETA RUSSA:\n\n• Escolha um LED pressionando W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L\n• Multiplicador: 2x, 4x, 8x, 16x...\n• Acerte = continua com multiplicador maior\n• Erre = perde TODA a pontuação!\n\n⌨️ Controles:\nTeclas W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L = Escolha do LED\n\n🎯 Objetivo:\nArriscar para multiplicar pontos!"),
                7 => ("⚡ Lightning Strike", "⚡ LIGHTNING STRIKE:\n\n• Padrão pisca por milissegundos\n• Memorize e reproduza rapidamente\n• Tempo de exibição diminui por nível\n• Erro = Game Over instantâneo\n\n⌨️ Controles:\nTeclas W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L = LEDs da matriz\n\n🎯 Objetivo:\nMemória e reflexos ultra-rápidos!"),
                8 => ("🎯 Sniper Mode", "🎯 SNIPER MODE:\n\n• Alvos piscam por apenas 0.1 segundo\n• Pressione a tecla exata no tempo\n• 10 acertos = vitória impossível\n• Bônus x10 se completar!\n\n⌨️ Controles:\nTeclas W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L = Mira precisa\n\n🎯 Objetivo:\nPrecisão absoluta em tempo mínimo!"),
                _ => ("Selecione um Jogo", "Selecione um jogo na lista para ver as instruções detalhadas.")
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
