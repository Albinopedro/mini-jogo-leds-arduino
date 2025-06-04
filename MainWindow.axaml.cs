using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Globalization;
using Avalonia.Controls.Shapes;
using miniJogo.Models;
using miniJogo.Services;
using miniJogo.Views;
using miniJogo.Models.Auth;
using System.Linq;

namespace miniJogo;

public partial class MainWindow : Window
{
    private SerialPort? _serialPort;
    private Timer _statusTimer = null!;
    private string _playerName = "";
    private int _currentGameMode = 0;
    private bool _gameActive = false;
    private int _score = 0;
    private int _level = 1;
    private DateTime _gameStartTime;
    private ScoreService _scoreService;
    private ClientSessionService _sessionService;
    private DebugWindow? _debugWindow;
    private bool _isFullScreen = true;
    private User? _currentUser;
    private bool _isClientMode = false;
    // Adicione esta linha junto às outras variáveis de instância no início da classe
    private volatile bool _isSessionEnding = false;
    private readonly object _gameStateLock = new object();
    private bool _disposed = false;
    private volatile bool _isClosing = false;
    private readonly object _closingLock = new object();
    private volatile bool _isShowingSessionDialog = false;
    private readonly object _sessionDialogLock = new object();

    // LED Matrix (4x4)
    private readonly Ellipse[,] _ledMatrix = new Ellipse[4, 4];

    // LED Timers for effects
    private readonly Dictionary<int, System.Threading.Timer> _ledTimers = new();
    private readonly object _ledTimersLock = new();

    // Game descriptions for the new games
    private readonly Dictionary<int, string> _gameDescriptions = new()
    {
        { 1, "🎯 Pressione o LED que acende antes que ele apague! Reflexos rápidos são essenciais." },
        { 2, "🧠 Memorize e repita a sequência de LEDs que pisca. Cada nível adiciona mais LEDs!" },
        { 3, "🐱 Você é o gato! Persiga o rato (LED vermelho) pela matriz usando as teclas." },
        { 4, "☄️ Desvie dos meteoros (LEDs vermelhos) que caem! Use as setas para mover." },
        { 5, "🎸 Pressione os LEDs no ritmo da música! Timing perfeito = pontos extras." },
        { 6, "🎲 Roleta Russa LED! Escolha um LED - acerte e multiplique sua pontuação, erre e perca tudo!" },
        { 7, "⚡ Lightning Strike! Memorize padrões ultra-rápidos que aparecem por milissegundos!" },
        { 8, "🎯 Sniper Mode! Atire nos alvos que piscam por apenas 0.1 segundo - precisão extrema!" }
    };

    // Game instructions for the new games
    private readonly Dictionary<int, string> _gameInstructions = new()
    {
        { 1, "PEGA-LUZ:\n• Pressione 0-9, A-F quando o LED acender\n• Seja rápido! LEDs apagam sozinhos\n• +10 pontos por acerto\n• +5 pontos por velocidade" },
        { 2, "SEQUÊNCIA MALUCA:\n• Observe a sequência de LEDs\n• Repita pressionando 0-9, A-F\n• Cada nível adiciona +1 LED\n• Erro = Game Over" },
        { 3, "GATO E RATO:\n• Use setas para mover o gato\n• Capture o rato vermelho\n• Evite as armadilhas azuis\n• +20 pontos por captura" },
        { 4, "ESQUIVA METEOROS:\n• Use ↑↓←→ para desviar\n• Meteoros caem aleatoriamente\n• Sobreviva o máximo possível\n• +1 ponto por segundo" },
        { 5, "GUITAR HERO:\n• Pressione 0-9, A-F no ritmo\n• Siga as batidas musicais\n• Combo = pontos multiplicados\n• Precisão é fundamental" },
        { 6, "ROLETA RUSSA:\n• Escolha um LED pressionando 0-9, A-F\n• Multiplicador: 2x, 4x, 8x, 16x...\n• Acerte = continua com multiplicador maior\n• Erre = perde TODA a pontuação!" },
        { 7, "LIGHTNING STRIKE:\n• Padrão pisca por milissegundos\n• Memorize e reproduza rapidamente\n• Tempo de exibição diminui por nível\n• Erro = Game Over instantâneo" },
        { 8, "SNIPER MODE:\n• Alvos piscam por apenas 0.1 segundo\n• Pressione a tecla exata no tempo\n• 10 acertos = vitória impossível\n• Bônus x10 se completar!" }
    };

    public MainWindow() : this(null, 1)
    {
        // Default constructor for designer
    }

    public MainWindow(User? user, int selectedGameMode = 1)
    {
        InitializeComponent();
        InitializeLedMatrix();
        InitializeTimer();
        _scoreService = new ScoreService();
        _sessionService = new ClientSessionService();

        // Set user and configure interface
        _currentUser = user ?? new User { Name = "Designer", Type = UserType.Admin };
        _isClientMode = _currentUser?.Type == UserType.Client;

        // Create client session if needed
        if (_isClientMode && _currentUser != null)
        {
            _sessionService.CreateSession(_currentUser, (GameMode)selectedGameMode);
        }

        ConfigureInterfaceForUser(selectedGameMode);

        // Start in fullscreen
        WindowState = WindowState.FullScreen;
        _isFullScreen = true;

        RefreshPorts();

        // Auto-connect for clients
        if (_isClientMode)
        {
            _ = Task.Run(AutoConnectArduino);
        }
    }

