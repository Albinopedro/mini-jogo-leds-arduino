using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Globalization;
using Avalonia.Controls.Shapes;
using miniJogo.Models;
using miniJogo.Services;
using miniJogo.Views;

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
    private DebugWindow? _debugWindow;

    // LED Matrix (4x4)
    private readonly Ellipse[,] _ledMatrix = new Ellipse[4, 4];
    
    // LED auto-restore timer
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

    public MainWindow()
    {
        InitializeComponent();
        InitializeLedMatrix();
        InitializeTimer();
        _scoreService = new ScoreService();
        RefreshPorts();

        // Set initial values
        PlayerDisplayText.Text = "👤 Jogador: Não definido";
        GameDescriptionText.Text = "Selecione um jogo para ver a descrição...";
        CurrentGameText.Text = "Nenhum";

        // Populate game mode combo box
        PopulateGameModeComboBox();
        
        // Set first game as default
        GameModeComboBox.SelectedIndex = 0;
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
                    Stroke = Brushes.White,
                    StrokeThickness = 3,
                    Margin = new Avalonia.Thickness(10)
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
            ConnectionStatus.Fill = Brushes.White;
            ConnectionBorder.Background = new SolidColorBrush(Color.FromRgb(197, 48, 48));
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
                    ConnectionStatus.Fill = Brushes.LimeGreen;
                    ConnectionBorder.Background = new SolidColorBrush(Color.FromRgb(56, 161, 105));
                    StartGameButton.IsEnabled = true;
                    AddDebugMessage($"Arduino conectado na porta {selectedPort}");

                    // Send initialization command
                    await Task.Delay(2000); // Wait for Arduino to initialize
                    _serialPort.WriteLine("INIT");
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Erro ao conectar: {ex.Message}");
                    await ShowMessage("Erro de Conexão", $"Não foi possível conectar ao Arduino:\n{ex.Message}");
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
            StatusText.Text = "🟢 Arduino pronto! Selecione um jogo e aperte Iniciar.";
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
                _gameActive = false;
                StartGameButton.IsEnabled = true;
                StopGameButton.IsEnabled = false;
                
                // Sync final score from Arduino if provided
                if (int.TryParse(eventValue, out var finalScore))
                {
                    _score = finalScore;
                }
                
                StatusText.Text = $"🎮 GAME OVER! Pontuação Final: {_score}";
                SaveGameScore();
                AddDebugMessage($"Fim de jogo - Pontuação final: {_score}");
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
                    AddDebugMessage($"Tecla incorreta pressionada: {wrongKey}");
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
                    AddDebugMessage($"Level up: {level}, Score: {_score}");
                }
                break;

            case "GAME_STARTED":
                if (int.TryParse(eventValue, out var gameMode))
                {
                    _currentGameMode = gameMode;
                    _gameActive = true;
                    StartGameButton.IsEnabled = false;
                    StopGameButton.IsEnabled = true;
                    StatusText.Text = "🎮 Jogo iniciado! Boa sorte!";
                    AddDebugMessage($"Jogo iniciado: modo {gameMode}");
                    UpdateUI();
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
                break;

            case "ROLETA_MAX_WIN":
                StatusText.Text = "🏆 VITÓRIA MÁXIMA! Você é corajoso demais!";
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
                break;

            case "SNIPER_TIMEOUT":
                StatusText.Text = "⏰ MUITO LENTO! O alvo desapareceu antes de você atirar.";
                break;

            case "SNIPER_VICTORY":
                StatusText.Text = "🏆 LEGENDÁRIO! 10/10 acertos! Você é um sniper de elite!";
                break;

            case "COMBO":
                if (int.TryParse(eventValue, out var comboCount))
                {
                    StatusText.Text = $"🔥 COMBO x{comboCount}! Pontuação multiplicada!";
                }
                break;

            case "PERFECT":
                StatusText.Text = "⭐ PERFEITO! Timing excelente!";
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
        if (ledIndex < 0 || ledIndex >= 16) return;

        var row = ledIndex / 4;
        var col = ledIndex % 4;

        if (row < 4 && col < 4)
        {
            _ledMatrix[row, col].Fill = GetLedActiveColor(row);
            
            // Auto-restore LED after 200ms as fallback (in case KeyUp is missed)
            lock (_ledTimersLock)
            {
                // Cancel existing timer for this LED if any
                if (_ledTimers.TryGetValue(ledIndex, out var existingTimer))
                {
                    existingTimer.Dispose();
                }
                
                // Create new timer to restore LED color
                _ledTimers[ledIndex] = new System.Threading.Timer(
                    _ => 
                    {
                        Dispatcher.UIThread.InvokeAsync(() => RestoreLedColor(ledIndex));
                        lock (_ledTimersLock)
                        {
                            if (_ledTimers.TryGetValue(ledIndex, out var timer))
                            {
                                timer.Dispose();
                                _ledTimers.Remove(ledIndex);
                            }
                        }
                    },
                    null,
                    TimeSpan.FromMilliseconds(200),
                    Timeout.InfiniteTimeSpan
                );
            }
        }
    }

    private IBrush GetLedDefaultColor(int row)
    {
        return row switch
        {
            0 => new SolidColorBrush(Color.FromRgb(80, 20, 20)),    // Dark red
            1 => new SolidColorBrush(Color.FromRgb(80, 60, 20)),    // Dark yellow
            2 => new SolidColorBrush(Color.FromRgb(20, 80, 20)),    // Dark green
            3 => new SolidColorBrush(Color.FromRgb(20, 20, 80)),    // Dark blue
            _ => Brushes.Gray
        };
    }

    private IBrush GetLedActiveColor(int row)
    {
        return row switch
        {
            0 => new SolidColorBrush(Color.FromRgb(255, 100, 100)), // Bright red
            1 => new SolidColorBrush(Color.FromRgb(255, 255, 100)), // Bright yellow
            2 => new SolidColorBrush(Color.FromRgb(100, 255, 100)), // Bright green
            3 => new SolidColorBrush(Color.FromRgb(100, 150, 255)), // Bright blue
            _ => Brushes.White
        };
    }

    private void ClearLedMatrix()
    {
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                _ledMatrix[row, col].Fill = GetLedDefaultColor(row);
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
        if (ledIndex < 0 || ledIndex >= 16) return;

        var row = ledIndex / 4;
        var col = ledIndex % 4;

        if (row < 4 && col < 4)
        {
            _ledMatrix[row, col].Fill = GetLedDefaultColor(row);
            
            // Cancel auto-restore timer since we're manually restoring
            lock (_ledTimersLock)
            {
                if (_ledTimers.TryGetValue(ledIndex, out var timer))
                {
                    timer.Dispose();
                    _ledTimers.Remove(ledIndex);
                }
            }
        }
    }

    private void SavePlayerButton_Click(object? sender, RoutedEventArgs e)
    {
        var name = PlayerNameTextBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            _playerName = name;
            PlayerDisplayText.Text = $"👤 Jogador: {_playerName}";
            AddDebugMessage($"Nome do jogador definido: {_playerName}");
        }
    }

    private void GameModeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
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

                // Enable start button if connected
                if (_serialPort?.IsOpen == true)
                {
                    StartGameButton.IsEnabled = true;
                }
            }
        }
    }

    private void OpenDebugButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_debugWindow == null)
        {
            _debugWindow = new DebugWindow();
            _debugWindow.Closed += (s, e) => _debugWindow = null;
        }
        _debugWindow.Show();
        _debugWindow.Activate();
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

        _gameActive = true;
        _score = 0;
        _level = 1;
        _gameStartTime = DateTime.Now;

        StartGameButton.IsEnabled = false;
        StopGameButton.IsEnabled = true;

        var command = $"START_GAME:{_currentGameMode}";
        _serialPort.WriteLine(command);

        StatusText.Text = "🚀 Jogo iniciado! Boa sorte!";
        AddDebugMessage($"Jogo iniciado: Modo {_currentGameMode}");
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

    private void RefreshPortsButton_Click(object? sender, RoutedEventArgs e)
    {
        RefreshPorts();
        AddDebugMessage("Lista de portas atualizada");
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

    private async void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new Window
        {
            Title = "⚙️ Configurações",
            Width = 500,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };

        stackPanel.Children.Add(new TextBlock
        {
            Text = "⚙️ Configurações do Jogo",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Margin = new Avalonia.Thickness(0, 0, 0, 15)
        });

        // Player name setting
        var playerPanel = new StackPanel { Spacing = 5 };
        playerPanel.Children.Add(new TextBlock { Text = "👤 Nome do Jogador:" });
        var playerTextBox = new TextBox { Text = _playerName, Watermark = "Digite seu nome..." };
        playerPanel.Children.Add(playerTextBox);
        stackPanel.Children.Add(playerPanel);

        // Serial port settings
        var portPanel = new StackPanel { Spacing = 5 };
        portPanel.Children.Add(new TextBlock { Text = "🔌 Porta Serial:" });
        var portCombo = new ComboBox();
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

        var saveButton = new Button { Content = "💾 Salvar", MinWidth = 80 };
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

        var cancelButton = new Button { Content = "❌ Cancelar", MinWidth = 80 };
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

🎯 JOGOS DISPONÍVEIS:
• Pega-Luz: Reflexos rápidos
• Sequência Maluca: Memória
• Gato e Rato: Perseguição
• Esquiva Meteoros: Sobrevivência
• Guitar Hero: Ritmo
• Corrida Infinita: Velocidade
• Quebra-Cabeça: Lógica
• Reflexo Rápido: Tempo de reação

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

💡 DICAS
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
        _statusTimer?.Dispose();

        // Clean up LED timers
        lock (_ledTimersLock)
        {
            foreach (var timer in _ledTimers.Values)
            {
                timer.Dispose();
            }
            _ledTimers.Clear();
        }

        if (_serialPort?.IsOpen == true)
        {
            try
            {
                _serialPort.WriteLine("DISCONNECT");
                _serialPort.Close();
            }
            catch { }
        }

        _debugWindow?.Close();
        base.OnClosed(e);
    }

    private async Task ShowMessage(string title, string message)
    {
        var messageWindow = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
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
