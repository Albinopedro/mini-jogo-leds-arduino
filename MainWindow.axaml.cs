using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using miniJogo.Views;
using miniJogo.Services;
using miniJogo.Models;

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
    private readonly ScoreService _scoreService;

    // LED visualization
    private readonly Ellipse[,] _ledMatrix = new Ellipse[4, 4];

    // Game instructions
    private readonly Dictionary<int, string> _gameInstructions = new()
    {
        { 1, "PEGA-LUZ:\nQuando um LED acender, pressione a tecla correspondente o mais rápido possível!\nTeclas: 0-9, A-F para os 16 LEDs da matriz 4x4." },
        { 2, "SEQUÊNCIA MALUCA:\nMemorize a sequência de LEDs que acendem e repita na ordem correta.\nA sequência fica mais longa a cada rodada!" },
        { 3, "GATO E RATO:\nControle o gato (LED fixo) para pegar o rato (LED piscante).\nUse as teclas 0-9, A-F para mover o gato." },
        { 4, "ESQUIVA METEOROS:\nEsquive dos meteoros que aparecem! Mova-se com as teclas 0-9, A-F.\nSobreviva o máximo de tempo possível." },
        { 5, "GUITAR HERO:\nToque as notas quando elas chegarem na linha inferior!\nUse as teclas 0-3 para as 4 colunas da matriz." }
    };

    public MainWindow()
    {
        InitializeComponent();
        _scoreService = new ScoreService();
        InitializeLedMatrix();
        InitializeTimer();
        RefreshPorts();

        // Set initial game selection
        GameModeListBox.SelectedIndex = 0;
    }

    private void InitializeLedMatrix()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                var led = new Ellipse
                {
                    Width = 60,
                    Height = 60,
                    Fill = Brushes.Gray,
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    Margin = new Avalonia.Thickness(5)
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
        _statusTimer = new Timer(100);
        _statusTimer.Elapsed += async (sender, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_serialPort?.IsOpen == true)
                {
                    try
                    {
                        _serialPort.WriteLine("GET_STATUS");
                    }
                    catch { }
                }
            });
        };
        _statusTimer.Start();
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
            _serialPort.Dispose();
            _serialPort = null;

            ConnectionStatus.Fill = Brushes.Red;
            ConnectionText.Text = "Desconectado";
            ConnectButton.Content = "🔗 Conectar";
            StartGameButton.IsEnabled = false;
            StopGameButton.IsEnabled = false;

            StatusText.Text = "Arduino desconectado.";
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

                    // Wait for Arduino ready signal
                    await Task.Delay(2000);

                    ConnectionStatus.Fill = Brushes.Green;
                    ConnectionText.Text = "Conectado";
                    ConnectButton.Content = "🔌 Desconectar";
                    StartGameButton.IsEnabled = true;

                    StatusText.Text = $"Arduino conectado em {selectedPort}";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Erro ao conectar: {ex.Message}";
                }
            }
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort?.IsOpen != true) return;

        try
        {
            string data = _serialPort.ReadLine().Trim();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProcessArduinoMessage(data);
            });
        }
        catch { }
    }

    private void ProcessArduinoMessage(string message)
    {
        if (message.StartsWith("STATUS:"))
        {
            var parts = message.Substring(7).Split(',');
            if (parts.Length >= 4)
            {
                _currentGameMode = int.Parse(parts[0]);
                _gameActive = parts[1] == "1";
                _score = int.Parse(parts[2]);
                _level = int.Parse(parts[3]);

                UpdateUI();
            }
        }
        else if (message.StartsWith("EVENT:"))
        {
            ProcessGameEvent(message.Substring(6));
        }
        else if (message == "ARDUINO_READY")
        {
            StatusText.Text = "Arduino pronto para jogar!";
        }
    }

    private void ProcessGameEvent(string eventData)
    {
        var parts = eventData.Split(',');
        var eventType = parts[0];

        switch (eventType)
        {
            case "GAME_STARTED":
                StatusText.Text = "Jogo iniciado!";
                StopGameButton.IsEnabled = true;
                StartGameButton.IsEnabled = false;
                _gameStartTime = DateTime.Now;
                break;

            case "GAME_STOPPED":
                StatusText.Text = $"Jogo finalizado! Pontuação final: {parts[1]}";
                StopGameButton.IsEnabled = false;
                StartGameButton.IsEnabled = true;
                ClearLedMatrix();
                
                // Save score if player name is set and game was active
                if (!string.IsNullOrEmpty(_playerName) && _score > 0)
                {
                    SaveGameScore();
                }
                break;

            case "PEGA_LUZ_TARGET":
                if (parts.Length > 1)
                {
                    int ledIndex = int.Parse(parts[1]);
                    HighlightLed(ledIndex, Brushes.Yellow);
                    StatusText.Text = $"Aperte a tecla {GetKeyForLed(ledIndex)}!";
                }
                break;

            case "PEGA_LUZ_HIT":
                StatusText.Text = $"Acertou! Tempo de reação: {parts[1]}ms";
                break;

            case "PEGA_LUZ_MISS":
                StatusText.Text = "Errou! Tente novamente.";
                break;

            case "PEGA_LUZ_TIMEOUT":
                StatusText.Text = "Tempo esgotado!";
                break;

            case "SEQUENCIA_SHOW_START":
                StatusText.Text = "Memorize a sequência...";
                break;

            case "SEQUENCIA_INPUT_START":
                StatusText.Text = "Agora repita a sequência!";
                break;

            case "SEQUENCIA_COMPLETE":
                StatusText.Text = "Sequência correta! Próximo nível...";
                break;

            case "SEQUENCIA_WRONG":
                StatusText.Text = "Sequência errada! Tente novamente.";
                break;

            case "GATO_CAUGHT_RATO":
                StatusText.Text = "Gato pegou o rato! +1 ponto";
                break;

            case "METEOR_HIT":
                StatusText.Text = "Atingido por meteoro! -1 ponto";
                break;

            case "GUITAR_NOTE_SPAWN":
                StatusText.Text = "Nova nota apareceu!";
                break;

            case "GUITAR_HIT":
                StatusText.Text = $"Nota acertada! Timing: {parts[1]}ms";
                break;

            case "GUITAR_MISS":
                StatusText.Text = "Nota perdida!";
                break;

            case "GUITAR_WRONG":
                StatusText.Text = "Tecla errada!";
                break;
        }
    }

    private void UpdateUI()
    {
        ScoreText.Text = _score.ToString();
        LevelText.Text = _level.ToString();

        string gameName = _currentGameMode switch
        {
            1 => "Pega-Luz",
            2 => "Sequência Maluca",
            3 => "Gato e Rato",
            4 => "Esquiva Meteoros",
            5 => "Guitar Hero",
            _ => "Nenhum"
        };

        CurrentGameText.Text = gameName;
    }

    private void HighlightLed(int ledIndex, IBrush color)
    {
        ClearLedMatrix();

        int row = ledIndex / 4;
        int col = ledIndex % 4;

        if (row >= 0 && row < 4 && col >= 0 && col < 4)
        {
            _ledMatrix[row, col].Fill = color;
        }
    }

    private void ClearLedMatrix()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                _ledMatrix[row, col].Fill = Brushes.Gray;
            }
        }
    }

    private string GetKeyForLed(int ledIndex)
    {
        return ledIndex switch
        {
            0 => "0", 1 => "1", 2 => "2", 3 => "3",
            4 => "4", 5 => "5", 6 => "6", 7 => "7",
            8 => "8", 9 => "9", 10 => "A", 11 => "B",
            12 => "C", 13 => "D", 14 => "E", 15 => "F",
            _ => "?"
        };
    }

    private int GetLedForKey(Key key)
    {
        return key switch
        {
            Key.D0 => 0, Key.D1 => 1, Key.D2 => 2, Key.D3 => 3,
            Key.D4 => 4, Key.D5 => 5, Key.D6 => 6, Key.D7 => 7,
            Key.D8 => 8, Key.D9 => 9, Key.A => 10, Key.B => 11,
            Key.C => 12, Key.D => 13, Key.E => 14, Key.F => 15,
            _ => -1
        };
    }

    private void Window_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_serialPort?.IsOpen != true) return;

        try
        {
            switch (e.Key)
            {
                case Key.F1:
                    StartGameButton_Click(null, new RoutedEventArgs());
                    break;

                case Key.F2:
                    StopGameButton_Click(null, new RoutedEventArgs());
                    break;

                case Key.F3:
                    ResetScoreButton_Click(null, new RoutedEventArgs());
                    break;

                case Key.F4:
                    ViewScoresButton_Click(null, new RoutedEventArgs());
                    break;

                default:
                    int ledIndex = GetLedForKey(e.Key);
                    if (ledIndex >= 0 && _gameActive)
                    {
                        _serialPort.WriteLine($"KEY_PRESS:{ledIndex}");
                        HighlightLed(ledIndex, Brushes.LightBlue);

                        // Clear highlight after a short delay
                        Task.Delay(200).ContinueWith(_ =>
                        {
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                _ledMatrix[ledIndex / 4, ledIndex % 4].Fill = Brushes.Gray;
                            });
                        });
                    }
                    break;
            }
        }
        catch { }
    }

    private void SavePlayerButton_Click(object? sender, RoutedEventArgs e)
    {
        _playerName = PlayerNameTextBox.Text?.Trim() ?? "";
        if (!string.IsNullOrEmpty(_playerName))
        {
            PlayerDisplayText.Text = $"Jogador: {_playerName}";
            StatusText.Text = $"Nome do jogador salvo: {_playerName}";
        }
        else
        {
            PlayerDisplayText.Text = "Jogador: Não definido";
        }
    }

    private void GameModeListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GameModeListBox.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            int gameMode = int.Parse(tag);

            if (_gameInstructions.TryGetValue(gameMode, out string? instructions))
            {
                InstructionsText.Text = instructions;
            }

            if (!_gameActive)
            {
                _currentGameMode = gameMode;
                UpdateUI();
            }
        }
    }

    private void StartGameButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen != true) return;

        if (string.IsNullOrEmpty(_playerName))
        {
            StatusText.Text = "Por favor, defina um nome de jogador primeiro!";
            return;
        }

        if (GameModeListBox.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            int gameMode = int.Parse(tag);

            try
            {
                _serialPort.WriteLine($"START_GAME:{gameMode}");
                StatusText.Text = $"Iniciando jogo: {item.Content}";
                _gameStartTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erro ao iniciar jogo: {ex.Message}";
            }
        }
    }

    private void StopGameButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen != true) return;
        
        try
        {
            _serialPort.WriteLine("STOP_GAME");
            StatusText.Text = "Parando jogo...";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro ao parar jogo: {ex.Message}";
        }
    }

    private void ResetScoreButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_serialPort?.IsOpen != true) return;
        
        try
        {
            _serialPort.WriteLine("RESET_SCORE");
            StatusText.Text = "Pontuação resetada!";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro ao resetar pontuação: {ex.Message}";
        }
    }

    private void RefreshPortsButton_Click(object? sender, RoutedEventArgs e)
    {
        RefreshPorts();
        StatusText.Text = "Lista de portas atualizada.";
    }

    private void ViewScoresButton_Click(object? sender, RoutedEventArgs e)
    {
        var scoreWindow = new ScoreWindow();
        scoreWindow.ShowDialog(this);
    }

    private async void SaveGameScore()
    {
        try
        {
            var score = new PlayerScore
            {
                PlayerName = _playerName,
                Game = (GameMode)_currentGameMode,
                Score = _score,
                Level = _level,
                Date = _gameStartTime,
                Duration = DateTime.Now - _gameStartTime
            };

            await _scoreService.SaveScoreAsync(score);
            StatusText.Text = $"Pontuação salva! {_playerName}: {_score} pontos";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Erro ao salvar pontuação: {ex.Message}";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _statusTimer?.Stop();
        _statusTimer?.Dispose();
        
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }
        
        base.OnClosed(e);
    }
}