    public class CodeGeneratorDialog : Window
    {
        private NumericUpDown _countInput;

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
                Text = "Quantos códigos deseja gerar para impressão?",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 224)),
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
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

            stack.Children.Add(new TextBlock
            {
                Text = "💡 Os códigos serão salvos em um arquivo de texto formatado e pronto para impressão e corte em bilhetes individuais.",
                FontSize = 15,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 174, 192)),
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(20, 15, 20, 15)
            });

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

    private void InitializeLedMatrix()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                var led = new Ellipse
                {
                    Width = 80,
                    Height = 80,
                    Fill = GetLedDefaultColor(row),
                    Stroke = new SolidColorBrush(Color.FromRgb(79, 172, 254)), // #4FACFE
                    StrokeThickness = 2,
                    Margin = new Avalonia.Thickness(12)
                };

                Grid.SetRow(led, row);
                Grid.SetColumn(led, col);
                LedMatrix.Children.Add(led);
                _ledMatrix[row, col] = led;
            }
        }
    }

    private void InitializeTimer()
    {
        _statusTimer = new Timer(async _ =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_gameActive)
                {
                    var elapsed = DateTime.Now - _gameStartTime;
                    TimeText.Text = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                }
                UpdateUI();
            });
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void ConfigureInterfaceForUser(int selectedGameMode)
    {
        // Set player name
        PlayerDisplayText.Text = $"👤 {(_isClientMode ? "Jogador" : "Admin")}: {_currentUser?.Name ?? "Desconhecido"}";

        // Set current game mode FIRST
        _currentGameMode = selectedGameMode;

        // Then populate ComboBox and set selection
        PopulateGameModeComboBox();
        GameModeComboBox.SelectedIndex = selectedGameMode - 1;

        AddDebugMessage($"Jogo selecionado configurado: {GetGameName(selectedGameMode)} (ID: {selectedGameMode})");

        // Configure UI based on user type
        if (_isClientMode)
        {
            // Hide admin features for clients (but keep debug and logout button visible)
            SettingsButton.IsVisible = false;
            GenerateCodesButton.IsVisible = false;

            // Hide manual connection controls
            RefreshPortsButton.IsVisible = false;

            // Set player name directly
            _playerName = _currentUser?.Name ?? "Cliente";

            // Update logout button text for clients
            LogoutButton.Content = "🚪 Encerrar Sessão";

            // Update status
            StatusText.Text = $"🎮 Bem-vindo, {_currentUser?.Name ?? "Cliente"}! Conectando ao Arduino...";

            // Lock game mode selection for clients - they play ONLY the selected game
            GameModeComboBox.IsEnabled = false;

            // Display session status for the selected game only
            UpdateRemainingRoundsDisplay();

            // Show clear message about single game session
            var selectedGameName = GetGameName(_currentGameMode);
            AddDebugMessage($"Cliente {_currentUser?.Name} iniciará sessão de jogo único: {selectedGameName}");
        }
        else
        {
            // Admin has access to everything including new buttons
            GenerateCodesButton.IsVisible = true;
            LogoutButton.IsVisible = true;
            StatusText.Text = "🔧 Modo Administrador - Acesso completo ativado";
        }

        // Set game description
        GameDescriptionText.Text = GetGameDescription(selectedGameMode);
        CurrentGameText.Text = GetGameName(selectedGameMode);

        // Show remaining rounds for clients
        if (_isClientMode)
        {
            UpdateRemainingRoundsDisplay();
        }
    }

    private async void LogoutButton_Click(object? sender, RoutedEventArgs e)
    {
        // Prevent multiple logout attempts
        lock (_closingLock)
        {
            if (_isClosing || _disposed)
            {
                AddDebugMessage("[LOGOUT] Logout já em andamento, ignorando nova tentativa");
                return;
            }
        }

        // Stop any active games
        if (_gameActive)
        {
            StopGameButton_Click(null, new RoutedEventArgs());
        }

        // Show confirmation dialog with different messages for clients vs admins
        string title = _isClientMode ? "Encerrar Sessão" : "Logout";
        string message = _isClientMode
            ? $"Tem certeza que deseja encerrar sua sessão, {_currentUser?.Name}?\nSua sessão será finalizada e você retornará à tela de login."
            : "Tem certeza que deseja fazer logout?\nO jogo será fechado e você retornará à tela de login.";

        var result = await ShowConfirmDialog(title, message);

        if (result)
        {
            lock (_closingLock)
            {
                if (_isClosing || _disposed)
                {
                    AddDebugMessage("[LOGOUT] Logout já em andamento, ignorando confirmação");
                    return;
                }
                _isClosing = true;
            }

            AddDebugMessage($"[LOGOUT] {(_isClientMode ? "Cliente" : "Administrador")} {_currentUser?.Name ?? "Usuário"} fazendo logout");

            // End client session if in client mode
            if (_isClientMode && _currentUser != null)
            {
                AddDebugMessage($"[LOGOUT] Encerrando sessão do cliente {_currentUser.Name}");
                _sessionService.EndSession(_currentUser.Id);
            }

            // Disconnect Arduino
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.Close();
                    _serialPort = null;
                    AddDebugMessage("[LOGOUT] Arduino desconectado");
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"[LOGOUT] Erro ao desconectar Arduino: {ex.Message}");
                }
            }

            // Return to login without closing application
            await ReturnToLoginSafely();
        }
        else
        {
            AddDebugMessage($"[LOGOUT] Logout cancelado pelo {(_isClientMode ? "cliente" : "administrador")}");
        }
    }

    private async Task ReturnToLoginSafely()
    {
        try
        {
            AddDebugMessage("[LOGOUT] ⚠️ ReturnToLoginSafely chamado - iniciando retorno seguro ao login");
            System.Diagnostics.Debug.WriteLine("[LOGOUT] ⚠️ ReturnToLoginSafely chamado");

            // Set closing flag to prevent other operations
            lock (_closingLock)
            {
                _isClosing = true;
            }

            // Hide current window first
            Hide();

            // Small delay to ensure UI updates and prevent race conditions
            await Task.Delay(200);

            // Just close this window - App.axaml.cs will handle showing login
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddDebugMessage("[LOGOUT] 🚪 Fechando MainWindow via ReturnToLoginSafely");
                System.Diagnostics.Debug.WriteLine("[LOGOUT] 🚪 Fechando MainWindow via ReturnToLoginSafely");
                
                try
                {
                    Close();
                    AddDebugMessage("[LOGOUT] ✅ MainWindow fechado - App.axaml.cs irá mostrar login");
                }
                catch (Exception closeEx)
                {
                    AddDebugMessage($"[LOGOUT] ❌ Erro ao fechar window: {closeEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LOGOUT] ❌ Erro ao fechar window: {closeEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            AddDebugMessage($"[LOGOUT] ❌ Erro no retorno seguro ao login: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LOGOUT] ❌ Erro no retorno seguro ao login: {ex.Message}");
            
            // Fallback: still try to close the window
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => Close());
            }
            catch (Exception fallbackEx)
            {
                AddDebugMessage($"[LOGOUT] 💥 Erro crítico no fallback: {fallbackEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[LOGOUT] 💥 Erro crítico no fallback: {fallbackEx.Message}");
            }
        }
    }

    private async void GenerateCodesButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser?.Type != UserType.Admin)
        {
            await ShowMessage("Acesso Negado", "Apenas administradores podem gerar códigos de cliente.");
            return;
        }

        try
        {
            var dialog = new CodeGeneratorDialog();
            var result = await dialog.ShowDialog<int?>(this);

            if (result.HasValue && result.Value > 0)
            {
                var authService = new AuthService();
                var codes = authService.GenerateClientCodes(result.Value);
                var fileName = $"bilhetes_jogo_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                authService.SaveCodesToFile(codes, fileName);

                await ShowMessage("Códigos Gerados", $"✅ {codes.Count} códigos gerados com sucesso!\n\nArquivo salvo: {fileName}\n\nOs códigos estão prontos para impressão e corte em bilhetes.");
                AddDebugMessage($"Admin gerou {codes.Count} códigos de cliente. Arquivo: {fileName}");
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("Erro", $"Erro ao gerar códigos: {ex.Message}");
            AddDebugMessage($"Erro na geração de códigos: {ex.Message}");
        }
    }

    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        var confirmWindow = new Window
        {
            Title = title,
            Width = 550,
            Height = 350,
            MinWidth = 450,
            MinHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Background = new SolidColorBrush(Color.FromRgb(26, 32, 44))
        };

        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(40), Spacing = 30 };

        stackPanel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontSize = 18,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            Margin = new Avalonia.Thickness(20)
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 30,
            Margin = new Avalonia.Thickness(0, 20, 0, 0)
        };

        var yesButton = new Button
        {
            Content = "✅ Sim",
            Padding = new Avalonia.Thickness(30, 15),
            Background = new SolidColorBrush(Color.FromRgb(229, 62, 62)),
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(8),
            FontSize = 16,
            FontWeight = FontWeight.Medium
        };

        var noButton = new Button
        {
            Content = "❌ Não",
            Padding = new Avalonia.Thickness(30, 15),
            Background = new SolidColorBrush(Color.FromRgb(74, 85, 104)),
            Foreground = Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(8),
            FontSize = 16,
            FontWeight = FontWeight.Medium
        };

        bool result = false;
        yesButton.Click += (s, e) => { result = true; confirmWindow.Close(); };
        noButton.Click += (s, e) => { result = false; confirmWindow.Close(); };

        buttonPanel.Children.Add(yesButton);
        buttonPanel.Children.Add(noButton);
        stackPanel.Children.Add(buttonPanel);

        confirmWindow.Content = stackPanel;
        await confirmWindow.ShowDialog(this);

        return result;
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
            _ => "Nenhum"
        };
    }

    private async Task AutoConnectArduino()
    {
        await Task.Delay(1000); // Wait for UI to load

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                var ports = SerialPort.GetPortNames();

                if (ports.Length > 0)
                {
                    // Try to connect to the first available port
                    foreach (var port in ports)
                    {
                        try
                        {
                            _serialPort = new SerialPort(port, 9600);
                            _serialPort.DataReceived += SerialPort_DataReceived;
                            _serialPort.Open();

                            // Update UI
                            ConnectionText.Text = "Conectado";
                            ConnectionStatus.Fill = Brushes.LimeGreen;
                            ConnectionBorder.Background = new SolidColorBrush(Color.FromRgb(56, 161, 105));
                            StartGameButton.IsEnabled = true;
                            StatusText.Text = $"✅ Arduino conectado automaticamente na porta {port}. Jogo selecionado: {GetGameName(_currentGameMode)}";
                            AddDebugMessage($"Arduino conectado automaticamente na porta {port}");

                            // Confirm game mode is locked for clients after successful connection
                            if (_isClientMode)
                            {
                                GameModeComboBox.IsEnabled = false;
                                AddDebugMessage($"Modo de jogo bloqueado para cliente: {GetGameName(_currentGameMode)}");
                            }

                            break; // Successfully connected
                        }
                        catch
                        {
                            // Try next port
                            continue;
                        }
                    }

                    if (_serialPort?.IsOpen != true)
                    {
                        StatusText.Text = "⚠️ Arduino não encontrado. Conecte o dispositivo e tente novamente.";

                        // For clients, if Arduino can't connect, they can't play
                        if (_isClientMode)
                        {
                            StatusText.Text = "⚠️ Arduino não encontrado. Aguarde a reconexão...";
                            AddDebugMessage("Cliente não pode jogar sem Arduino - aguardando conexão");
                        }
                    }
                }
                else
                {
                    StatusText.Text = "⚠️ Nenhuma porta serial encontrada. Verifique a conexão do Arduino.";

                    // For clients, disable game functions if no Arduino
                    if (_isClientMode)
                    {
                        StartGameButton.IsEnabled = false;
                        StatusText.Text = "⚠️ Arduino necessário para jogar. Aguarde a conexão...";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Erro na conexão automática: {ex.Message}";
                AddDebugMessage($"Erro na conexão automática: {ex.Message}");

                // For clients, ensure they can't start without Arduino
                if (_isClientMode)
                {
                    StartGameButton.IsEnabled = false;
                }
            }
        });
    }

    private void RefreshPorts()
    {
        PortComboBox.Items.Clear();

        var ports = SerialPort.GetPortNames();
        foreach (var port in ports)
        {
            PortComboBox.Items.Add(port);
        }

        if (ports.Length > 0)
        {
            PortComboBox.SelectedIndex = 0;
        }
    }

    private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen == true)
        {
            // Disconnect
            _serialPort.Close();
            _serialPort = null;
            ConnectButton.Content = "🔗 Conectar";
            ConnectionText.Text = "Desconectado";
            ConnectionStatus.Fill = new SolidColorBrush(Color.FromRgb(79, 172, 254)); // #4FACFE
            ConnectionBorder.Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(58, 58, 107), 0),  // #3A3A6B
                    new GradientStop(Color.FromRgb(74, 74, 123), 1)   // #4A4A7B
                }
            };
            AddDebugMessage("Arduino desconectado.");
        }
        else
        {
            // Connect
            if (PortComboBox.SelectedItem is string selectedPort)
            {
                try
                {
                    _serialPort = new SerialPort(selectedPort, 9600);
                    _serialPort.DataReceived += SerialPort_DataReceived;
                    _serialPort.Open();

                    ConnectButton.Content = "🔌 Desconectar";
                    ConnectionText.Text = "Conectado";
                    ConnectionStatus.Fill = new SolidColorBrush(Color.FromRgb(0, 242, 254)); // #00F2FE
                    ConnectionBorder.Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromRgb(79, 172, 254), 0),  // #4FACFE
                            new GradientStop(Color.FromRgb(0, 242, 254), 1)    // #00F2FE
                        }
                    };
                    StartGameButton.IsEnabled = true;

                    // Lock game selection for clients after connection
                    if (_isClientMode)
                    {
                        GameModeComboBox.IsEnabled = false;
                    }

                    AddDebugMessage($"Arduino conectado na porta {selectedPort}");

                    // Send initialization command
                    await Task.Delay(2000); // Wait for Arduino to initialize
                    _serialPort.WriteLine("INIT");
                }
                catch (Exception ex)
                {
                    ConnectButton.Content = "🔌 Conectar";
                    ConnectionText.Text = "Erro de Conexão";
                    ConnectionStatus.Fill = new SolidColorBrush(Color.FromRgb(183, 148, 246)); // #B794F6
                    ConnectionBorder.Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromRgb(102, 126, 234), 0),  // #667EEA
                            new GradientStop(Color.FromRgb(118, 75, 162), 1)    // #764BA2
                        }
                    };
                    StartGameButton.IsEnabled = false;

                    AddDebugMessage($"Erro ao conectar: {ex.Message}");
                    if (!_isClientMode) // Only show error dialogs to admins
                    {
                        await ShowMessage("Erro de Conexão", $"Não foi possível conectar ao Arduino:\n{ex.Message}");
                    }
                }
            }
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var message = _serialPort?.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProcessArduinoMessage(message);
                });
            }
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddDebugMessage($"Erro na recepção: {ex.Message}");
            });
        }
    }

    private void ProcessArduinoMessage(string message)
    {
        AddDebugMessage($"Arduino: {message}", true);

        if (message.StartsWith("GAME_EVENT:"))
        {
            var eventData = message.Substring("GAME_EVENT:".Length);
            ProcessGameEvent(eventData);
        }
        else if (message == "READY")
        {
            StatusText.Text = "🟢 Arduino pronto! Conexão estabelecida com sucesso!";
            // Trigger visual celebration
            TriggerVisualEffect("CONNECTION_SUCCESS");
        }
        else if (message == "ALL_LEDS_OFF")
        {
            ClearLedMatrix();
        }
        else
        {
            // Treat all other messages as game events for consistency
            ProcessGameEvent(message);
        }
    }

    private void AddDebugMessage(string message, bool isDebug = false)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var formattedMessage = $"[{timestamp}] {message}";

        _debugWindow?.AddMessage(formattedMessage, isDebug);
    }

    private void ProcessGameEvent(string eventData)
    {
        var parts = eventData.Split(':');
        if (parts.Length < 2) return;

        var eventType = parts[0];
        var eventValue = parts[1];

        switch (eventType)
        {
            case "SCORE":
                var scoreData = eventValue.Split(',');
                if (scoreData.Length >= 1 && int.TryParse(scoreData[0], out var scoreIncrease))
                {
                    _score += scoreIncrease;
                    // If total score is provided as second parameter, use it for sync
                    if (scoreData.Length >= 2 && int.TryParse(scoreData[1], out var syncScore))
                    {
                        _score = syncScore;
                    }
                    UpdateUI();
                    AddDebugMessage($"Pontuação: +{scoreIncrease} (Total: {_score})");
                }
                break;

            case "LEVEL":
                if (int.TryParse(eventValue, out var newLevel))
                {
                    _level = newLevel;
                    UpdateUI();
                    AddDebugMessage($"Nível aumentado: {newLevel}");
                }
                break;
            case "GAME_OVER":
                // Thread-safe check and set to prevent multiple GAME_OVER processing
                bool shouldProcessGameOver = false;
                lock (_gameStateLock)
                {
                    if (_gameActive && !_isSessionEnding)
                    {
                        shouldProcessGameOver = true;
                    }
                    else
                    {
                        AddDebugMessage("[EVENTO] GAME_OVER recebido, mas o jogo já não está ativo ou a sessão está a terminar. A ignorar.");
                    }
                }

                if (!shouldProcessGameOver) break;

                // Execute game over logic
                _gameActive = false;
                StartGameButton.IsEnabled = true;
                StopGameButton.IsEnabled = false;

                if (int.TryParse(eventValue, out var finalScore))
                {
                    _score = finalScore;
                }

                StatusText.Text = $"🎮 GAME OVER! Pontuação Final: {_score}";
                SaveGameScore();
                AddDebugMessage($"[EVENTO] GAME_OVER - Fim de jogo - Pontuação final: {_score}");
                TriggerVisualEffect("GAME_OVER");

                // For clients, check if session should end after game over
                if (_isClientMode && _currentUser != null)
                {
                    var remainingRounds = _sessionService.GetRemainingRounds(_currentUser.Id);
                    AddDebugMessage($"Após GAME_OVER - Rodadas restantes no jogo selecionado: {remainingRounds}");

                    if (remainingRounds <= 0)
                    {
                        lock (_gameStateLock)
                        {
                            _isSessionEnding = true;
                        }
                        AddDebugMessage($"Cliente {_currentUser.Name} esgotou todas as chances do jogo selecionado - iniciando fim de sessão");
                        
                        // Prevent multiple session dialogs
                        lock (_sessionDialogLock)
                        {
                            if (!_isShowingSessionDialog)
                            {
                                _isShowingSessionDialog = true;
                                Task.Run(async () => await ShowRoundsCompletedDialog());
                            }
                            else
                            {
                                AddDebugMessage("[SESSÃO] Diálogo de sessão já está sendo mostrado, ignorando");
                            }
                        }
                    }
                    else
                    {
                        // Still has chances in the selected game
                        var selectedGameName = _sessionService.GetClientSelectedGame(_currentUser.Id).GetDisplayName();
                        StatusText.Text = $"🎮 GAME OVER! Ainda tem {remainingRounds} chance(s) em {selectedGameName}";
                        AddDebugMessage($"Game over - cliente ainda tem {remainingRounds} chances no jogo selecionado");
                    }
                }
                break;

            case "STATUS":
                StatusText.Text = eventValue;
                break;

            case "HIT":
                var hitData = eventValue.Split(',');
                if (hitData.Length >= 2 && int.TryParse(hitData[0], out var ledHit) && int.TryParse(hitData[1], out var hitTotalScore))
                {
                    var pointsEarned = hitTotalScore - _score;
                    _score = hitTotalScore; // Sync with Arduino score
                    HighlightLed(ledHit);
                    StatusText.Text = $"🎯 Acertou LED {ledHit}! +{pointsEarned} pontos (Total: {_score})";
                    UpdateUI();
                    AddDebugMessage($"Acerto no LED {ledHit}, pontuação sincronizada: {_score}");
                }
                break;

            case "MISS":
                StatusText.Text = "❌ Muito lento! O LED apagou sozinho.";
                AddDebugMessage("[EVENTO] MISS - LED apagou antes do jogador pressionar");
                RecordClientRoundLoss();
                break;

            case "LED_ON":
                if (int.TryParse(eventValue, out var ledOnIndex))
                {
                    HighlightLed(ledOnIndex);
                    AddDebugMessage($"LED {ledOnIndex} aceso");
                }
                break;

            case "LED_OFF":
                if (int.TryParse(eventValue, out var ledOffIndex))
                {
                    RestoreLedColor(ledOffIndex);
                    AddDebugMessage($"LED {ledOffIndex} apagado");
                }
                break;

            case "WRONG_KEY":
                if (int.TryParse(eventValue, out var wrongKey))
                {
                    StatusText.Text = $"❌ Tecla errada! Pressionou {wrongKey}, mas deveria ser outro LED.";
                    AddDebugMessage($"[EVENTO] WRONG_KEY - Tecla incorreta pressionada: {wrongKey}");
                    RecordClientRoundLoss();
                }
                break;

            case "LEVEL_UP":
                var levelData = eventValue.Split(',');
                if (levelData.Length >= 1 && int.TryParse(levelData[0], out var level))
                {
                    _level = level;
                    if (levelData.Length >= 2 && int.TryParse(levelData[1], out var score))
                    {
                        _score = score;
                    }
                    StatusText.Text = $"🆙 NÍVEL {level}! Dificuldade aumentada! Pontuação: {_score}";
                    UpdateUI();
                    AddDebugMessage($"[EVENTO] LEVEL_UP - Nível: {level}, Pontuação: {_score}");
                    TriggerVisualEffect("LEVEL_UP");
                }
                break;

            case "GAME_STARTED":
                if (int.TryParse(eventValue, out var gameMode))
                {
                    _currentGameMode = gameMode;
                    _gameActive = true;
                    StartGameButton.IsEnabled = false;
                    StopGameButton.IsEnabled = true;
                    StatusText.Text = "🎮 Jogo iniciado! Prepare-se para a ação!";
                    AddDebugMessage($"[EVENTO] GAME_STARTED - Jogo iniciado: modo {gameMode} ({GetGameName(gameMode)})");
                    UpdateUI();
                    TriggerVisualEffect("GAME_START");
                }
                break;

            case "KEY_RELEASED":
                if (int.TryParse(eventValue, out var releasedKey))
                {
                    AddDebugMessage($"Tecla {releasedKey} liberada");
                }
                break;

            case "METEOR_HIT":
                if (int.TryParse(eventValue, out var meteorPos))
                {
                    StatusText.Text = "💥 IMPACTO! Um meteoro te atingiu! Game Over!";
                    HighlightLed(meteorPos);
                    AddDebugMessage($"Meteoro atingiu posição: {meteorPos}");
                    RecordClientRoundLoss();
                }
                break;

            case "NOTE_HIT":
                var noteHitData = eventValue.Split(',');
                if (noteHitData.Length >= 1 && int.TryParse(noteHitData[0], out var column))
                {
                    var pointsEarned = 10;
                    if (noteHitData.Length >= 2 && int.TryParse(noteHitData[1], out var noteTotalScore))
                    {
                        pointsEarned = noteTotalScore - _score;
                        _score = noteTotalScore;
                    }
                    else
                    {
                        _score += pointsEarned;
                    }
                    StatusText.Text = $"🎵 NOTA PERFEITA! Coluna {column} +{pointsEarned} pontos (Total: {_score})";
                    UpdateUI();
                    AddDebugMessage($"Nota acertada coluna {column}, pontuação: {_score}");
                }
                break;

            case "NOTE_MISS":
                StatusText.Text = "🎵 Nota perdida! Muito cedo ou muito tarde. Siga o ritmo!";
                RecordClientRoundLoss();
                break;

            case "SEQUENCE_START":
                StatusText.Text = "👀 ATENÇÃO! Memorize a sequência de LEDs que vai piscar...";
                break;

            case "SEQUENCE_REPEAT":
                StatusText.Text = "🔄 Sua vez! Repita a sequência na mesma ordem.";
                break;

            case "PLAYER_MOVE":
                if (int.TryParse(eventValue, out var playerPos))
                {
                    ClearLedMatrix();
                    HighlightLed(playerPos);
                }
                break;

            case "OBSTACLE":
                if (int.TryParse(eventValue, out var obstaclePos))
                {
                    var row = obstaclePos / 4;
                    var col = obstaclePos % 4;
                    if (row < 4 && col < 4)
                    {
                        _ledMatrix[row, col].Fill = Brushes.Red;
                    }
                }
                break;

            case "CLEAR":
                ClearLedMatrix();
                break;

            // Roleta Russa Events
            case "ROLETA_ROUND_START":
                var roletaData = eventValue.Split(',');
                if (roletaData.Length >= 2)
                {
                    StatusText.Text = $"🎲 Roleta Russa - Rodada {roletaData[0]} | Multiplicador: {roletaData[1]}x | Escolha um LED!";
                }
                break;

            case "ROLETA_SAFE":
                StatusText.Text = "💚 SEGURO! Parabéns! Pontuação multiplicada. Continuar para próxima rodada?";
                break;

            case "ROLETA_EXPLODE":
                StatusText.Text = "💥 EXPLODIU! Era o LED com bomba. Perdeu toda a pontuação!";
                ClearLedMatrix();
                TriggerVisualEffect("EXPLOSION");
                RecordClientRoundLoss();
                break;

            case "ROLETA_MAX_WIN":
                StatusText.Text = "🏆 VITÓRIA MÁXIMA! Você é corajoso demais!";
                TriggerVisualEffect("VICTORY");
                break;

            // Lightning Strike Events
            case "LIGHTNING_PATTERN_SHOW":
                var lightningData = eventValue.Split(',');
                if (lightningData.Length >= 2)
                {
                    StatusText.Text = $"⚡ Lightning Strike! Memorize {lightningData[0]} LEDs em apenas {lightningData[1]}ms!";
                }
                break;

            case "LIGHTNING_INPUT_START":
                StatusText.Text = "⚡ RÁPIDO! Reproduza o padrão que você viu na ordem correta!";
                break;

            case "LIGHTNING_COMPLETE":
                StatusText.Text = "⚡ PERFEITO! Reflexos incríveis! Próximo nível será mais difícil...";
                break;

            case "LIGHTNING_WRONG":
                StatusText.Text = "❌ Errou! O padrão correto está sendo mostrado agora. Game Over!";
                RecordClientRoundLoss();
                break;

            // Sniper Mode Events
            case "SNIPER_TARGET_SPAWN":
                StatusText.Text = "🎯 ALVO À VISTA! Você tem 0.1 segundo para atirar!";
                break;

            case "SNIPER_HIT":
                var sniperData = eventValue.Split(',');
                if (sniperData.Length >= 2)
                {
                    StatusText.Text = $"🎯 TIRO CERTEIRO! Acertos: {sniperData[0]}/10 | Tempo: {sniperData[1]}ms";
                }
                break;

            case "SNIPER_MISS":
                StatusText.Text = "❌ Tiro errado! Mirou no lugar errado ou muito devagar.";
                RecordClientRoundLoss();
                break;

            case "SNIPER_TIMEOUT":
                StatusText.Text = "⏰ MUITO LENTO! O alvo desapareceu antes de você atirar.";
                RecordClientRoundLoss();
                break;

            case "SNIPER_VICTORY":
                StatusText.Text = "🏆 LEGENDÁRIO! 10/10 acertos! Você é um sniper de elite!";
                TriggerVisualEffect("VICTORY");
                break;

            case "GATO_RATO_TIMEOUT":
                if (int.TryParse(eventValue, out var captures))
                {
                    StatusText.Text = $"⏰ TEMPO ESGOTADO! Você capturou {captures} ratos em 2 minutos. Sessão finalizada!";
                    AddDebugMessage($"[NEGÓCIO] Gato e Rato timeout - {captures} capturas - Sessão sendo finalizada por regra de negócio");

                    // Business rule: timeout ends the session permanently for this client
                    if (_isClientMode && _currentUser != null)
                    {
                        AddDebugMessage($"[NEGÓCIO] Cliente {_currentUser.Name} atingiu timeout no Gato e Rato - finalizando sessão permanentemente");

                        // Mark session as completed due to timeout with business rule
                        _sessionService.EndSessionByTimeout(_currentUser.Id, "Timeout de 2 minutos no jogo Gato e Rato");

                        // Set flag to prevent further gameplay
                        lock (_gameStateLock)
                        {
                            _isSessionEnding = true;
                        }

                        // Show timeout completion dialog after a brief delay
                        Task.Run(async () =>
                        {
                            await Task.Delay(2000); // Give time to read the timeout message
                            await ShowTimeoutCompletedDialog(captures);
                        });
                    }
                }
                break;

            case "GATO_RATO_WIN":
                StatusText.Text = "🏆 VITÓRIA! Você capturou todos os ratos necessários!";
                TriggerVisualEffect("VICTORY");
                break;

            case "COMBO":
                if (int.TryParse(eventValue, out var comboCount))
                {
                    StatusText.Text = $"🔥 COMBO x{comboCount}! Pontuação multiplicada!";
                    TriggerVisualEffect("COMBO");
                }
                break;

            case "PERFECT":
                StatusText.Text = "⭐ PERFEITO! Timing excelente!";
                TriggerVisualEffect("PERFECT_HIT");
                break;

            case "GOOD":
                StatusText.Text = "👍 Bom timing!";
                break;

            case "REACTION_TIME":
                if (int.TryParse(eventValue, out var reactionMs))
                {
                    StatusText.Text = $"⚡ Tempo de reação: {reactionMs}ms";
                }
                break;

            // Additional missing events
            case "TARGET_MISSED":
                StatusText.Text = "❌ Alvo perdido! Muito devagar.";
                RecordClientRoundLoss();
                break;

            case "SPEED_BONUS":
                if (int.TryParse(eventValue, out var bonus))
                {
                    _score += bonus;
                    StatusText.Text = $"🚀 BÔNUS DE VELOCIDADE! +{bonus} pontos extras!";
                    UpdateUI();
                }
                break;

            case "PENALTY":
                if (int.TryParse(eventValue, out var penalty))
                {
                    _score = Math.Max(0, _score - penalty);
                    StatusText.Text = $"⚠️ Penalidade! -{penalty} pontos";
                    UpdateUI();
                }
                break;

            case "COUNTDOWN":
                StatusText.Text = $"⏰ {eventValue}";
                break;

            case "ROUND_COMPLETE":
                StatusText.Text = "✅ Rodada completa! Preparando próxima...";
                break;

            case "TIME_WARNING":
                StatusText.Text = "⚠️ ATENÇÃO! Tempo acabando!";
                break;

            case "BONUS_ROUND":
                StatusText.Text = "⭐ RODADA BÔNUS! Pontuação dobrada!";
                break;

            case "NEW_RECORD":
                StatusText.Text = "🏆 NOVO RECORDE! Parabéns!";
                TriggerVisualEffect("FIREWORKS");
                break;

            case "STREAK":
                if (int.TryParse(eventValue, out var streak))
                {
                    StatusText.Text = $"🔥 SEQUÊNCIA! {streak} acertos consecutivos!";
                }
                break;

            case "DIFFICULTY_UP":
                StatusText.Text = "📈 Dificuldade aumentada! Prepare-se!";
                break;

            // Handle simple status messages without parameters
            case "READY":
                StatusText.Text = "🟢 Arduino pronto! Selecione um jogo e aperte Iniciar.";
                break;

            default:
                AddDebugMessage($"Evento desconhecido: {eventType} = {eventValue}");
                break;
        }
    }

    private void PopulateGameModeComboBox()
    {
        GameModeComboBox.Items.Clear();

        var games = new[]
        {
            new { Id = 1, Name = "🎯 Pega-Luz", Description = "Reflexos rápidos" },
            new { Id = 2, Name = "🧠 Sequência Maluca", Description = "Memória" },
            new { Id = 3, Name = "🐱 Gato e Rato", Description = "Perseguição" },
            new { Id = 4, Name = "☄️ Esquiva Meteoros", Description = "Sobrevivência" },
            new { Id = 5, Name = "🎸 Guitar Hero", Description = "Ritmo" },
            new { Id = 6, Name = "🎲 Roleta Russa", Description = "Sorte e Coragem" },
            new { Id = 7, Name = "⚡ Lightning Strike", Description = "Velocidade Extrema" },
            new { Id = 8, Name = "🎯 Sniper Mode", Description = "Precisão Máxima" }
        };

        foreach (var game in games)
        {
            var item = new ComboBoxItem
            {
                Content = game.Name,
                Tag = game.Id
            };
            GameModeComboBox.Items.Add(item);
        }
    }

    private void TriggerVisualEffect(string effectType)
    {
        if (_serialPort?.IsOpen != true) return;

        try
        {
            switch (effectType.ToUpper())
            {
                case "CONNECTION_SUCCESS":
                    // Already handled by Arduino after INIT
                    break;
                case "GAME_START":
                    // Already handled by Arduino in START_GAME
                    break;
                case "GAME_OVER":
                    // Already handled by Arduino in STOP_GAME
                    break;
                case "LEVEL_UP":
                    // Already handled by Arduino
                    break;
                case "PERFECT_HIT":
                    // Already handled by Arduino
                    break;
                case "COMBO":
                    // Already handled by Arduino
                    break;
                case "EXPLOSION":
                    // Already handled by Arduino
                    break;
                case "VICTORY":
                    // Already handled by Arduino
                    break;
                case "FIREWORKS":
                    _serialPort.WriteLine("EFFECT_FIREWORKS");
                    break;
                case "RAINBOW":
                    _serialPort.WriteLine("EFFECT_RAINBOW");
                    break;
                case "MATRIX":
                    _serialPort.WriteLine("EFFECT_MATRIX");
                    break;
                case "PULSE":
                    _serialPort.WriteLine("EFFECT_PULSE");
                    break;
                case "STOP_EFFECTS":
                    _serialPort.WriteLine("STOP_EFFECTS");
                    break;
            }
            AddDebugMessage($"Efeito visual disparado: {effectType}");
        }
        catch (Exception ex)
        {
            AddDebugMessage($"Erro ao disparar efeito visual: {ex.Message}");
        }
    }

    private void UpdateUI()
    {
        ScoreText.Text = _score.ToString();
        LevelText.Text = _level.ToString();

        if (!string.IsNullOrEmpty(_playerName))
        {
            PlayerDisplayText.Text = $"👤 Jogador: {_playerName}";
        }

        if (_currentGameMode > 0 && _currentGameMode <= _gameDescriptions.Count)
        {
            var gameNames = new[] { "", "Pega-Luz", "Sequência Maluca", "Gato e Rato", "Esquiva Meteoros", "Guitar Hero", "Roleta Russa", "Lightning Strike", "Sniper Mode" };
            if (_currentGameMode < gameNames.Length)
            {
                CurrentGameText.Text = gameNames[_currentGameMode];
            }
        }
    }

    private void HighlightLed(int ledIndex)
    {
        if (_disposed || ledIndex < 0 || ledIndex >= 16) return;

        var row = ledIndex / 4;
        var col = ledIndex % 4;

        if (row < 4 && col < 4)
        {
            // Set active color with enhanced visual effect
            _ledMatrix[row, col].Fill = GetLedActiveColor(row);
            
            // Add glow effect
            _ledMatrix[row, col].Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(79, 172, 254), // #4FACFE
                BlurRadius = 15,
                OffsetX = 0,
                OffsetY = 0
            };

            // Auto-restore LED after 200ms as fallback (in case KeyUp is missed)
            lock (_ledTimersLock)
            {
                // Cancel existing timer for this LED if any
                if (_ledTimers.TryGetValue(ledIndex, out var existingTimer))
                {
                    existingTimer.Dispose();
                    _ledTimers.Remove(ledIndex);
                }

                // Create new timer to restore LED color
                var timer = new System.Threading.Timer(
                    _ =>
                    {
                        if (!_disposed)
                        {
                            try
                            {
                                Dispatcher.UIThread.InvokeAsync(() => RestoreLedColor(ledIndex));
                            }
                            catch (Exception ex)
                            {
                                AddDebugMessage($"Erro ao restaurar LED {ledIndex}: {ex.Message}");
                            }
                        }

                        // Clean up timer
                        lock (_ledTimersLock)
                        {
                            if (_ledTimers.TryGetValue(ledIndex, out var timerToDispose))
                            {
                                timerToDispose.Dispose();
                                _ledTimers.Remove(ledIndex);
                            }
                        }
                    },
                    null,
                    TimeSpan.FromMilliseconds(200),
                    Timeout.InfiniteTimeSpan
                );

                _ledTimers[ledIndex] = timer;
            }
        }
    }

    private IBrush GetLedDefaultColor(int row)
    {
        return row switch
        {
            0 => new SolidColorBrush(Color.FromRgb(60, 15, 30)),     // Dark purple-red
            1 => new SolidColorBrush(Color.FromRgb(30, 45, 75)),     // Dark blue
            2 => new SolidColorBrush(Color.FromRgb(25, 35, 65)),     // Darker blue
            3 => new SolidColorBrush(Color.FromRgb(45, 25, 60)),     // Dark purple
            _ => new SolidColorBrush(Color.FromRgb(30, 30, 60))      // Dark blue-gray
        };
    }

    private IBrush GetLedActiveColor(int row)
    {
        return row switch
        {
            0 => new SolidColorBrush(Color.FromRgb(79, 172, 254)),  // Bright cyan-blue #4FACFE
            1 => new SolidColorBrush(Color.FromRgb(0, 242, 254)),   // Bright cyan #00F2FE
            2 => new SolidColorBrush(Color.FromRgb(183, 148, 246)), // Light purple #B794F6
            3 => new SolidColorBrush(Color.FromRgb(102, 126, 234)), // Blue-purple #667EEA
            _ => new SolidColorBrush(Color.FromRgb(79, 172, 254))   // Default cyan
        };
    }

    private void ClearLedMatrix()
    {
        if (_disposed) return;

        // Clear all active timers first
        lock (_ledTimersLock)
        {
            foreach (var timer in _ledTimers.Values)
            {
                timer?.Dispose();
            }
            _ledTimers.Clear();
        }

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                _ledMatrix[row, col].Fill = GetLedDefaultColor(row);
                _ledMatrix[row, col].Effect = null; // Remove any glow effects
            }
        }
    }

    private int GetKeyForLed(int ledIndex)
    {
        // Convert LED index to key code
        return ledIndex switch
        {
            0 => (int)Key.D0,
            1 => (int)Key.D1,
            2 => (int)Key.D2,
            3 => (int)Key.D3,
            4 => (int)Key.D4,
            5 => (int)Key.D5,
            6 => (int)Key.D6,
            7 => (int)Key.D7,
            8 => (int)Key.D8,
            9 => (int)Key.D9,
            10 => (int)Key.A,
            11 => (int)Key.B,
            12 => (int)Key.C,
            13 => (int)Key.D,
            14 => (int)Key.E,
            15 => (int)Key.F,
            _ => -1
        };
    }

    private int GetLedForKey(Key key)
    {
        return key switch
        {
            Key.D0 => 0,
            Key.D1 => 1,
            Key.D2 => 2,
            Key.D3 => 3,
            Key.D4 => 4,
            Key.D5 => 5,
            Key.D6 => 6,
            Key.D7 => 7,
            Key.D8 => 8,
            Key.D9 => 9,
            Key.A => 10,
            Key.B => 11,
            Key.C => 12,
            Key.D => 13,
            Key.E => 14,
            Key.F => 15,
            _ => -1
        };

    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        // Handle function keys
        switch (e.Key)
        {
            case Key.F1:
                if (StartGameButton.IsEnabled) StartGameButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F2:
                if (StopGameButton.IsEnabled) StopGameButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F3:
                ResetScoreButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F4:
                ViewScoresButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F5:
                // Efeito Rainbow
                TriggerVisualEffect("RAINBOW");
                StatusText.Text = "🌈 Efeito Arco-íris ativado! (F6 para parar)";
                e.Handled = true;
                return;
            case Key.F6:
                // Parar todos os efeitos
                TriggerVisualEffect("STOP_EFFECTS");
                StatusText.Text = "⏹️ Efeitos visuais interrompidos.";
                e.Handled = true;
                return;
            case Key.F7:
                // Efeito Matrix
                TriggerVisualEffect("MATRIX");
                StatusText.Text = "💚 Matrix Rain ativado! (F6 para parar)";
                e.Handled = true;
                return;
            case Key.F8:
                // Efeito Pulse
                TriggerVisualEffect("PULSE");
                StatusText.Text = "💓 Pulso Universal ativado! (F6 para parar)";
                e.Handled = true;
                return;
            case Key.F9:
                // Fogos de artifício
                TriggerVisualEffect("FIREWORKS");
                StatusText.Text = "🎆 Fogos de artifício! Efeito único de 2 segundos.";
                e.Handled = true;
                return;
            case Key.F10:
                // Demo completa dos efeitos
                if (_serialPort?.IsOpen == true)
                {
                    VisualEffectsDemo_Click(null, new RoutedEventArgs());
                }
                e.Handled = true;
                return;
            case Key.F11:
                // Toggle full-screen
                ToggleFullScreen();
                e.Handled = true;
                return;
        }

        if (!_gameActive || _serialPort?.IsOpen != true) return;

        string command = "";

        // Handle LED keys (0-9, A-F)
        var ledIndex = GetLedForKey(e.Key);
        if (ledIndex >= 0)
        {
            command = $"KEY_PRESS:{ledIndex}";
            HighlightLed(ledIndex);
        }
        // Handle arrow keys
        else if (e.Key == Key.Up) command = "MOVE:UP";
        else if (e.Key == Key.Down) command = "MOVE:DOWN";
        else if (e.Key == Key.Left) command = "MOVE:LEFT";
        else if (e.Key == Key.Right) command = "MOVE:RIGHT";
        else if (e.Key == Key.Enter) command = "ACTION:CONFIRM";
        else if (e.Key == Key.Escape) command = "ACTION:CANCEL";

        if (!string.IsNullOrEmpty(command))
        {
            _serialPort.WriteLine(command);
            AddDebugMessage($"Comando enviado: {command}");
            e.Handled = true;
        }
    }

    private void Window_KeyUp(object? sender, KeyEventArgs e)
    {
        if (!_gameActive || _serialPort?.IsOpen != true) return;

        // Handle LED keys (0-9, A-F) - restore original color when key is released
        var ledIndex = GetLedForKey(e.Key);
        if (ledIndex >= 0)
        {
            RestoreLedColor(ledIndex);

            // Send key release command to Arduino
            string command = $"KEY_RELEASE:{ledIndex}";
            _serialPort.WriteLine(command);
            AddDebugMessage($"Comando enviado: {command}");

            e.Handled = true;
        }
    }

    private void RestoreLedColor(int ledIndex)
    {
        if (_disposed || ledIndex < 0 || ledIndex >= 16) return;

        var row = ledIndex / 4;
        var col = ledIndex % 4;

        if (row < 4 && col < 4)
        {
            _ledMatrix[row, col].Fill = GetLedDefaultColor(row);
            // Remove glow effect
            _ledMatrix[row, col].Effect = null;
        }

        // Clear the timer for this LED
        lock (_ledTimersLock)
        {
            if (_ledTimers.TryGetValue(ledIndex, out var timer))
            {
                timer.Dispose();
                _ledTimers.Remove(ledIndex);
            }
        }
    }

    private void SavePlayerButton_Click(object? sender, RoutedEventArgs e)
    {
        // This is only for admin mode now
        if (_isClientMode) return;

        var name = PlayerNameTextBox.Text?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            _playerName = name;
            PlayerDisplayText.Text = $"👤 Jogador: {name}";
        }
    }

    private void GameModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Prevent ALL game mode changes for clients - they can only play the selected game
        if (_isClientMode)
        {
            AddDebugMessage("Tentativa de mudar jogo bloqueada - cliente pode jogar apenas o jogo selecionado na sessão");
            // Reset combo box to current game mode
            GameModeComboBox.SelectedIndex = _currentGameMode - 1;
            return;
        }

        if (GameModeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            if (int.TryParse(selectedItem.Tag?.ToString(), out var gameMode))
            {
                _currentGameMode = gameMode;

                // Update description
                if (_gameDescriptions.TryGetValue(gameMode, out var description))
                {
                    GameDescriptionText.Text = description;
                }

                // Update current game display
                CurrentGameText.Text = selectedItem.Content?.ToString()?.Substring(2) ?? "Desconhecido"; // Remove emoji

                AddDebugMessage($"Jogo selecionado: {CurrentGameText.Text} (ID: {gameMode})");

                // Enable start button if connected (only for admin or if client can play this game)
                if (_serialPort?.IsOpen == true)
                {
                    if (_isClientMode && _currentUser != null)
                    {
                        var canPlay = _sessionService.CanClientPlayGame(_currentUser.Id);
                        StartGameButton.IsEnabled = canPlay;
                    }
                    else
                    {
                        StartGameButton.IsEnabled = true;
                    }
                }

                // Update remaining rounds display for clients
                if (_isClientMode)
                {
                    UpdateRemainingRoundsDisplay();
                }
            }
        }
    }

    private void OpenDebugButton_Click(object? sender, RoutedEventArgs e)
    {
        // Both admins and clients can access debug for troubleshooting
        if (_debugWindow == null)
        {
            _debugWindow = new DebugWindow();
            _debugWindow.Closed += (s, args) => _debugWindow = null;

            // Configure debug window based on user type
            if (_isClientMode && _currentUser != null)
            {
                _debugWindow.Title = $"🔧 Debug Console - Cliente: {_currentUser.Name}";
                _debugWindow.OnRefreshClientInfo = ShowClientDebugInfo;
                _debugWindow.SetRefreshButtonVisibility(true);
                ShowClientDebugInfo();
            }
            else
            {
                _debugWindow.Title = "🔧 Debug Console - Administrador";
                _debugWindow.SetRefreshButtonVisibility(false);
                _debugWindow.AddMessage("[INFO] Modo Administrador - Acesso completo", true);
            }
        }
        _debugWindow.Show();
        _debugWindow.Activate();
    }

    private void ShowClientDebugInfo()
    {
        if (_debugWindow == null || _currentUser == null) return;

        try
        {
            _debugWindow.AddMessage("═══════════════════════════════════════", true);
            _debugWindow.AddMessage($"[INFO] 👤 Cliente: {_currentUser.Name}", true);
            _debugWindow.AddMessage($"[INFO] 🆔 ID: {_currentUser.Id}", true);

            var sessionStatus = _sessionService.GetClientSessionStatus(_currentUser.Id);
            _debugWindow.AddMessage($"[INFO] 📊 Status da Sessão: {sessionStatus}", true);

            var selectedGame = _sessionService.GetClientSelectedGame(_currentUser.Id);
            _debugWindow.AddMessage($"[INFO] 🎮 Jogo Selecionado: {selectedGame.GetDisplayName()}", true);

            var remainingRounds = _sessionService.GetRemainingRounds(_currentUser.Id);
            _debugWindow.AddMessage($"[INFO] 🔄 Rodadas Restantes: {remainingRounds}", true);

            var session = _sessionService.GetSession(_currentUser.Id);
            if (session != null)
            {
                _debugWindow.AddMessage($"[INFO] ❌ Erros Cometidos: {session.ErrorsCommitted}/{session.MaxErrors}", true);
                _debugWindow.AddMessage($"[INFO] ⏰ Sessão Iniciada: {session.SessionStart:HH:mm:ss}", true);
                _debugWindow.AddMessage($"[INFO] ✅ Sessão Ativa: {(session.IsActive ? "Sim" : "Não")}", true);
            }

            _debugWindow.AddMessage($"[INFO] 🔗 Arduino Conectado: {(_serialPort?.IsOpen == true ? "Sim" : "Não")}", true);
            _debugWindow.AddMessage($"[INFO] 🎯 Jogo Ativo: {(_gameActive ? "Sim" : "Não")}", true);
            _debugWindow.AddMessage($"[INFO] 🏆 Pontuação Atual: {_score}", true);
            _debugWindow.AddMessage($"[INFO] 📈 Nível Atual: {_level}", true);

            _debugWindow.AddMessage("═══════════════════════════════════════", true);
            _debugWindow.AddMessage("[INFO] 💡 Dica: Esta janela mostra informações técnicas que podem ajudar", true);
            _debugWindow.AddMessage("[INFO] 💡 a resolver problemas durante o jogo. Mantenha-a aberta se", true);
            _debugWindow.AddMessage("[INFO] 💡 estiver enfrentando dificuldades.", true);
            _debugWindow.AddMessage("═══════════════════════════════════════", true);
        }
        catch (Exception ex)
        {
            _debugWindow.AddMessage($"[ERROR] Erro ao obter informações de debug: {ex.Message}", false);
        }
    }

    private async void StartGameButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen != true)
        {
            await ShowMessage("Erro", "Arduino não está conectado!");
            return;
        }

        if (string.IsNullOrWhiteSpace(_playerName))
        {
            await ShowMessage("Aviso", "Por favor, defina seu nome antes de iniciar o jogo!");
            return;
        }

        if (_currentGameMode == 0)
        {
            await ShowMessage("Erro", "Selecione um jogo primeiro!");
            return;
        }

        // Check if client can play this game
        if (_isClientMode && _currentUser != null)
        {
            var gameMode = (GameMode)_currentGameMode;
            AddDebugMessage($"[INÍCIO] 🎮 Cliente {_currentUser.Name} tentando iniciar jogo {gameMode.GetDisplayName()}");

            // Check if session is blocked due to timeout (business rule)
            bool isSessionEnding;
            lock (_gameStateLock)
            {
                isSessionEnding = _isSessionEnding;
            }

            if (isSessionEnding)
            {
                AddDebugMessage($"[INÍCIO] 🚫 Cliente bloqueado - Sessão finalizada por timeout ou regra de negócio");
                await ShowMessage("Sessão Finalizada",
                    "Sua sessão foi finalizada permanentemente devido ao timeout no jogo Gato e Rato.\n" +
                    "Para jogar novamente, faça logout e entre com uma nova sessão.");
                return;
            }

            if (!_sessionService.CanClientPlayGame(_currentUser.Id))
            {
                var remaining = _sessionService.GetRemainingRounds(_currentUser.Id);
                AddDebugMessage($"[INÍCIO] ❌ Cliente bloqueado - Erros restantes: {remaining}");
                await ShowMessage("Limite de Erros Atingido",
                    $"Você já cometeu o máximo de erros permitidos em {gameMode.GetDisplayName()}!\n" +
                    $"Erros restantes: {remaining}");
                return;
            }

            AddDebugMessage($"[INÍCIO] ✅ Cliente autorizado a jogar - Rodadas restantes: {_sessionService.GetRemainingRounds(_currentUser.Id)}");
        }

        _gameActive = true;
        _score = 0;
        _level = 1;
        _gameStartTime = DateTime.Now;

        StartGameButton.IsEnabled = false;
        StopGameButton.IsEnabled = true;

        var command = $"START_GAME:{_currentGameMode}";
        _serialPort.WriteLine(command);

        StatusText.Text = "🚀 Jogo iniciado! Boa sorte!";

        if (_isClientMode && _currentUser != null)
        {
            AddDebugMessage($"[INÍCIO] 🚀 Cliente {_currentUser.Name} iniciou jogo {GetGameName(_currentGameMode)} - Comando: {command}");
        }
        else
        {
            AddDebugMessage($"[INÍCIO] 🚀 Administrador iniciou jogo {GetGameName(_currentGameMode)} - Comando: {command}");
        }

        UpdateUI();
    }

    private void StopGameButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.WriteLine("STOP_GAME");
        }

        _gameActive = false;
        StartGameButton.IsEnabled = true;
        StopGameButton.IsEnabled = false;

        StatusText.Text = "⏹️ Jogo interrompido pelo jogador.";
        AddDebugMessage("Jogo interrompido pelo usuário");

        if (_score > 0)
        {
            SaveGameScore();
        }

        ClearLedMatrix();
    }

    private void ResetScoreButton_Click(object? sender, RoutedEventArgs e)
    {
        _score = 0;
        _level = 1;
        UpdateUI();
        AddDebugMessage("Pontuação resetada");
        StatusText.Text = "🔄 Pontuação resetada!";
    }

    private async void RefreshPortsButton_Click(object? sender, RoutedEventArgs e)
    {
        // Visual feedback for button click
        if (sender is Button button)
        {
            var originalContent = button.Content;
            button.Content = "🔄 Atualizando...";
            button.IsEnabled = false;
            
            await Task.Delay(500); // Small delay for visual feedback
            
            RefreshPorts();
            
            button.Content = originalContent;
            button.IsEnabled = true;
        }
        else
        {
            RefreshPorts();
        }
    }

    private async void ViewScoresButton_Click(object? sender, RoutedEventArgs e)
    {
        var scoresWindow = new Views.ScoresWindow(_scoreService);
        await scoresWindow.ShowDialog(this);
    }

    private void SaveGameScore()
    {
        if (!string.IsNullOrWhiteSpace(_playerName) && _score > 0)
        {
            var gameNames = new[] { "", "Pega-Luz", "Sequência Maluca", "Gato e Rato", "Esquiva Meteoros", "Guitar Hero", "Roleta Russa", "Lightning Strike", "Sniper Mode" };
            var gameName = _currentGameMode < gameNames.Length ? gameNames[_currentGameMode] : "Desconhecido";

            var duration = DateTime.Now - _gameStartTime;
            var score = new GameScore
            {
                PlayerName = _playerName,
                GameMode = gameName,
                Score = _score,
                Level = _level,
                Duration = duration,
                PlayedAt = DateTime.Now
            };

            _scoreService.SaveScore(score);
            AddDebugMessage($"Pontuação salva: {_playerName} - {_score} pontos");
        }
    }

    private async void QuickStartButton_Click(object? sender, RoutedEventArgs e)
    {
        // Quick start with default settings
        if (_serialPort?.IsOpen != true)
        {
            await ShowMessage("Aviso", "Conecte o Arduino primeiro para usar o Início Rápido!");
            return;
        }

        if (string.IsNullOrWhiteSpace(_playerName))
        {
            _playerName = "Jogador";
            PlayerNameTextBox.Text = _playerName;
            PlayerDisplayText.Text = $"👤 Jogador: {_playerName}";
        }

        if (_currentGameMode == 0)
        {
            GameModeComboBox.SelectedIndex = 0; // Select first game
        }

        StartGameButton_Click(sender, e);
    }

    private async void InstructionsButton_Click(object? sender, RoutedEventArgs e)
    {
        var instructionsWindow = new InstructionsWindow(_gameInstructions);
        await instructionsWindow.ShowDialog(this);
    }

    private async void VisualEffectsDemo_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen != true)
        {
            await ShowMessage("Aviso", "Conecte o Arduino primeiro para ver os efeitos visuais!");
            return;
        }

        StatusText.Text = "🎆 Demonstração de Efeitos Visuais em andamento...";

        // Sequência de demonstração dos efeitos
        var effects = new[]
        {
            ("RAINBOW", "🌈 Onda Arco-íris", 3000),
            ("MATRIX", "💚 Matrix Rain", 3000),
            ("PULSE", "💓 Pulso Universal", 2000),
            ("FIREWORKS", "🎆 Fogos de Artifício", 2000)
        };

        foreach (var (effect, description, duration) in effects)
        {
            StatusText.Text = $"🎭 {description}";
            TriggerVisualEffect(effect);
            await Task.Delay(duration);
            TriggerVisualEffect("STOP_EFFECTS");
            await Task.Delay(500);
        }

        StatusText.Text = "✨ Demonstração concluída! Que tal jogar agora?";
        TriggerVisualEffect("FIREWORKS"); // Grande final
        await Task.Delay(2000);
        TriggerVisualEffect("STOP_EFFECTS");
    }

    public async void TestAllVisualEffects()
    {
        if (_serialPort?.IsOpen != true) return;

        // Teste rápido de todos os efeitos para debug
        var testEffects = new[] { "RAINBOW", "MATRIX", "PULSE", "FIREWORKS" };

        foreach (var effect in testEffects)
        {
            AddDebugMessage($"Testando efeito: {effect}");
            TriggerVisualEffect(effect);
            await Task.Delay(1000);
            TriggerVisualEffect("STOP_EFFECTS");
            await Task.Delay(200);
        }
    }

    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new Window
        {
            Title = "⚙️ Configurações",
            Width = 600,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(15, 15, 35)) // Dark background #0F0F23
        };

        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };

        stackPanel.Children.Add(new TextBlock
        {
            Text = "⚙️ Configurações do Jogo",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        });

        // Player name setting
        var playerPanel = new StackPanel { Spacing = 8 };
        playerPanel.Children.Add(new TextBlock 
        { 
            Text = "👤 Nome do Jogador:",
            Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // #E2E8F0
            FontWeight = FontWeight.Medium
        });
        var playerTextBox = new TextBox 
        { 
            Text = _playerName, 
            Watermark = "Digite seu nome...",
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(58, 58, 107), 0),  // #3A3A6B
                    new GradientStop(Color.FromRgb(74, 74, 123), 1)   // #4A4A7B
                }
            },
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 254)), // #4FACFE
            CornerRadius = new CornerRadius(8),
            Padding = new Avalonia.Thickness(12, 8)
        };
        playerPanel.Children.Add(playerTextBox);
        stackPanel.Children.Add(playerPanel);

        // Serial port settings
        var portPanel = new StackPanel { Spacing = 8 };
        portPanel.Children.Add(new TextBlock 
        { 
            Text = "🔌 Porta Serial:",
            Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // #E2E8F0
            FontWeight = FontWeight.Medium
        });
        var portCombo = new ComboBox
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(58, 58, 107), 0),  // #3A3A6B
                    new GradientStop(Color.FromRgb(74, 74, 123), 1)   // #4A4A7B
                }
            },
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(79, 172, 254)), // #4FACFE
            CornerRadius = new CornerRadius(8),
            Padding = new Avalonia.Thickness(12, 8)
        };
        foreach (var port in SerialPort.GetPortNames())
        {
            portCombo.Items.Add(port);
        }
        if (PortComboBox.SelectedItem != null)
        {
            portCombo.SelectedItem = PortComboBox.SelectedItem;
        }
        portPanel.Children.Add(portCombo);
        stackPanel.Children.Add(portPanel);

        // Buttons
        var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

        var saveButton = new Button 
        { 
            Content = "💾 Salvar", 
            MinWidth = 100,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(79, 172, 254), 0),  // #4FACFE
                    new GradientStop(Color.FromRgb(0, 242, 254), 1)    // #00F2FE
                }
            },
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Avalonia.Thickness(15, 8),
            FontWeight = FontWeight.SemiBold
        };
        saveButton.Click += (s, e) =>
        {
            _playerName = playerTextBox.Text ?? "";
            PlayerNameTextBox.Text = _playerName;
            if (!string.IsNullOrWhiteSpace(_playerName))
            {
                PlayerDisplayText.Text = $"👤 Jogador: {_playerName}";
            }

            if (portCombo.SelectedItem != null)
            {
                PortComboBox.SelectedItem = portCombo.SelectedItem;
            }

            settingsWindow.Close();
        };

        var cancelButton = new Button 
        { 
            Content = "❌ Cancelar", 
            MinWidth = 100,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.FromRgb(102, 126, 234), 0),  // #667EEA
                    new GradientStop(Color.FromRgb(118, 75, 162), 1)    // #764BA2
                }
            },
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Avalonia.Thickness(15, 8),
            FontWeight = FontWeight.SemiBold
        };
        cancelButton.Click += (s, e) => settingsWindow.Close();

        buttonPanel.Children.Add(saveButton);
        buttonPanel.Children.Add(cancelButton);
        stackPanel.Children.Add(buttonPanel);

        settingsWindow.Content = stackPanel;
        await settingsWindow.ShowDialog(this);
    }

    private async void HelpButton_Click(object? sender, RoutedEventArgs e)
    {
        var helpWindow = new Window
        {
            Title = "❓ Ajuda - Mini Jogo LEDs",
            Width = 600,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var scrollViewer = new ScrollViewer();
        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };

        stackPanel.Children.Add(new TextBlock
        {
            Text = "❓ Ajuda - Mini Jogo LEDs",
            FontSize = 20,
            FontWeight = FontWeight.Bold
        });

        var helpText = @"🎮 COMO JOGAR

🔌 CONEXÃO:
1. Conecte o Arduino via USB
2. Selecione a porta correta
3. Clique em 'Conectar'

👤 CONFIGURAÇÃO:
1. Digite seu nome
2. Escolha um jogo
3. Clique em 'Iniciar' ou use F1

⌨️ CONTROLES:
• 0-9, A-F: Pressionar LEDs específicos
• Setas: Mover cursor/personagem
• Enter: Confirmar ação
• Esc: Cancelar/Voltar
• F1: Iniciar jogo
• F2: Parar jogo
• F3: Reset pontuação
• F4: Ver rankings
• F5: Efeito Arco-íris
• F6: Parar efeitos visuais
• F7: Efeito Matrix Rain
• F8: Pulso Universal
• F9: Fogos de artifício
• F10: Demo completa de efeitos

🎯 JOGOS DISPONÍVEIS:
• 🎯 Pega-Luz: Reflexos rápidos
• 🧠 Sequência Maluca: Memória
• 🐱 Gato e Rato: Perseguição
• ☄️ Esquiva Meteoros: Sobrevivência
• 🎸 Guitar Hero: Ritmo
• 🎲 Roleta Russa: Sorte extrema
• ⚡ Lightning Strike: Velocidade máxima
• 🎯 Sniper Mode: Precisão impossível

🏆 PONTUAÇÃO:
• Acertos rápidos = mais pontos
• Combos multiplicam pontuação
• Níveis aumentam dificuldade
• Recordes são salvos automaticamente

🔧 PROBLEMAS COMUNS:
• Arduino não conecta: Verifique a porta
• LEDs não acendem: Reinicie a conexão
• Jogo não responde: Verifique o cabo USB
• Performance lenta: Feche outros programas

✨ EFEITOS VISUAIS:
O Arduino possui animações épicas para:
• Inicialização e conexão
• Início e fim de jogos
• Acertos perfeitos e combos
• Explosões e vitórias
• Use F5-F10 para demonstrações!

💡 DICAS:
:
• Use o Início Rápido para começar rapidamente
• Veja todas as instruções antes de jogar
• Pratique no modo Debug para testar
• Mantenha o Arduino sempre conectado durante o jogo";

        stackPanel.Children.Add(new TextBlock
        {
            Text = helpText,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = "Courier New",
            FontSize = 12,
            LineHeight = 18
        });

        var closeButton = new Button
        {
            Content = "✅ Fechar",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            MinWidth = 100,
            Margin = new Avalonia.Thickness(0, 20, 0, 0)
        };
        closeButton.Click += (s, e) => helpWindow.Close();
        stackPanel.Children.Add(closeButton);

        scrollViewer.Content = stackPanel;
        helpWindow.Content = scrollViewer;

        await helpWindow.ShowDialog(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        // Prevent multiple cleanup attempts
        lock (_closingLock)
        {
            if (_disposed) return;
            _disposed = true;
            _isClosing = true;
        }

        try
        {
            AddDebugMessage("[CLOSE] 🔥 MainWindow OnClosed chamado - iniciando cleanup");
            System.Diagnostics.Debug.WriteLine("[CLOSE] 🔥 MainWindow OnClosed chamado - iniciando cleanup");
            System.Diagnostics.Debug.WriteLine($"[CLOSE] 🔍 Usuário atual: {_currentUser?.Name ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"[CLOSE] 🔍 Modo cliente: {_isClientMode}");
            System.Diagnostics.Debug.WriteLine($"[CLOSE] 🔍 Sessão terminando: {_isSessionEnding}");
            System.Diagnostics.Debug.WriteLine($"[CLOSE] 🔍 Está fechando: {_isClosing}");

            // Stop the status timer
            _statusTimer?.Dispose();

            // Clean up LED timers
            lock (_ledTimersLock)
            {
                foreach (var timer in _ledTimers.Values)
                {
                    timer?.Dispose();
                }
                _ledTimers.Clear();
            }

            // Disconnect Arduino safely
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.WriteLine("DISCONNECT");
                    System.Threading.Thread.Sleep(200); // Reduced wait time
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Erro ao desconectar Arduino: {ex.Message}");
                }
                finally
                {
                    _serialPort = null;
                }
            }

            // End user session if exists
            if (_currentUser != null)
            {
                try
                {
                    _sessionService.EndSession(_currentUser.Id);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao finalizar sessão: {ex.Message}");
                }
            }

            // Close debug window
            try
            {
                _debugWindow?.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao fechar debug window: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro durante cleanup: {ex.Message}");
        }
        finally
        {
            AddDebugMessage("[CLOSE] ✅ MainWindow OnClosed concluído - cleanup finalizado");
            System.Diagnostics.Debug.WriteLine("[CLOSE] ✅ MainWindow OnClosed concluído - chamando base.OnClosed");
            base.OnClosed(e);
            System.Diagnostics.Debug.WriteLine("[CLOSE] 🏁 base.OnClosed executado - MainWindow totalmente fechado");
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
                AddDebugMessage("Modo tela cheia desabilitado (F11)");
            }
            else
            {
                // Enter full-screen
                WindowState = WindowState.FullScreen;
                _isFullScreen = true;
                AddDebugMessage("Modo tela cheia habilitado (F11)");
            }
        }
        catch (Exception ex)
        {
            AddDebugMessage($"Erro ao alternar tela cheia: {ex.Message}");
        }
    }



    private string GetGameDescription(int gameMode)
    {
        return _gameDescriptions.TryGetValue(gameMode, out var description)
            ? description
            : "Selecione um jogo para ver a descrição...";
    }

    private void UpdateRemainingRoundsDisplay()
    {
        if (!_isClientMode || _currentUser == null) return;

        try
        {
            var sessionStatus = _sessionService.GetClientSessionStatus(_currentUser.Id);
            StatusText.Text = sessionStatus;

            var remaining = _sessionService.GetRemainingRounds(_currentUser.Id);
            AddDebugMessage($"Status atualizado - Rodadas restantes: {remaining}");
        }
        catch (Exception ex)
        {
            AddDebugMessage($"Erro ao atualizar display de rodadas: {ex.Message}");
        }
    }

    private void RecordClientRoundLoss()
    {
        if (_isClientMode && _currentUser != null)
        {
            var selectedGame = _sessionService.GetClientSelectedGame(_currentUser.Id);
            var remainingBefore = _sessionService.GetRemainingRounds(_currentUser.Id);

            AddDebugMessage($"[DEBUG] 🔄 RecordClientRoundLoss - Antes: {remainingBefore} chances restantes para {_currentUser.Name} em {selectedGame.GetDisplayName()}");

            _sessionService.RecordGameError(_currentUser.Id);

            var remainingAfter = _sessionService.GetRemainingRounds(_currentUser.Id);
            AddDebugMessage($"[DEBUG] 🔄 RecordClientRoundLoss - Depois: {remainingAfter} chances restantes para {_currentUser.Name} em {selectedGame.GetDisplayName()}");

            AddDebugMessage($"[SESSÃO] ❌ Cliente {_currentUser.Name} cometeu erro em {selectedGame.GetDisplayName()} - Restam {remainingAfter} chances");

            UpdateRemainingRoundsDisplay();

            // Check if session should end (all chances in the selected game exhausted)
            if (_sessionService.ShouldEndClientSession(_currentUser.Id) && !_isSessionEnding)
            {
                lock (_gameStateLock)
                {
                    _isSessionEnding = true;
                }

                AddDebugMessage($"[SESSÃO] 🚫 Cliente {_currentUser.Name} esgotou todas as chances em {selectedGame.GetDisplayName()} - iniciando fim de sessão automático");

                StopGameImmediately();

                // Show session completed dialog and return to login
                lock (_sessionDialogLock)
                {
                    if (!_isShowingSessionDialog)
                    {
                        _isShowingSessionDialog = true;
                        Task.Run(async () => await ShowRoundsCompletedDialog());
                    }
                    else
                    {
                        AddDebugMessage("[SESSÃO] Diálogo de sessão já está sendo mostrado, ignorando");
                    }
                }
            }
            else
            {
                AddDebugMessage($"[SESSÃO] ✅ Sessão continua - Ainda restam {remainingAfter} chances para {_currentUser.Name}");
            }
        }
        else
        {
            AddDebugMessage("[DEBUG] ⚠️ RecordClientRoundLoss chamado mas não está em modo cliente ou usuário é null");
        }
    }

    private void StopGameImmediately()
    {
        try
        {
            _gameActive = false;

            // Clear any LED effects
            ClearLedMatrix();

            // Send stop command to Arduino
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.WriteLine("STOP_GAME");
                }
                catch (Exception arduinoEx)
                {
                    AddDebugMessage($"Erro ao enviar comando STOP_GAME: {arduinoEx.Message}");
                }
            }

            // Update UI state
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    StartGameButton.IsEnabled = false;
                    StopGameButton.IsEnabled = false;
                    StatusText.Text = "🛑 GAME OVER - Limite de erros atingido! Termine sessão";
                }
                catch (Exception uiEx)
                {
                    AddDebugMessage($"Erro ao atualizar UI: {uiEx.Message}");
                }
            });

            AddDebugMessage("Jogo parado automaticamente - limite de erros atingido");
        }
        catch (Exception ex)
        {
            AddDebugMessage($"Erro crítico ao parar jogo: {ex.Message}");
        }
    }

    private async Task ShowTimeoutCompletedDialog(int captures)
    {
        try
        {
            if (_currentUser == null)
            {
                AddDebugMessage("ShowTimeoutCompletedDialog: _currentUser é null, forçando retorno ao login");
                await ForceReturnToLogin();
                return;
            }

            AddDebugMessage($"Mostrando diálogo de timeout completo para {_currentUser.Name} - {captures} capturas");

            // Ensure we're on UI thread
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    // Show custom timeout dialog
                    var dialog = new Window()
                    {
                        Title = "⏰ Tempo Esgotado - Sessão Finalizada",
                        Width = 1200,
                        Height = 800,
                        MinWidth = 800,
                        MinHeight = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Background = new SolidColorBrush(Color.FromRgb(26, 32, 44)),
                        WindowState = WindowState.FullScreen,
                        Topmost = true,
                        CanResize = false,
                        ExtendClientAreaToDecorationsHint = true,
                        ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome
                    };

                    var mainPanel = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(100),
                        Spacing = 50,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };

                    // Title
                    mainPanel.Children.Add(new TextBlock
                    {
                        Text = "⏰ TEMPO ESGOTADO!",
                        FontSize = 72,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Foreground = Avalonia.Media.Brushes.Orange,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    });

                    // Player info
                    mainPanel.Children.Add(new TextBlock
                    {
                        Text = $"👤 Jogador: {_currentUser.Name}",
                        FontSize = 36,
                        FontWeight = Avalonia.Media.FontWeight.Medium,
                        Foreground = Avalonia.Media.Brushes.White,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    });

                    // Game result
                    mainPanel.Children.Add(new TextBlock
                    {
                        Text = $"🐱 Ratos capturados: {captures}",
                        FontSize = 32,
                        FontWeight = Avalonia.Media.FontWeight.Medium,
                        Foreground = Avalonia.Media.Brushes.LightGreen,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    });

                    // Time info
                    mainPanel.Children.Add(new TextBlock
                    {
                        Text = "⏱️ Tempo de jogo: 2 minutos",
                        FontSize = 28,
                        FontWeight = Avalonia.Media.FontWeight.Medium,
                        Foreground = Avalonia.Media.Brushes.LightGray,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center
                    });

                    // Business rule explanation
                    var ruleText = new TextBlock
                    {
                        Text = "🔒 REGRA DE NEGÓCIO:\nSua sessão foi finalizada permanentemente.\nPara jogar novamente, faça um novo login.",
                        FontSize = 26,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Foreground = Avalonia.Media.Brushes.Yellow,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextAlignment = Avalonia.Media.TextAlignment.Center,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Avalonia.Thickness(0, 40, 0, 0),
                        MaxWidth = 800
                    };
                    mainPanel.Children.Add(ruleText);

                    // Return button
                    var returnButton = new Button
                    {
                        Content = "🔙 Retornar ao Login",
                        FontSize = 32,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Padding = new Avalonia.Thickness(60, 25),
                        MinWidth = 400,
                        MinHeight = 80,
                        Background = Avalonia.Media.Brushes.DarkRed,
                        Foreground = Avalonia.Media.Brushes.White,
                        CornerRadius = new Avalonia.CornerRadius(15),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 50, 0, 0)
                    };

                    returnButton.Click += async (s, e) =>
                    {
                        try
                        {
                            AddDebugMessage("[TIMEOUT] Botão de retorno clicado - forçando logout");
                            dialog.Close();
                            await ForceReturnToLogin();
                        }
                        catch (Exception ex)
                        {
                            AddDebugMessage($"Erro ao retornar do timeout: {ex.Message}");
                            await ForceReturnToLogin();
                        }
                    };

                    mainPanel.Children.Add(returnButton);
                    dialog.Content = mainPanel;

                    // Auto-return after 15 seconds
                    var autoReturnTimer = new System.Threading.Timer(
                        callback: _ => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            try
                            {
                                AddDebugMessage("[TIMEOUT] Timer automático ativado - forçando logout");
                                dialog.Close();
                                await ForceReturnToLogin();
                            }
                            catch (Exception ex)
                            {
                                AddDebugMessage($"Erro no timer automático: {ex.Message}");
                                await ForceReturnToLogin();
                            }
                        }),
                        state: null,
                        dueTime: TimeSpan.FromSeconds(15),
                        period: System.Threading.Timeout.InfiniteTimeSpan
                    );

                    dialog.Closed += (s, e) => autoReturnTimer?.Dispose();

                    await dialog.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Erro ao criar diálogo de timeout: {ex.Message}");
                    await ForceReturnToLogin();
                }
            });
        }
        catch (Exception ex)
        {
            AddDebugMessage($"Erro crítico em ShowTimeoutCompletedDialog: {ex.Message}");
            await ForceReturnToLogin();
        }
    }

    private async Task ShowRoundsCompletedDialog()
    {
        try
        {
            AddDebugMessage("[SESSÃO] ShowRoundsCompletedDialog iniciado");

            // Double-check dialog is not already showing
            lock (_sessionDialogLock)
            {
                if (_isShowingSessionDialog && _disposed)
                {
                    AddDebugMessage("[SESSÃO] Window já foi descartado, cancelando diálogo");
                    return;
                }
            }

            if (_currentUser == null)
            {
                AddDebugMessage("ShowRoundsCompletedDialog: _currentUser é null, forçando retorno ao login");
                lock (_sessionDialogLock) { _isShowingSessionDialog = false; }
                await ForceReturnToLogin();
                return;
            }

            var session = _sessionService.GetSession(_currentUser.Id);
            if (session == null)
            {
                AddDebugMessage("ShowRoundsCompletedDialog: sessão não encontrada, forçando retorno ao login");
                lock (_sessionDialogLock) { _isShowingSessionDialog = false; }
                await ForceReturnToLogin();
                return;
            }

            AddDebugMessage($"Mostrando diálogo de sessão completa para {_currentUser.Name}");

            // Ensure we're on UI thread
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    var roundsCompletedWindow = new RoundsCompletedWindow();
                    var gameSummary = new Dictionary<GameMode, int>();
                    gameSummary[session.SelectedGame] = session.ErrorsCommitted;
                    roundsCompletedWindow.SetGameSummary(gameSummary, _currentUser.Name);

                    // Handle return to login event - this MUST happen
                    roundsCompletedWindow.OnReturnToLogin += async (sender, e) =>
                    {
                        try
                        {
                            // Prevent multiple calls
                            lock (_closingLock)
                            {
                                if (_isClosing || _disposed)
                                {
                                    AddDebugMessage("[SESSÃO] OnReturnToLogin chamado mas window já está fechando");
                                    return;
                                }
                                _isClosing = true;
                            }

                            // Reset session dialog flag
                            lock (_sessionDialogLock)
                            {
                                _isShowingSessionDialog = false;
                            }

                            AddDebugMessage("[SESSÃO] Evento OnReturnToLogin disparado - processando retorno ao login");

                            // End the session
                            if (_currentUser != null)
                                _sessionService.EndSession(_currentUser.Id);

                            // Small delay to ensure session is properly ended
                            await Task.Delay(100);

                            // Force close this window first
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                AddDebugMessage("[SESSÃO] 🚪 Fechando MainWindow via RoundsCompletedDialog");
                                System.Diagnostics.Debug.WriteLine("[SESSÃO] 🚪 Fechando MainWindow via RoundsCompletedDialog");
                                
                                try
                                {
                                    Close();
                                    AddDebugMessage("[SESSÃO] ✅ Retorno ao login concluído com sucesso - App.axaml.cs irá mostrar login");
                                }
                                catch (Exception closeEx)
                                {
                                    AddDebugMessage($"[SESSÃO] Erro ao fechar window: {closeEx.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            AddDebugMessage($"Erro no evento OnReturnToLogin: {ex.Message}");
                            // Reset dialog flag on error
                            lock (_sessionDialogLock) { _isShowingSessionDialog = false; }
                            // If anything fails, still try to close and show login
                            await ForceReturnToLogin();
                        }
                    };


                    // Show as modal dialog to block all interaction
                    try
                    {
                        await roundsCompletedWindow.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        AddDebugMessage($"Erro ao mostrar diálogo modal: {ex.Message}");
                        // If ShowDialog fails, show normally and force modal behavior
                        roundsCompletedWindow.Show();

                        // Auto-trigger return to login after a delay as fallback
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(8000); // 8 seconds fallback
                            await Dispatcher.UIThread.InvokeAsync(async () => await ForceReturnToLogin());
                        });
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Erro ao criar RoundsCompletedWindow: {ex.Message}");
                    lock (_sessionDialogLock) { _isShowingSessionDialog = false; }
                    await ForceReturnToLogin();
                }
            });
        }
        catch (Exception ex)
        {
            AddDebugMessage($"Erro crítico em ShowRoundsCompletedDialog: {ex.Message}");
            lock (_sessionDialogLock) { _isShowingSessionDialog = false; }
            await ForceReturnToLogin();
        }
    }

    private async Task ForceReturnToLogin()
    {
        try
        {
            // Prevent multiple calls
            lock (_closingLock)
            {
                if (_isClosing || _disposed)
                {
                    AddDebugMessage("[LOGOUT] ForceReturnToLogin chamado mas window já está fechando");
                    return;
                }
                _isClosing = true;
            }

            AddDebugMessage("[LOGOUT] ⚠️ ForceReturnToLogin: iniciando retorno forçado ao login");
            System.Diagnostics.Debug.WriteLine("[LOGOUT] ⚠️ ForceReturnToLogin chamado");

            // End session if user exists
            if (_currentUser != null)
            {
                try
                {
                    _sessionService.EndSession(_currentUser.Id);
                    AddDebugMessage($"[LOGOUT] 📝 Sessão do usuário {_currentUser.Name} encerrada");
                }
                catch (Exception sessionEx)
                {
                    AddDebugMessage($"[LOGOUT] Erro ao encerrar sessão: {sessionEx.Message}");
                }
            }

            // Small delay to ensure session cleanup
            await Task.Delay(150);

            // Just close the window - App.axaml.cs will handle showing login
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddDebugMessage("[LOGOUT] 🚪 Fechando MainWindow via ForceReturnToLogin");
                System.Diagnostics.Debug.WriteLine("[LOGOUT] 🚪 Fechando MainWindow via ForceReturnToLogin");
                
                try
                {
                    Close();
                    AddDebugMessage("[LOGOUT] ✅ MainWindow fechado via ForceReturnToLogin");
                }
                catch (Exception closeEx)
                {
                    AddDebugMessage($"[LOGOUT] Erro ao fechar window: {closeEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[LOGOUT] Erro ao fechar window: {closeEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            AddDebugMessage($"[LOGOUT] ❌ ForceReturnToLogin: erro crítico: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LOGOUT] ❌ ForceReturnToLogin: erro crítico: {ex.Message}");
            
            // Last resort fallback - just close window
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => Close());
            }
            catch (Exception fallbackEx)
            {
                AddDebugMessage($"[LOGOUT] 💥 Falha crítica total: {fallbackEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[LOGOUT] 💥 Falha crítica total: {fallbackEx.Message}");
            }
        }
    }

    private async Task ShowMessage(string title, string message)
    {
        var messageWindow = new Window
        {
            Title = title,
            Width = 600,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };
        stackPanel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

        var button = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        button.Click += (s, e) => messageWindow.Close();
        stackPanel.Children.Add(button);

        messageWindow.Content = stackPanel;
        await messageWindow.ShowDialog(this);
    }
}
