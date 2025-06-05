using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace miniJogo.Services
{
    public enum AudioEvent
    {
        // Sistema
        Startup,
        LoginSuccess,
        LoginError,
        ButtonClick,
        ButtonHover,
        Notification,
        Shutdown,
        
        // Jogos Gerais
        GameStart,
        GameOver,
        Victory,
        LevelUp,
        ScoreHit,
        Error,
        Countdown,
        
        // Pega-Luz
        PegaLuzHit,
        PegaLuzMiss,
        
        // Sequência Maluca
        SequenciaCorrect,
        SequenciaWrong,
        SequenciaShow,
        
        // Gato e Rato
        GatoCapture,
        GatoMove,
        RatoMove,
        
        // Esquiva Meteoros
        MeteoroExplosion,
        MeteoroSpawn,
        PlayerMove,
        
        // Guitar Hero
        GuitarNote,
        GuitarPerfect,
        GuitarMiss,
        

        
        // Lightning Strike
        LightningFlash,
        LightningCorrect,
        LightningWrong,
        
        // Sniper Mode
        SniperShot,
        SniperHit,
        SniperMiss,
        
        // Efeitos Especiais
        MatrixSound,
        PulseSound,
        Fireworks,
        DemoMusic,
        BounceSound,
        PowerDown,
        
        // Música de Fundo
        LoginBackgroundMusic,
        
        // Controles e Teclas
        KeyPress,
        KeyRelease,
        ArrowKey,
        FunctionKey,
        GameControl
    }
    
    public class AudioService
    {
        private readonly Dictionary<AudioEvent, string> _audioFiles;
        private bool _isEnabled = true;
        private float _masterVolume = 0.7f;
        private bool _isWindows;
        private bool _isLinux;
        
        // Background music management
        private CancellationTokenSource? _backgroundMusicCancellationTokenSource;
        private Task? _backgroundMusicTask;
        private bool _backgroundMusicPlaying = false;
        
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => _isEnabled = value; 
        }
        
        public float MasterVolume 
        { 
            get => _masterVolume; 
            set => _masterVolume = Math.Clamp(value, 0.0f, 1.0f); 
        }

        public AudioService()
        {
            _audioFiles = InitializeAudioFiles();
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            
            System.Diagnostics.Debug.WriteLine($"🎵 AudioService iniciado - OS: {(_isWindows ? "Windows" : _isLinux ? "Linux" : "Other")}");
            Console.WriteLine($"🎵 AudioService iniciado - OS: {(_isWindows ? "Windows" : _isLinux ? "Linux" : "Other")}");
            
            // Test audio capability
            TestAudioCapability();
        }
        
        private void TestAudioCapability()
        {
            try
            {
                Console.WriteLine("🎵 Testando capacidade de áudio...");
                PlaySound(AudioEvent.ButtonClick);
                System.Diagnostics.Debug.WriteLine("🎵 Teste de áudio: OK");
                Console.WriteLine("🎵 Teste de áudio: OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Teste de áudio falhou: {ex.Message}");
                Console.WriteLine($"⚠️ Teste de áudio falhou: {ex.Message}");
            }
        }
        
        private Dictionary<AudioEvent, string> InitializeAudioFiles()
        {
            return new Dictionary<AudioEvent, string>
            {
                // Sistema
                [AudioEvent.Startup] = "Assets/Audio/Sistema/startup.wav",
                [AudioEvent.LoginSuccess] = "Assets/Audio/Sistema/login_success.wav",
                [AudioEvent.LoginError] = "Assets/Audio/Sistema/login_error.wav",
                [AudioEvent.ButtonClick] = "Assets/Audio/Sistema/button_click.wav",
                [AudioEvent.ButtonHover] = "Assets/Audio/Sistema/button_hover.wav",
                [AudioEvent.Notification] = "Assets/Audio/Sistema/notification.wav",
                [AudioEvent.Shutdown] = "Assets/Audio/Sistema/shutdown.wav",
                
                // Jogos Gerais
                [AudioEvent.GameStart] = "Assets/Audio/Jogos/game_start.wav",
                [AudioEvent.GameOver] = "Assets/Audio/Jogos/game_over.wav",
                [AudioEvent.Victory] = "Assets/Audio/Jogos/victory.wav",
                [AudioEvent.LevelUp] = "Assets/Audio/Jogos/level_up.wav",
                [AudioEvent.ScoreHit] = "Assets/Audio/Jogos/score_hit.wav",
                [AudioEvent.Error] = "Assets/Audio/Jogos/error_sound.wav",
                [AudioEvent.Countdown] = "Assets/Audio/Jogos/countdown.wav",
                
                // Específicos por Jogo
                [AudioEvent.PegaLuzHit] = "Assets/Audio/Específicos/pega_luz_hit.wav",
                [AudioEvent.PegaLuzMiss] = "Assets/Audio/Específicos/pega_luz_miss.wav",
                [AudioEvent.SequenciaCorrect] = "Assets/Audio/Específicos/sequencia_correct.wav",
                [AudioEvent.SequenciaWrong] = "Assets/Audio/Específicos/sequencia_wrong.wav",
                [AudioEvent.SequenciaShow] = "Assets/Audio/Específicos/sequencia_show.wav",
                [AudioEvent.GatoCapture] = "Assets/Audio/Específicos/gato_capture.wav",
                [AudioEvent.GatoMove] = "Assets/Audio/Específicos/gato_move.wav",
                [AudioEvent.RatoMove] = "Assets/Audio/Específicos/rato_move.wav",
                [AudioEvent.MeteoroExplosion] = "Assets/Audio/Específicos/meteoro_explosion.wav",
                [AudioEvent.MeteoroSpawn] = "Assets/Audio/Específicos/meteoro_spawn.wav",
                [AudioEvent.PlayerMove] = "Assets/Audio/Específicos/player_move.wav",
                [AudioEvent.GuitarNote] = "Assets/Audio/Específicos/guitar_note.wav",
                [AudioEvent.GuitarPerfect] = "Assets/Audio/Específicos/guitar_perfect.wav",
                [AudioEvent.GuitarMiss] = "Assets/Audio/Específicos/guitar_miss.wav",
                [AudioEvent.LightningFlash] = "Assets/Audio/Específicos/lightning_flash.wav",
                [AudioEvent.LightningCorrect] = "Assets/Audio/Específicos/lightning_correct.wav",
                [AudioEvent.LightningWrong] = "Assets/Audio/Específicos/lightning_wrong.wav",
                [AudioEvent.SniperShot] = "Assets/Audio/Específicos/sniper_shot.wav",
                [AudioEvent.SniperHit] = "Assets/Audio/Específicos/sniper_hit.wav",
                [AudioEvent.SniperMiss] = "Assets/Audio/Específicos/sniper_miss.wav",
                
                // Efeitos Especiais
                [AudioEvent.MatrixSound] = "Assets/Audio/Efeitos/matrix_sound.wav",
                [AudioEvent.PulseSound] = "Assets/Audio/Efeitos/pulse_sound.wav",
                [AudioEvent.Fireworks] = "Assets/Audio/Efeitos/fireworks.wav",
                [AudioEvent.DemoMusic] = "Assets/Audio/Efeitos/demo_music.wav",
                [AudioEvent.BounceSound] = "Assets/Audio/Efeitos/bounce_sound.wav",
                [AudioEvent.PowerDown] = "Assets/Audio/Efeitos/power_down.wav",
                
                // Música de Fundo
                [AudioEvent.LoginBackgroundMusic] = "Assets/Audio/Ambiente/C418 - Moog City 2.mp3",
                
                // Controles e Teclas
                [AudioEvent.KeyPress] = "Assets/Audio/Sistema/button_click.wav",
                [AudioEvent.KeyRelease] = "Assets/Audio/Sistema/button_hover.wav",
                [AudioEvent.ArrowKey] = "Assets/Audio/Específicos/player_move.wav",
                [AudioEvent.FunctionKey] = "Assets/Audio/Efeitos/bounce_sound.wav",
                [AudioEvent.GameControl] = "Assets/Audio/Sistema/button_click.wav"
            };
        }
        
        public async Task PlaySoundAsync(AudioEvent audioEvent, float volumeMultiplier = 1.0f)
        {
            if (!_isEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"🔇 Áudio desabilitado - ignorando {audioEvent}");
                Console.WriteLine($"🔇 Áudio desabilitado - ignorando {audioEvent}");
                return;
            }
                
            try
            {
                System.Diagnostics.Debug.WriteLine($"🎵 Tentando reproduzir: {audioEvent}");
                Console.WriteLine($"🎵 Tentando reproduzir: {audioEvent}");
                
                // Try to play the audio file first
                if (_audioFiles.TryGetValue(audioEvent, out var filePath) && File.Exists(filePath))
                {
                    Console.WriteLine($"📁 Arquivo encontrado: {filePath}");
                    await PlayAudioFileAsync(filePath, audioEvent);
                    return;
                }
                else
                {
                    Console.WriteLine($"❌ Arquivo não encontrado para {audioEvent}: {_audioFiles.GetValueOrDefault(audioEvent, "N/A")}");
                }
                
                // Fallback to system sounds
                Console.WriteLine($"🔊 Usando fallback de sistema para {audioEvent}");
                await PlaySystemSoundAsync(audioEvent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao reproduzir som {audioEvent}: {ex.Message}");
                Console.WriteLine($"⚠️ Erro ao reproduzir som {audioEvent}: {ex.Message}");
                
                // Ultimate fallback: console output
                Console.WriteLine($"🎵 {audioEvent}");
            }
        }
        
        private async Task PlayAudioFileAsync(string filePath, AudioEvent audioEvent)
        {
            try
            {
                if (_isWindows)
                {
                    await PlayWindowsAudioAsync(filePath);
                }
                else if (_isLinux)
                {
                    await PlayLinuxAudioAsync(filePath);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ OS não suportado para reprodução de arquivo: {audioEvent}");
                    await PlaySystemSoundAsync(audioEvent);
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Som reproduzido com sucesso: {audioEvent}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao reproduzir arquivo {filePath}: {ex.Message}");
                await PlaySystemSoundAsync(audioEvent);
            }
        }
        
        private async Task PlayWindowsAudioAsync(string filePath)
        {
            try
            {
                // Try using System.Media.SoundPlayer on Windows
                var soundPlayer = new System.Media.SoundPlayer(filePath);
                await Task.Run(() => 
                {
                    soundPlayer.Load();
                    soundPlayer.Play();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ SoundPlayer falhou: {ex.Message}");
                
                // Fallback to PowerShell
                await Task.Run(() =>
                {
                    try
                    {
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell",
                            Arguments = $"-c \"(New-Object Media.SoundPlayer '{filePath}').PlaySync()\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        
                        using var process = System.Diagnostics.Process.Start(startInfo);
                        process?.WaitForExit(2000); // Timeout after 2 seconds
                    }
                    catch (Exception psEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ PowerShell audio fallback falhou: {psEx.Message}");
                    }
                });
            }
        }
        
        private async Task PlayLinuxAudioAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"🐧 Tentando reproduzir no Linux: {filePath}");
                    
                    // Try multiple Linux audio players
                    var players = new[] { "aplay", "paplay", "ffplay", "mpg123", "mplayer" };
                    
                    foreach (var player in players)
                    {
                        try
                        {
                            Console.WriteLine($"🔊 Testando player: {player}");
                            
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = player,
                                Arguments = player switch
                                {
                                    "aplay" => $"-q \"{filePath}\"",
                                    "paplay" => $"\"{filePath}\"",
                                    "ffplay" => $"-nodisp -autoexit -v quiet \"{filePath}\"",
                                    "mpg123" => $"-q \"{filePath}\"",
                                    "mplayer" => $"-really-quiet \"{filePath}\"",
                                    _ => $"\"{filePath}\""
                                },
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            };
                            
                            Console.WriteLine($"📋 Comando: {player} {startInfo.Arguments}");
                            
                            using var process = System.Diagnostics.Process.Start(startInfo);
                            if (process != null)
                            {
                                process.WaitForExit(3000); // Timeout after 3 seconds
                                Console.WriteLine($"🏁 Exit code: {process.ExitCode}");
                                if (process.ExitCode == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine($"✅ Audio reproduzido com {player}");
                                    Console.WriteLine($"✅ Audio reproduzido com {player}");
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ {player} falhou: {ex.Message}");
                            Console.WriteLine($"⚠️ {player} falhou: {ex.Message}");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("⚠️ Nenhum player de áudio Linux funcionou");
                    Console.WriteLine("⚠️ Nenhum player de áudio Linux funcionou");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Erro geral no Linux audio: {ex.Message}");
                    Console.WriteLine($"⚠️ Erro geral no Linux audio: {ex.Message}");
                }
            });
        }
        
        private async Task PlaySystemSoundAsync(AudioEvent audioEvent)
        {
            await Task.Run(() =>
            {
                try
                {
                    var frequency = GetFrequencyForEvent(audioEvent);
                    var duration = GetDurationForEvent(audioEvent);
                    
                    if (_isWindows)
                    {
                        try
                        {
                            Console.Beep(frequency, duration);
                            System.Diagnostics.Debug.WriteLine($"🔊 Windows beep: {frequency}Hz por {duration}ms");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Windows beep falhou: {ex.Message}");
                        }
                    }
                    else if (_isLinux)
                    {
                        try
                        {
                            // Try Linux beep command
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "beep",
                                Arguments = $"-f {frequency} -l {duration}",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            };
                            
                            using var process = System.Diagnostics.Process.Start(startInfo);
                            process?.WaitForExit(1000);
                            
                            System.Diagnostics.Debug.WriteLine($"🔊 Linux beep: {frequency}Hz por {duration}ms");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Linux beep falhou: {ex.Message}");
                            
                            // Ultimate fallback: speaker control
                            try
                            {
                                var startInfo = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "bash",
                                    Arguments = $"-c \"echo -e '\\a'\"",
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                };
                                
                                using var process = System.Diagnostics.Process.Start(startInfo);
                                process?.WaitForExit(500);
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine("⚠️ Até mesmo o beep do sistema falhou");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Sistema beep falhou: {ex.Message}");
                }
            });
        }
        
        public void PlaySound(AudioEvent audioEvent, float volumeMultiplier = 1.0f)
        {
            _ = Task.Run(() => PlaySoundAsync(audioEvent, volumeMultiplier));
        }
        
        private int GetFrequencyForEvent(AudioEvent audioEvent)
        {
            return audioEvent switch
            {
                // Sons de sucesso - frequências altas
                AudioEvent.LoginSuccess or AudioEvent.Victory or AudioEvent.LevelUp => 1000,
                AudioEvent.ScoreHit or AudioEvent.PegaLuzHit or AudioEvent.GuitarNote => 800,
                AudioEvent.SequenciaCorrect or AudioEvent.GatoCapture => 900,
                AudioEvent.LightningCorrect or AudioEvent.SniperHit => 1200,
                
                // Sons de erro - frequências baixas
                AudioEvent.LoginError or AudioEvent.Error or AudioEvent.GameOver => 300,
                AudioEvent.SequenciaWrong or AudioEvent.GuitarMiss => 250,
                AudioEvent.LightningWrong or AudioEvent.SniperMiss => 200,
                
                // Sons de sistema - frequências médias
                AudioEvent.ButtonClick => 600,
                AudioEvent.ButtonHover => 500,
                AudioEvent.Notification or AudioEvent.GameStart => 700,
                AudioEvent.Startup => 400,
                
                // Sons específicos
                AudioEvent.MeteoroExplosion => 150,
                AudioEvent.LightningFlash => 1500,
                AudioEvent.SniperShot => 350,
                AudioEvent.GatoMove => 450,
                AudioEvent.RatoMove => 650,
                AudioEvent.PlayerMove => 550,
                AudioEvent.MeteoroSpawn => 300,
                
                // Efeitos
                AudioEvent.MatrixSound => 400,
                AudioEvent.PulseSound => 600,
                AudioEvent.Fireworks => 800,
                AudioEvent.DemoMusic => 500,
                
                // Default
                _ => 500
            };
        }
        
        private int GetDurationForEvent(AudioEvent audioEvent)
        {
            return audioEvent switch
            {
                // Sons muito curtos
                AudioEvent.ButtonClick or AudioEvent.ButtonHover => 100,
                AudioEvent.GatoMove or AudioEvent.RatoMove or AudioEvent.PlayerMove => 80,
                AudioEvent.LightningFlash => 50,
                
                // Sons curtos
                AudioEvent.ScoreHit or AudioEvent.PegaLuzHit => 150,
                
                // Sons médios
                AudioEvent.LoginSuccess or AudioEvent.LoginError => 300,
                AudioEvent.Error or AudioEvent.GuitarNote => 250,
                AudioEvent.SequenciaCorrect or AudioEvent.SequenciaWrong => 200,
                AudioEvent.SniperShot or AudioEvent.SniperHit or AudioEvent.SniperMiss => 180,
                AudioEvent.LightningCorrect or AudioEvent.LightningWrong => 220,
                
                // Sons longos
                AudioEvent.GameStart or AudioEvent.Victory => 500,
                AudioEvent.LevelUp or AudioEvent.GameOver => 400,
                AudioEvent.GatoCapture => 350,
                AudioEvent.Notification => 300,
                
                // Sons muito longos
                AudioEvent.MeteoroExplosion => 600,
                AudioEvent.Startup => 1000,
                AudioEvent.Fireworks => 800,
                AudioEvent.DemoMusic => 1500,
                
                // Efeitos contínuos (simulados)
                AudioEvent.MatrixSound or AudioEvent.PulseSound => 400,
                
                // Default
                _ => 200
            };
        }
        
        public async Task PlaySoundSequenceAsync(AudioEvent[] events, int delayMs = 100)
        {
            foreach (var audioEvent in events)
            {
                await PlaySoundAsync(audioEvent);
                if (delayMs > 0)
                    await Task.Delay(delayMs);
            }
        }
        
        /// <summary>
        /// Starts playing background music continuously
        /// </summary>
        public async Task StartBackgroundMusicAsync()
        {
            try
            {
                // Stop any existing background music
                await StopBackgroundMusicAsync();
                
                _backgroundMusicPlaying = true;
                _backgroundMusicCancellationTokenSource = new CancellationTokenSource();
                
                Console.WriteLine("🎵 Iniciando playlist de música de fundo (Moog City 2, Aria Math, Sweden)...");
                System.Diagnostics.Debug.WriteLine("🎵 Iniciando playlist de música de fundo (Moog City 2, Aria Math, Sweden)...");
                
                _backgroundMusicTask = Task.Run(async () =>
                {
                    var cancellationToken = _backgroundMusicCancellationTokenSource.Token;
                    
                    while (!cancellationToken.IsCancellationRequested && _backgroundMusicPlaying)
                    {
                        try
                        {
                            if (_isEnabled) // Only play if audio is enabled
                            {
                                await PlayBackgroundMusicFile();
                            }
                            else
                            {
                                // If audio is disabled, wait a bit before checking again
                                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when cancellation is requested
                            break;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Erro na música de fundo: {ex.Message}");
                            Console.WriteLine($"⚠️ Erro na música de fundo: {ex.Message}");
                            
                            // Wait a bit before retrying to avoid rapid failures
                            await Task.Delay(5000, cancellationToken);
                        }
                    }
                }, _backgroundMusicCancellationTokenSource.Token);
                
                System.Diagnostics.Debug.WriteLine("✅ Música de fundo iniciada");
                Console.WriteLine("✅ Música de fundo iniciada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao iniciar música de fundo: {ex.Message}");
                Console.WriteLine($"⚠️ Erro ao iniciar música de fundo: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Stops the background music
        /// </summary>
        public async Task StopBackgroundMusicAsync()
        {
            try
            {
                _backgroundMusicPlaying = false;
                
                if (_backgroundMusicCancellationTokenSource != null)
                {
                    _backgroundMusicCancellationTokenSource.Cancel();
                    
                    if (_backgroundMusicTask != null)
                    {
                        await _backgroundMusicTask;
                        _backgroundMusicTask = null;
                    }
                    
                    _backgroundMusicCancellationTokenSource.Dispose();
                    _backgroundMusicCancellationTokenSource = null;
                }
                
                System.Diagnostics.Debug.WriteLine("🔇 Música de fundo parada");
                Console.WriteLine("🔇 Música de fundo parada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao parar música de fundo: {ex.Message}");
                Console.WriteLine($"⚠️ Erro ao parar música de fundo: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Plays the background music files in a playlist (Moog City 2, Aria Math, Sweden)
        /// </summary>
        private async Task PlayBackgroundMusicFile()
        {
            try
            {
                var playlist = new[]
                {
                    "Assets/Audio/Ambiente/C418 - Moog City 2.mp3",
                    "Assets/Audio/Ambiente/C418 - Aria Math.mp3",
                    "Assets/Audio/Ambiente/C418 - Sweden.mp3"
                };
                
                // Verify files exist
                var validFiles = playlist.Where(File.Exists).ToArray();
                
                if (validFiles.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Nenhum arquivo de música encontrado na playlist");
                    Console.WriteLine("⚠️ Nenhum arquivo de música encontrado na playlist");
                    return;
                }
                
                Console.WriteLine($"🎵 Playlist carregada: {validFiles.Length} música(s) - {string.Join(", ", validFiles.Select(Path.GetFileNameWithoutExtension))}");
                
                if (_isLinux)
                {
                    await PlayLinuxBackgroundMusic(validFiles);
                }
                else if (_isWindows)
                {
                    await PlayWindowsBackgroundMusic(validFiles);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Erro ao reproduzir playlist de música de fundo: {ex.Message}");
                Console.WriteLine($"⚠️ Erro ao reproduzir playlist de música de fundo: {ex.Message}");
            }
        }

        private async Task PlayLinuxBackgroundMusic(string[] validFiles)
        {
            try
            {
                // Build command to play all files in infinite loop
                var fileList = string.Join(" ", validFiles.Select(f => $"\"{f}\""));
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "mpg123",
                    Arguments = $"--loop -1 -q {fileList}", // Loop all files infinitely
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                Console.WriteLine($"🔧 Comando: mpg123 --loop -1 -q {fileList}");
                
                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process != null)
                {
                    Console.WriteLine($"🎵 Música ambiente INICIADA (PID: {process.Id})");
                    Console.WriteLine($"🎶 Tocando em loop: {string.Join(", ", validFiles.Select(Path.GetFileNameWithoutExtension))}");
                    Console.WriteLine("🔊 Se você não ouvir nada, verifique o volume do sistema!");
                    
                    var cancellationToken = _backgroundMusicCancellationTokenSource?.Token ?? CancellationToken.None;
                    
                    // Give the process a moment to start playing
                    await Task.Delay(1000);
                    
                    if (process.HasExited)
                    {
                        Console.WriteLine($"❌ mpg123 terminou imediatamente! Código de saída: {process.ExitCode}");
                        var error = await process.StandardError.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(error)) 
                            Console.WriteLine($"Erro: {error}");
                        return;
                    }
                    
                    Console.WriteLine("✅ mpg123 está rodando e deve estar tocando música agora!");
                    
                    try
                    {
                        // Wait for the process to finish or be cancelled
                        await process.WaitForExitAsync(cancellationToken);
                        Console.WriteLine("🎵 Processo mpg123 finalizou naturalmente");
                    }
                    catch (OperationCanceledException)
                    {
                        // Kill the process if cancellation is requested
                        if (!process.HasExited)
                        {
                            Console.WriteLine("🔇 PARANDO música ambiente...");
                            process.Kill(true);
                            // Wait a moment for clean shutdown
                            await Task.Delay(500);
                            Console.WriteLine("✅ Música ambiente PARADA com sucesso");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("❌ Falha ao criar processo mpg123");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro na reprodução Linux: {ex.Message}");
            }
        }

        private async Task PlayWindowsBackgroundMusic(string[] validFiles)
        {
            // For Windows, loop through the playlist continuously
            while (_backgroundMusicPlaying && !(_backgroundMusicCancellationTokenSource?.Token.IsCancellationRequested ?? true))
            {
                foreach (var filePath in validFiles)
                {
                    if (!_backgroundMusicPlaying || (_backgroundMusicCancellationTokenSource?.Token.IsCancellationRequested ?? true))
                        break;
                    
                    Console.WriteLine($"🎵 Reproduzindo: {Path.GetFileNameWithoutExtension(filePath)}");
                    
                    try
                    {
                        // Use Windows Media Player COM object for better control
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell",
                            Arguments = $@"-Command ""
                                Add-Type -AssemblyName presentationCore;
                                $player = New-Object System.Windows.Media.MediaPlayer;
                                $player.Open([uri]'{filePath.Replace("'", "''")}');
                                $player.Play();
                                
                                # Wait for the file to finish or be cancelled
                                $timeout = 600; # 10 minutes max per track
                                $elapsed = 0;
                                while ($player.Position -lt $player.NaturalDuration.TimeSpan -and $elapsed -lt $timeout) {{
                                    Start-Sleep -Milliseconds 500;
                                    $elapsed += 0.5;
                                }}
                                
                                $player.Stop();
                                $player.Close();
                            """,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        
                        using var process = System.Diagnostics.Process.Start(startInfo);
                        if (process != null)
                        {
                            var cancellationToken = _backgroundMusicCancellationTokenSource?.Token ?? CancellationToken.None;
                            await process.WaitForExitAsync(cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("🔇 Reprodução de música cancelada");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Erro ao reproduzir {Path.GetFileName(filePath)}: {ex.Message}");
                        // Continue to next track
                    }
                    
                    // Small delay between tracks
                    if (_backgroundMusicPlaying && !(_backgroundMusicCancellationTokenSource?.Token.IsCancellationRequested ?? true))
                    {
                        await Task.Delay(1000, _backgroundMusicCancellationTokenSource?.Token ?? CancellationToken.None);
                    }
                }
            }
        }
        
        public void StopAllSounds()
        {
            // Stop background music
            _ = Task.Run(async () => await StopBackgroundMusicAsync());
            
            System.Diagnostics.Debug.WriteLine("🔇 StopAllSounds chamado");
        }
        
        public void SetMasterVolume(float volume)
        {
            MasterVolume = volume;
            System.Diagnostics.Debug.WriteLine($"🔊 Volume master definido para: {volume:P0}");
        }
        
        public void SetCategoryVolume(string category, float volume)
        {
            // SimpleAudioService não suporta volume por categoria
            // mas mantém a interface para compatibilidade
            System.Diagnostics.Debug.WriteLine($"🔊 Volume da categoria '{category}' definido para: {volume:P0} (simulado)");
        }
        
        public void Dispose()
        {
            // Stop background music before disposing
            _ = Task.Run(async () => await StopBackgroundMusicAsync());
            
            StopAllSounds();
            System.Diagnostics.Debug.WriteLine("🎵 AudioService disposed");
        }
    }
}