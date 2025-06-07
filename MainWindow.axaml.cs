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
    private AudioService _audioService;
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
        { 3, "🐱 Capture o rato 14 vezes em apenas 120 segundos! Ele acelera e pisca cada vez mais rápido!" },
        { 4, "☄️ Desvie dos meteoros (LEDs vermelhos) que caem! Use as setas para mover." },
        { 5, "🎸 Pressione os LEDs no ritmo da música! Timing perfeito = pontos extras." },
        { 6, "⚡ Lightning Strike! Memorize padrões ultra-rápidos que aparecem por milissegundos!" },
        { 7, "🎯 Sniper Mode! Atire nos alvos que piscam por apenas 0.1 segundo - precisão extrema!" }
    };

    // Game instructions for the new games
    private readonly Dictionary<int, string> _gameInstructions = new()
    {
        { 1, "PEGA-LUZ:\n• Pressione W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L quando o LED acender\n• Seja rápido! LEDs apagam sozinhos\n• +10 pontos por acerto\n• +5 pontos por velocidade" },
        { 2, "SEQUÊNCIA MALUCA:\n• Observe a sequência de LEDs\n• Repita pressionando W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L\n• Cada nível adiciona +1 LED\n• Erro = Game Over" },
        { 3, "GATO E RATO:\n• Use setas para mover o gato\n• Capture o rato 14 VEZES em apenas 120 segundos!\n• Rato acelera drasticamente a cada captura\n• Pisca ultra-rápido após 8 capturas" },
        { 4, "ESQUIVA METEOROS:\n• Use ↑↓←→ para desviar\n• Meteoros caem aleatoriamente\n• Sobreviva o máximo possível\n• +1 ponto por segundo" },
        { 5, "GUITAR HERO:\n• Pressione W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L no ritmo\n• Siga as batidas musicais\n• Combo = pontos multiplicados\n• Precisão é fundamental" },
        { 6, "LIGHTNING STRIKE:\n• Padrão pisca por milissegundos\n• Memorize e reproduza rapidamente\n• Tempo de exibição diminui por nível\n• Erro = Game Over instantâneo" },
        { 7, "SNIPER MODE:\n• Alvos piscam por apenas 0.1 segundo\n• Pressione a tecla exata no tempo\n• 10 acertos = vitória impossível\n• Bônus x10 se completar!" }
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
        _audioService = new AudioService();        // Play startup sound
        _audioService.PlaySound(AudioEvent.Startup);

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

        // Game configured silently for performance

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
            // Client session configured silently for performance
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
        _audioService.PlaySound(AudioEvent.ButtonClick);
        // Prevent multiple logout attempts
        lock (_closingLock)
        {
            if (_isClosing || _disposed)
            {
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
                    return;
                }
                _isClosing = true;
            }

            // Logout processed silently for performance

            // End client session if in client mode
            if (_isClientMode && _currentUser != null)
            {
                _sessionService.EndSession(_currentUser.Id);
            }

            // Disconnect Arduino
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.Close();
                    _serialPort = null;
                }
                catch (Exception)
                {
                    // Silent error handling
                }
            }

            // Return to login without closing application
            await ReturnToLoginSafely();
        }
        else
        {
            // Logout cancelled silently
        }
    }

    private async Task ReturnToLoginSafely()
    {
        try
        {
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
                try
                {
                    Close();
                }
                catch (Exception)
                {
                    // Silent error handling
                }
            });
        }
        catch (Exception)
        {
            // Fallback: still try to close the window
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() => Close());
            }
            catch (Exception)
            {
                // Silent error handling
            }
        }
    }

    private async void GenerateCodesButton_Click(object? sender, RoutedEventArgs e)
    {
        _audioService.PlaySound(AudioEvent.ButtonClick);
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
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("Erro", $"Erro ao gerar códigos: {ex.Message}");
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
            6 => "⚡ Lightning Strike",
            7 => "🎯 Sniper Mode",
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

                            // Confirm game mode is locked for clients after successful connection
                            if (_isClientMode)
                            {
                                GameModeComboBox.IsEnabled = false;
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
        _audioService.PlaySound(AudioEvent.ButtonClick);
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
            // Arduino disconnected silently
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
        if (message.StartsWith("GAME_EVENT:"))
        {
            var eventData = message.Substring("GAME_EVENT:".Length);
            ProcessGameEvent(eventData);
        }
        else if (message == "READY")
        {
            StatusText.Text = "🟢 Arduino pronto! Conexão estabelecida com sucesso! Inicie o jogo.";
            TriggerVisualEffect("CONNECTION_SUCCESS");
        }
        else if (message == "ALL_LEDS_OFF")
        {
            ClearLedMatrix();
        }
        else
        {
            ProcessGameEvent(message);
        }
    }

    private void AddDebugMessage(string message, bool isDebug = false)
    {
        // Only add critical messages to improve performance
        if (!isDebug && (_debugWindow != null || message.Contains("ERRO") || message.Contains("GAME_OVER")))
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var formattedMessage = $"[{timestamp}] {message}";
            _debugWindow?.AddMessage(formattedMessage, isDebug);
        }
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
                    CheckVictoryConditions();
                }
                break;

            case "LEVEL":
                if (int.TryParse(eventValue, out var newLevel))
                {
                    _level = newLevel;
                    UpdateUI();
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
                        return; // Silent return for better performance
                    }
                }

                if (!shouldProcessGameOver) break;

                // Execute game over logic
                _gameActive = false;
                StartGameButton.IsEnabled = true;
                StopGameButton.IsEnabled = false;

                // Stop dynamic game music
                _ = Task.Run(async () => await _audioService.StopGameMusicAsync());

                if (int.TryParse(eventValue, out var finalScore))
                {
                    _score = finalScore;
                }

                _audioService.PlaySound(AudioEvent.GameOver);
                SaveGameScore();
                TriggerVisualEffect("GAME_OVER");

                // For clients, check if session should end after game over
                if (_isClientMode && _currentUser != null)
                {
                    var remainingRounds = _sessionService.GetRemainingRounds(_currentUser.Id);

                    if (remainingRounds <= 0)
                    {
                        lock (_gameStateLock)
                        {
                            _isSessionEnding = true;
                        }

                        // Prevent multiple session dialogs
                        lock (_sessionDialogLock)
                        {
                            if (!_isShowingSessionDialog)
                            {
                                _isShowingSessionDialog = true;
                                Task.Run(async () => await ShowRoundsCompletedDialog());
                            }
                        }
                    }
                    else
                    {
                        // Still has chances in the selected game
                        var selectedGameName = _sessionService.GetClientSelectedGame(_currentUser.Id).GetDisplayName();
                        StatusText.Text = $"🎮 GAME OVER! Ainda tem {remainingRounds} chance(s) em {selectedGameName}";
                    }
                }
                break;

            case "STATUS":
                StatusText.Text = eventValue;
                break;

            case "HIT":
                _audioService.PlaySound(AudioEvent.ScoreHit);
                var hitData = eventValue.Split(',');
                if (hitData.Length >= 2 && int.TryParse(hitData[0], out var ledHit) && int.TryParse(hitData[1], out var hitTotalScore))
                {
                    var pointsEarned = hitTotalScore - _score;
                    _score = hitTotalScore; // Sync with Arduino score
                    HighlightLed(ledHit);
                    StatusText.Text = $"🎯 Acertou LED {ledHit}! +{pointsEarned} pontos (Total: {_score})";
                    UpdateUI();

                    // Check victory conditions after score update
                    CheckVictoryConditions();
                }
                break;

            case "MISS":
                _audioService.PlaySound(AudioEvent.Error);
                StatusText.Text = "❌ Muito lento! O LED apagou sozinho.";
                RecordClientRoundLoss();
                break;

            case "LED_ON":
                if (int.TryParse(eventValue, out var ledOnIndex))
                {
                    HighlightLed(ledOnIndex);
                }
                break;

            case "LED_OFF":
                if (int.TryParse(eventValue, out var ledOffIndex))
                {
                    RestoreLedColor(ledOffIndex);
                }
                break;

            case "WRONG_KEY":
                if (int.TryParse(eventValue, out var wrongKey))
                {
                    StatusText.Text = $"❌ Tecla errada! Pressionou {wrongKey}, mas deveria ser outro LED.";
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
                    _audioService.PlaySound(AudioEvent.LevelUp);
                    UpdateUI();
                    TriggerVisualEffect("LEVEL_UP");

                    // Check victory conditions after score update
                    CheckVictoryConditions();
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
                    UpdateUI();
                    TriggerVisualEffect("GAME_START");
                }
                break;

            case "KEY_RELEASED":
                // Key release handled silently for performance
                break;

            case "METEOR_HIT":
                _audioService.PlaySound(AudioEvent.MeteoroExplosion);
                if (int.TryParse(eventValue, out var meteorPos))
                {
                    StatusText.Text = "💥 IMPACTO! Um meteoro te atingiu! Game Over!";
                    HighlightLed(meteorPos);
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
                    _audioService.PlaySound(AudioEvent.GuitarNote);
                    UpdateUI();

                    // Check victory conditions after score update
                    CheckVictoryConditions();
                }
                break;

            case "NOTE_MISS":
                _audioService.PlaySound(AudioEvent.Error);
                StatusText.Text = "🎵 Nota perdida! Muito cedo ou muito tarde. Siga o ritmo!";
                RecordClientRoundLoss();
                break;

            case "SEQUENCE_START":
                _audioService.PlaySound(AudioEvent.SequenciaShow);
                StatusText.Text = "👀 ATENÇÃO! Memorize a sequência de LEDs que vai piscar...";
                break;

            case "SEQUENCE_REPEAT":
                _audioService.PlaySound(AudioEvent.ButtonHover);
                StatusText.Text = "🔄 Sua vez! Repita a sequência na mesma ordem.";
                break;

            case "PLAYER_MOVE":
                _audioService.PlaySound(AudioEvent.PlayerMove);
                if (int.TryParse(eventValue, out var playerNewPos))
                {
                    StatusText.Text = $"🏃 Moveu para posição {playerNewPos}. Continue desviando!";
                    HighlightLed(playerNewPos);
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



            // Lightning Strike Events
            case "LIGHTNING_PATTERN_SHOW":
                _audioService.PlaySound(AudioEvent.LightningFlash);
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
                _audioService.PlaySound(AudioEvent.LightningCorrect);
                StatusText.Text = "⚡ PERFEITO! Reflexos incríveis! Próximo nível será mais difícil...";
                break;

            case "LIGHTNING_WRONG":
                _audioService.PlaySound(AudioEvent.LightningWrong);
                StatusText.Text = "❌ Errou! O padrão correto está sendo mostrado agora. Game Over!";
                RecordClientRoundLoss();
                break;

            // Sniper Mode Events
            case "SNIPER_TARGET_SPAWN":
                _audioService.PlaySound(AudioEvent.SniperShot);
                StatusText.Text = "🎯 ALVO À VISTA! Você tem 0.1 segundo para atirar!";
                break;

            case "SNIPER_HIT":
                _audioService.PlaySound(AudioEvent.SniperHit);
                var sniperData = eventValue.Split(',');
                if (sniperData.Length >= 2)
                {
                    StatusText.Text = $"🎯 TIRO CERTEIRO! Acertos: {sniperData[0]}/10 | Tempo: {sniperData[1]}ms";
                }
                break;

            case "SNIPER_MISS":
                _audioService.PlaySound(AudioEvent.SniperMiss);
                StatusText.Text = "❌ Tiro errado! Mirou no lugar errado ou muito devagar.";
                RecordClientRoundLoss();
                break;

            case "SNIPER_TIMEOUT":
                _audioService.PlaySound(AudioEvent.Error);
                StatusText.Text = "⏰ MUITO LENTO! O alvo desapareceu antes de você atirar.";
                RecordClientRoundLoss();
                break;

            case "PEGA_LUZ_VICTORY":
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 VITÓRIA PEGA-LUZ! 400 pontos com reflexos ultra-rápidos!";
                TriggerVisualEffect("VICTORY");
                break;

            case "SEQUENCIA_MALUCA_VICTORY":
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 VITÓRIA SEQUÊNCIA MALUCA! 8 rodadas perfeitas chegando a 10 passos!";
                TriggerVisualEffect("VICTORY");
                break;

            case "GUITAR_HERO_VICTORY":
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 VITÓRIA GUITAR HERO! 300 pontos com ritmo perfeito!";
                TriggerVisualEffect("VICTORY");
                break;

            case "ESQUIVA_METEOROS_VICTORY":
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 VITÓRIA METEOROS! Sobreviveu 3 minutos completos!";
                TriggerVisualEffect("VICTORY");
                break;

            case "LIGHTNING_STRIKE_VICTORY":
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 VITÓRIA LIGHTNING STRIKE! 20 sequências perfeitas!";
                TriggerVisualEffect("VICTORY");
                break;

            case "SNIPER_VICTORY":
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 LEGENDÁRIO! 10/10 acertos! Você é um sniper de elite!";
                TriggerVisualEffect("VICTORY");
                break;

            case "GATO_RATO_TIMEOUT":
                _audioService.PlaySound(AudioEvent.Error);
                if (int.TryParse(eventValue, out var captures))
                {
                    StatusText.Text = $"⏰ TEMPO ESGOTADO! Você capturou {captures} ratos em 2 minutos. Sessão finalizada!";

                    // Business rule: timeout ends the session permanently for this client
                    if (_isClientMode && _currentUser != null)
                    {
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
                _audioService.PlaySound(AudioEvent.Victory);
                StatusText.Text = "🏆 VITÓRIA! Você capturou todos os ratos necessários!";
                break;

            case "GATO_CAPTURE":
                _audioService.PlaySound(AudioEvent.GatoCapture);
                StatusText.Text = "🐱 CAPTURA! O gato pegou o rato! +20 pontos!";
                break;

            case "GATO_MOVE":
                _audioService.PlaySound(AudioEvent.GatoMove);
                break;

            case "RATO_MOVE":
                _audioService.PlaySound(AudioEvent.RatoMove);
                break;

            case "SEQUENCIA_CORRECT":
                _audioService.PlaySound(AudioEvent.SequenciaCorrect);
                StatusText.Text = "✅ CORRETO! Sequência reproduzida perfeitamente!";
                break;

            case "SEQUENCIA_WRONG":
                _audioService.PlaySound(AudioEvent.SequenciaWrong);
                StatusText.Text = "❌ ERRO! Sequência incorreta. Game Over!";
                RecordClientRoundLoss();
                break;

            case "METEORO_SPAWN":
                _audioService.PlaySound(AudioEvent.MeteoroSpawn);
                StatusText.Text = "☄️ METEORO À VISTA! Desvie rapidamente!";
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

                    // Check victory conditions after score update
                    CheckVictoryConditions();
                }
                break;

            case "PENALTY":
                if (int.TryParse(eventValue, out var penalty))
                {
                    _score = Math.Max(0, _score - penalty);
                    StatusText.Text = $"⚠️ Penalidade! -{penalty} pontos";
                    UpdateUI();

                    // Check victory conditions after score update (though unlikely after penalty)
                    CheckVictoryConditions();
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
                _audioService.PlaySound(AudioEvent.Victory);
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
                // Unknown events handled silently for performance
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
            new { Id = 6, Name = "⚡ Lightning Strike", Description = "Velocidade Extrema" },
            new { Id = 7, Name = "🎯 Sniper Mode", Description = "Precisão Máxima" }
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
            var gameNames = new[] { "", "Pega-Luz", "Sequência Maluca", "Gato e Rato", "Esquiva Meteoros", "Guitar Hero", "Lightning Strike", "Sniper Mode" };
            if (_currentGameMode < gameNames.Length)
            {
                CurrentGameText.Text = gameNames[_currentGameMode];
            }
        }
    }

    private bool CheckVictoryConditions()
    {
        if (!_gameActive) return false;

        var gameMode = (GameMode)_currentGameMode;
        bool victoryAchieved = false;
        string challengeDescription = "";

        switch (gameMode)
        {
            case GameMode.PegaLuz:
                if (_score >= 400)
                {
                    victoryAchieved = true;
                    challengeDescription = "Alcançou 400 pontos com reflexos ultra-rápidos";
                }
                break;
            case GameMode.SequenciaMaluca:
                // Updated: 8 rounds without errors (sequence reaches 10 steps)
                if (_score >= 80) // 8 rounds * 10 points per round
                {
                    victoryAchieved = true;
                    challengeDescription = "Completou 8 rodadas sem errar (sequência chegou a 10 passos)";
                }
                break;
            case GameMode.GatoRato:
                // DIFFICULTY INCREASED: Much harder challenge - 16 captures in 120 seconds
                if (_score >= 320) // 16 captures * 20 points per capture
                {
                    victoryAchieved = true;
                    challengeDescription = "Capturou o rato 16 vezes em apenas 120 segundos!";
                }
                break;

            case GameMode.EsquivaMeteoros:
                if (_score >= 180)
                {
                    victoryAchieved = true;
                    challengeDescription = "Sobreviveu por 180 segundos (3 minutos) sem ser atingido";
                }
                break;

            case GameMode.GuitarHero:
                if (_score >= 300)
                {
                    victoryAchieved = true;
                    challengeDescription = "Fez 300 pontos com ritmo perfeito";
                }
                break;
            case GameMode.LightningStrike:
                // Updated: 20 successful sequences for 200 points
                if (_score >= 200) // 20 rounds * 10 points per round
                {
                    victoryAchieved = true;
                    challengeDescription = "Completou 20 sequências de Lightning Strike sem errar";
                }
                break;
            case GameMode.SniperMode:
                // Updated: 10 hits for 100 points (more challenging)
                if (_score >= 100) // 10 hits * 10 points per hit
                {
                    victoryAchieved = true;
                    challengeDescription = "Acertou 10 alvos com reflexos ultra-rápidos (300ms por alvo)";
                }
                break;
        }

        if (victoryAchieved)
        {
            TriggerVictory(challengeDescription);
            return true;
        }

        return false;
    }

    private void TriggerVictory(string challengeDescription)
    {
        try
        {
            AddDebugMessage($"[VITÓRIA] Desafio conquistado: {challengeDescription}");

            // Stop the game
            _gameActive = false;
            StartGameButton.IsEnabled = true;
            StopGameButton.IsEnabled = false;

            // Stop dynamic game music
            _ = Task.Run(async () => await _audioService.StopGameMusicAsync());

            // Play victory sound
            _audioService.PlaySound(AudioEvent.Victory);

            // Save the score
            SaveGameScore();

            // Show victory window
            var victoryWindow = new Views.VictoryWindow();
            victoryWindow.SetVictoryDetails((GameMode)_currentGameMode, _score, _playerName, challengeDescription);

            // Handle return to login
            victoryWindow.OnReturnToLogin += async (sender, e) =>
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ReturnToLoginSafely();
                });
            };

            victoryWindow.Show();
            victoryWindow.Activate();
            victoryWindow.Focus();

            AddDebugMessage("[VITÓRIA] Janela de vitória exibida");
        }
        catch (Exception ex)
        {
            AddDebugMessage($"[ERRO] Erro ao exibir vitória: {ex.Message}");
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
        // Convert LED index to key code (W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L)
        return ledIndex switch
        {
            0 => (int)Key.W,   // Row 0, Col 0
            1 => (int)Key.E,   // Row 0, Col 1
            2 => (int)Key.R,   // Row 0, Col 2
            3 => (int)Key.T,   // Row 0, Col 3
            4 => (int)Key.S,   // Row 1, Col 0
            5 => (int)Key.D,   // Row 1, Col 1
            6 => (int)Key.F,   // Row 1, Col 2
            7 => (int)Key.G,   // Row 1, Col 3
            8 => (int)Key.Y,   // Row 2, Col 0
            9 => (int)Key.U,   // Row 2, Col 1
            10 => (int)Key.I,  // Row 2, Col 2
            11 => (int)Key.O,  // Row 2, Col 3
            12 => (int)Key.H,  // Row 3, Col 0
            13 => (int)Key.J,  // Row 3, Col 1
            14 => (int)Key.K,  // Row 3, Col 2
            15 => (int)Key.L,  // Row 3, Col 3
            _ => -1
        };
    }

    private int GetLedForKey(Key key)
    {
        return key switch
        {
            Key.W => 0,   // Row 0, Col 0
            Key.E => 1,   // Row 0, Col 1
            Key.R => 2,   // Row 0, Col 2
            Key.T => 3,   // Row 0, Col 3
            Key.S => 4,   // Row 1, Col 0
            Key.D => 5,   // Row 1, Col 1
            Key.F => 6,   // Row 1, Col 2
            Key.G => 7,   // Row 1, Col 3
            Key.Y => 8,   // Row 2, Col 0
            Key.U => 9,   // Row 2, Col 1
            Key.I => 10,  // Row 2, Col 2
            Key.O => 11,  // Row 2, Col 3
            Key.H => 12,  // Row 3, Col 0
            Key.J => 13,  // Row 3, Col 1
            Key.K => 14,  // Row 3, Col 2
            Key.L => 15,  // Row 3, Col 3
            _ => -1
        };

    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        // Handle function keys
        switch (e.Key)
        {
            case Key.Space:
                _audioService.PlaySound(AudioEvent.FunctionKey);
                if (StartGameButton.IsEnabled) StartGameButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F2:
                _audioService.PlaySound(AudioEvent.FunctionKey);
                if (StopGameButton.IsEnabled) StopGameButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F3:
                _audioService.PlaySound(AudioEvent.FunctionKey);
                ResetScoreButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F4:
                _audioService.PlaySound(AudioEvent.FunctionKey);
                ViewScoresButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F5:
                // Atualizar portas
                _audioService.PlaySound(AudioEvent.FunctionKey);
                RefreshPortsButton_Click(null, new RoutedEventArgs());
                e.Handled = true;
                return;
            case Key.F6:
                // Parar todos os efeitos
                _audioService.PlaySound(AudioEvent.FunctionKey);
                TriggerVisualEffect("STOP_EFFECTS");
                StatusText.Text = "⏹️ Efeitos visuais interrompidos.";
                e.Handled = true;
                return;
            case Key.F7:
                // Efeito Matrix
                _audioService.PlaySound(AudioEvent.MatrixSound);
                TriggerVisualEffect("MATRIX");
                StatusText.Text = "💚 Matrix Rain ativado! (F6 para parar)";
                e.Handled = true;
                return;
            case Key.F8:
                // Efeito Pulse
                _audioService.PlaySound(AudioEvent.PulseSound);
                TriggerVisualEffect("PULSE");
                StatusText.Text = "💓 Pulso Universal ativado! (F6 para parar)";
                e.Handled = true;
                return;
            case Key.F9:
                // Fogos de artifício
                _audioService.PlaySound(AudioEvent.Fireworks);
                TriggerVisualEffect("FIREWORKS");
                StatusText.Text = "🎆 Fogos de artifício! Efeito único de 2 segundos.";
                e.Handled = true;
                return;
            case Key.F10:
                // Demo completa dos efeitos
                _audioService.PlaySound(AudioEvent.FunctionKey);
                if (_serialPort?.IsOpen == true)
                {
                    VisualEffectsDemo_Click(null, new RoutedEventArgs());
                }
                e.Handled = true;
                return;
            case Key.F11:
                // Toggle full-screen
                _audioService.PlaySound(AudioEvent.FunctionKey);
                ToggleFullScreen();
                e.Handled = true;
                return;
        }

        if (!_gameActive || _serialPort?.IsOpen != true) return;

        string command = "";

        // Handle LED keys (W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L)
        var ledIndex = GetLedForKey(e.Key);
        if (ledIndex >= 0)
        {
            _audioService.PlaySound(AudioEvent.KeyPress);
            command = $"KEY_PRESS:{ledIndex}";
            HighlightLed(ledIndex);
        }
        // Handle arrow keys
        else if (e.Key == Key.Up)
        {
            _audioService.PlaySound(AudioEvent.ArrowKey);
            command = "MOVE:UP";
        }
        else if (e.Key == Key.Down)
        {
            _audioService.PlaySound(AudioEvent.ArrowKey);
            command = "MOVE:DOWN";
        }
        else if (e.Key == Key.Left)
        {
            _audioService.PlaySound(AudioEvent.ArrowKey);
            command = "MOVE:LEFT";
        }
        else if (e.Key == Key.Right)
        {
            _audioService.PlaySound(AudioEvent.ArrowKey);
            command = "MOVE:RIGHT";
        }
        else if (e.Key == Key.Enter)
        {
            _audioService.PlaySound(AudioEvent.GameControl);
            command = "ACTION:CONFIRM";
        }
        else if (e.Key == Key.Escape)
        {
            _audioService.PlaySound(AudioEvent.GameControl);
            command = "ACTION:CANCEL";
        }

        if (!string.IsNullOrEmpty(command))
        {
            _serialPort.WriteLine(command);
            e.Handled = true;
        }
    }

    private void Window_KeyUp(object? sender, KeyEventArgs e)
    {
        if (!_gameActive || _serialPort?.IsOpen != true) return;

        // Handle LED keys (W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L) - restore original color when key is released
        var ledIndex = GetLedForKey(e.Key);
        if (ledIndex >= 0)
        {
            _audioService.PlaySound(AudioEvent.KeyRelease);
            RestoreLedColor(ledIndex);

            // Send key release command to Arduino
            string command = $"KEY_RELEASE:{ledIndex}";
            _serialPort.WriteLine(command);
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
        _audioService.PlaySound(AudioEvent.ButtonClick);
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
        _audioService.PlaySound(AudioEvent.ButtonClick);
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
        _audioService.PlaySound(AudioEvent.GameStart);
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

            // Check if session is blocked due to timeout (business rule)
            bool isSessionEnding;
            lock (_gameStateLock)
            {
                isSessionEnding = _isSessionEnding;
            }

            if (isSessionEnding)
            {
                await ShowMessage("Sessão Finalizada",
                    "Sua sessão foi finalizada permanentemente devido ao timeout no jogo Gato e Rato.\n" +
                    "Para jogar novamente, faça logout e entre com uma nova sessão.");
                return;
            }

            if (!_sessionService.CanClientPlayGame(_currentUser.Id))
            {
                var remaining = _sessionService.GetRemainingRounds(_currentUser.Id);
                await ShowMessage("Limite de Erros Atingido",
                    $"Você já cometeu o máximo de erros permitidos em {gameMode.GetDisplayName()}!\n" +
                    $"Erros restantes: {remaining}");
                return;
            }
        }

        _gameActive = true;
        _score = 0;
        _level = 1;
        _gameStartTime = DateTime.Now;

        StartGameButton.IsEnabled = false;
        StopGameButton.IsEnabled = true;

        var command = $"START_GAME:{_currentGameMode}";
        _serialPort.WriteLine(command);

        // Start dynamic game music
        _ = Task.Run(async () => await _audioService.StartGameMusicForModeAsync(_currentGameMode));

        StatusText.Text = "🚀 Jogo iniciado! Boa sorte!";
        UpdateUI();
    }

    private void StopGameButton_Click(object? sender, RoutedEventArgs e)
    {
        _audioService.PlaySound(AudioEvent.ButtonClick);
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.WriteLine("STOP_GAME");
        }

        _gameActive = false;
        StartGameButton.IsEnabled = true;
        StopGameButton.IsEnabled = false;

        // Stop dynamic game music
        _ = Task.Run(async () => await _audioService.StopGameMusicAsync());

        StatusText.Text = "⏹️ Jogo interrompido pelo jogador.";

        if (_score > 0)
        {
            SaveGameScore();
        }

        ClearLedMatrix();
    }

    private void ResetScoreButton_Click(object? sender, RoutedEventArgs e)
    {
        _audioService.PlaySound(AudioEvent.ButtonClick);
        _score = 0;
        _level = 1;
        UpdateUI();
        AddDebugMessage("Pontuação resetada");
        StatusText.Text = "🔄 Pontuação resetada!";
    }

    private async void RefreshPortsButton_Click(object? sender, RoutedEventArgs e)
    {
        _audioService.PlaySound(AudioEvent.ButtonClick);
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
        _audioService.PlaySound(AudioEvent.ButtonClick);
        var scoresWindow = new Views.ScoresWindow(_scoreService);
        await scoresWindow.ShowDialog(this);
    }

    private void SaveGameScore()
    {
        if (!string.IsNullOrWhiteSpace(_playerName) && _score > 0)
        {
            var gameNames = new[] { "", "Pega-Luz", "Sequência Maluca", "Gato e Rato", "Esquiva Meteoros", "Guitar Hero", "Lightning Strike", "Sniper Mode" };
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
        _audioService.PlaySound(AudioEvent.GameStart);
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
        _audioService.PlaySound(AudioEvent.ButtonClick);
        var instructionsWindow = new InstructionsWindow(_gameInstructions);
        await instructionsWindow.ShowDialog(this);
    }

    private async void VisualEffectsDemo_Click(object? sender, RoutedEventArgs e)
    {
        _audioService.PlaySound(AudioEvent.DemoMusic);
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
3. Clique em 'Iniciar' ou use Espaço

⌨️ CONTROLES:
• W,E,R,T / S,D,F,G / Y,U,I,O / H,J,K,L: Pressionar LEDs específicos
• Setas: Mover cursor/personagem
• Enter: Confirmar ação
• Esc: Cancelar/Voltar
• Espaço: Iniciar jogo
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
            _sessionService.RecordGameError(_currentUser.Id);
            var remainingAfter = _sessionService.GetRemainingRounds(_currentUser.Id);

            UpdateRemainingRoundsDisplay();

            // Check if session should end (all chances in the selected game exhausted)
            if (_sessionService.ShouldEndClientSession(_currentUser.Id) && !_isSessionEnding)
            {
                lock (_gameStateLock)
                {
                    _isSessionEnding = true;
                }

                StopGameImmediately();

                // Show session completed dialog and return to login
                lock (_sessionDialogLock)
                {
                    if (!_isShowingSessionDialog)
                    {
                        _isShowingSessionDialog = true;
                        Task.Run(async () => await ShowRoundsCompletedDialog());
                    }
                }
            }
        }
    }

    private void StopGameImmediately()
    {
        try
        {
            _gameActive = false;
            ClearLedMatrix();

            // Stop dynamic game music
            _ = Task.Run(async () => await _audioService.StopGameMusicAsync());

            // Send stop command to Arduino
            if (_serialPort?.IsOpen == true)
            {
                try
                {
                    _serialPort.WriteLine("STOP_GAME");
                }
                catch (Exception)
                {
                    // Silent error handling
                }
            }

            // Update UI state
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    // Stop dynamic game music
                    _ = Task.Run(async () => await _audioService.StopGameMusicAsync());

                    StartGameButton.IsEnabled = false;
                    StopGameButton.IsEnabled = false;
                    StatusText.Text = "🛑 GAME OVER - Limite de erros atingido! Termine sessão";
                }
                catch (Exception)
                {
                    // Silent error handling
                }
            });
        }
        catch (Exception)
        {
            // Silent error handling for better performance
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
                    roundsCompletedWindow.SetGameSummary(gameSummary, _currentUser.Name);                    // Handle return to login event - this MUST happen
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

                            // Close the RoundsCompletedWindow first
                            try
                            {
                                if (sender is RoundsCompletedWindow window)
                                {
                                    window.Close();
                                }
                            }
                            catch (Exception windowEx)
                            {
                                AddDebugMessage($"[SESSÃO] Erro ao fechar RoundsCompletedWindow: {windowEx.Message}");
                            }

                            // End the session
                            if (_currentUser != null)
                                _sessionService.EndSession(_currentUser.Id);

                            // Small delay to ensure session is properly ended
                            await Task.Delay(100);

                            // Force close this window
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
                    };                    // Show as independent fullscreen window (not modal to allow proper fullscreen)
                    try
                    {
                        AddDebugMessage("[SESSÃO] 📺 Tentando mostrar RoundsCompletedWindow em fullscreen");

                        // Strategy 1: Hide main window first, then show fullscreen window
                        this.Hide();
                        await Task.Delay(50); // Brief delay to ensure main window is hidden

                        roundsCompletedWindow.Show();

                        // Wait for the window to be shown and processed
                        await Task.Delay(200);

                        AddDebugMessage("[SESSÃO] ✅ RoundsCompletedWindow mostrada com sucesso");
                    }
                    catch (Exception ex)
                    {
                        AddDebugMessage($"Erro ao mostrar janela fullscreen (estratégia 1): {ex.Message}");

                        // Strategy 2: Try alternative approach
                        try
                        {
                            AddDebugMessage("[SESSÃO] 🔄 Tentando estratégia alternativa para mostrar fullscreen");
                            this.WindowState = WindowState.Minimized;
                            await Task.Delay(50);
                            roundsCompletedWindow.Show();
                            await Task.Delay(100);
                            roundsCompletedWindow.Activate();
                        }
                        catch (Exception ex2)
                        {
                            AddDebugMessage($"Erro na estratégia alternativa: {ex2.Message}");
                            // Last resort: just show the window
                            roundsCompletedWindow.Show();
                        }

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
