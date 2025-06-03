using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using miniJogo.Models.Auth;
using miniJogo.Services;

namespace miniJogo.Views
{
    public partial class LoginWindow : Window
    {
        private AuthService _authService;
        private User? _currentUser;
        private int _selectedGameMode = 1;

        public User? AuthenticatedUser => _currentUser;
        public int SelectedGameMode => _selectedGameMode;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();
            InitializeGameModeSelector();
            
            // Focus on name field initially
            NameTextBox.Focus();
        }

        private void InitializeGameModeSelector()
        {
            // Set default game mode
            _selectedGameMode = 1;
            GameModeComboBox.SelectedIndex = 0;
            UpdateGameInstructions(1);
        }

        private void NameTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
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
            var code = CodeTextBox.Text?.Trim() ?? "";
            var name = NameTextBox.Text?.Trim() ?? "";
            
            if (string.IsNullOrWhiteSpace(code))
            {
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
                    _currentUser = result.User;
                    ShowStatus($"✅ {result.Message}", Brushes.LimeGreen);
                    
                    // Wait a moment to show success message
                    await Task.Delay(1000);
                    
                    Close(true);
                }
                else
                {
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
            var dialog = new CodeGeneratorDialog();
            var result = await dialog.ShowDialog<int?>(this);
            
            if (result.HasValue && result.Value > 0)
            {
                try
                {
                    var codes = _authService.GenerateClientCodes(result.Value);
                    var fileName = $"bilhetes_jogo_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    _authService.SaveCodesToFile(codes, fileName);
                    
                    ShowStatus($"✅ {codes.Count} códigos gerados e salvos em {fileName}", Brushes.LimeGreen);
                }
                catch (Exception ex)
                {
                    ShowStatus($"❌ Erro ao gerar códigos: {ex.Message}", Brushes.Red);
                }
            }
        }

        private void GameModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GameModeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _selectedGameMode = int.Parse(tag);
                UpdateGameInstructions(_selectedGameMode);
            }
        }

        private void UpdateGameInstructions(int gameMode)
        {
            var instructions = gameMode switch
            {
                1 => "🎯 PEGA-LUZ:\n• Pressione 0-9, A-F quando o LED acender\n• Seja rápido! LEDs apagam sozinhos\n• +10 pontos por acerto\n• +5 pontos por velocidade",
                2 => "🧠 SEQUÊNCIA MALUCA:\n• Observe a sequência de LEDs\n• Repita pressionando 0-9, A-F\n• Cada nível adiciona +1 LED\n• Erro = Game Over",
                3 => "🐱 GATO E RATO:\n• Use setas para mover o gato\n• Capture o rato vermelho\n• Evite as armadilhas azuis\n• +20 pontos por captura",
                4 => "☄️ ESQUIVA METEOROS:\n• Use ↑↓←→ para desviar\n• Meteoros caem aleatoriamente\n• Sobreviva o máximo possível\n• +1 ponto por segundo",
                5 => "🎸 GUITAR HERO:\n• Pressione 0-9, A-F no ritmo\n• Siga as batidas musicais\n• Combo = pontos multiplicados\n• Precisão é fundamental",
                6 => "🎲 ROLETA RUSSA:\n• Escolha um LED pressionando 0-9, A-F\n• Multiplicador: 2x, 4x, 8x, 16x...\n• Acerte = continua com multiplicador maior\n• Erre = perde TODA a pontuação!",
                7 => "⚡ LIGHTNING STRIKE:\n• Padrão pisca por milissegundos\n• Memorize e reproduza rapidamente\n• Tempo de exibição diminui por nível\n• Erro = Game Over instantâneo",
                8 => "🎯 SNIPER MODE:\n• Alvos piscam por apenas 0.1 segundo\n• Pressione a tecla exata no tempo\n• 10 acertos = vitória impossível\n• Bônus x10 se completar!",
                _ => "Selecione um jogo para ver as instruções..."
            };

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
            Width = 400;
            Height = 250;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;
            Background = new SolidColorBrush(Color.FromRgb(26, 32, 44));

            var stack = new StackPanel
            {
                Margin = new Avalonia.Thickness(30),
                Spacing = 20
            };

            stack.Children.Add(new TextBlock
            {
                Text = "📄 Gerador de Códigos de Cliente",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            stack.Children.Add(new TextBlock
            {
                Text = "Quantos códigos deseja gerar?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 224))
            });

            _countInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10000,
                Value = 50,
                Increment = 10,
                ShowButtonSpinner = true,
                Padding = new Avalonia.Thickness(10),
                FontSize = 16
            };
            stack.Children.Add(_countInput);

            _infoText = new TextBlock
            {
                Text = "Os códigos serão salvos em um arquivo de texto\npronto para impressão e corte.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192)),
                TextAlignment = Avalonia.Media.TextAlignment.Center
            };
            stack.Children.Add(_infoText);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 15
            };

            var generateButton = new Button
            {
                Content = "✅ Gerar",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(56, 161, 105)),
                Foreground = Brushes.White,
                CornerRadius = new Avalonia.CornerRadius(6)
            };
            generateButton.Click += (s, e) =>
            {
                Close((int)_countInput.Value);
            };

            var cancelButton = new Button
            {
                Content = "❌ Cancelar",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(229, 62, 62)),
                Foreground = Brushes.White,
                CornerRadius = new Avalonia.CornerRadius(6)
            };
            cancelButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(generateButton);
            buttonPanel.Children.Add(cancelButton);
            stack.Children.Add(buttonPanel);

            Content = stack;
        }
    }
}