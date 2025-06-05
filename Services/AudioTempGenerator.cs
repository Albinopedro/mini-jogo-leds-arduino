using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace miniJogo.Services
{
    /// <summary>
    /// Gerador temporário de arquivos de áudio para desenvolvimento.
    /// Cria arquivos WAV de silêncio ou tons simples quando os arquivos reais não estão disponíveis.
    /// </summary>
    public static class AudioTempGenerator
    {
        /// <summary>
        /// Gera todos os arquivos de áudio temporários necessários para o desenvolvimento
        /// </summary>
        public static async Task GenerateAllTempAudioFilesAsync()
        {
            var audioDefinitions = new[]
            {
                // Sistema
                new AudioDef("Assets/Audio/Sistema/startup.wav", ToneType.Rising, 1.0f),
                new AudioDef("Assets/Audio/Sistema/login_success.wav", ToneType.Success, 0.5f),
                new AudioDef("Assets/Audio/Sistema/login_error.wav", ToneType.Error, 0.5f),
                new AudioDef("Assets/Audio/Sistema/button_click.wav", ToneType.Click, 0.1f),
                new AudioDef("Assets/Audio/Sistema/button_hover.wav", ToneType.Hover, 0.1f),
                new AudioDef("Assets/Audio/Sistema/notification.wav", ToneType.Notification, 0.3f),
                new AudioDef("Assets/Audio/Sistema/shutdown.wav", ToneType.Falling, 1.0f),
                
                // Jogos
                new AudioDef("Assets/Audio/Jogos/game_start.wav", ToneType.Fanfare, 2.0f),
                new AudioDef("Assets/Audio/Jogos/game_over.wav", ToneType.GameOver, 2.0f),
                new AudioDef("Assets/Audio/Jogos/victory.wav", ToneType.Victory, 3.0f),
                new AudioDef("Assets/Audio/Jogos/level_up.wav", ToneType.LevelUp, 1.5f),
                new AudioDef("Assets/Audio/Jogos/score_hit.wav", ToneType.Hit, 0.2f),
                new AudioDef("Assets/Audio/Jogos/error_sound.wav", ToneType.Error, 0.5f),
                new AudioDef("Assets/Audio/Jogos/countdown.wav", ToneType.Countdown, 0.5f),
                
                // Específicos
                new AudioDef("Assets/Audio/Específicos/pega_luz_hit.wav", ToneType.Hit, 0.2f),
                new AudioDef("Assets/Audio/Específicos/sequencia_correct.wav", ToneType.Success, 0.3f),
                new AudioDef("Assets/Audio/Específicos/gato_capture.wav", ToneType.Capture, 0.4f),
                new AudioDef("Assets/Audio/Específicos/meteoro_explosion.wav", ToneType.Explosion, 0.6f),
                new AudioDef("Assets/Audio/Específicos/guitar_note.wav", ToneType.Note, 0.3f),
                new AudioDef("Assets/Audio/Específicos/roleta_tick.wav", ToneType.Tick, 0.1f),
                new AudioDef("Assets/Audio/Específicos/lightning_flash.wav", ToneType.Lightning, 0.2f),
                new AudioDef("Assets/Audio/Específicos/sniper_shot.wav", ToneType.Shot, 0.3f),
                
                // Efeitos
                new AudioDef("Assets/Audio/Efeitos/matrix_sound.wav", ToneType.Matrix, 2.0f),
                new AudioDef("Assets/Audio/Efeitos/pulse_sound.wav", ToneType.Pulse, 1.0f),
                new AudioDef("Assets/Audio/Efeitos/fireworks.wav", ToneType.Fireworks, 3.0f),
                new AudioDef("Assets/Audio/Efeitos/demo_music.wav", ToneType.Demo, 5.0f)
            };

            foreach (var audio in audioDefinitions)
            {
                try
                {
                    await GenerateAudioFileAsync(audio.FilePath, audio.Type, audio.Duration);
                    Console.WriteLine($"✅ Gerado: {audio.FilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao gerar {audio.FilePath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gera um arquivo de áudio baseado no tipo especificado
        /// </summary>
        public static async Task GenerateAudioFileAsync(string filePath, ToneType toneType, float duration)
        {
            // Criar diretório se não existir
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            const int sampleRate = 44100;
            const int channels = 1; // Mono para economia de espaço
            var samples = (int)(sampleRate * duration);

            using var writer = new WaveFileWriter(filePath, new WaveFormat(sampleRate, 16, channels));
            
            await Task.Run(() =>
            {
                var buffer = new short[samples];
                GenerateTone(buffer, sampleRate, toneType, duration);
                
                // Converter para bytes e escrever
                var byteBuffer = new byte[buffer.Length * 2];
                Buffer.BlockCopy(buffer, 0, byteBuffer, 0, byteBuffer.Length);
                writer.Write(byteBuffer, 0, byteBuffer.Length);
            });
        }

        private static void GenerateTone(short[] buffer, int sampleRate, ToneType toneType, float duration)
        {
            var samples = buffer.Length;
            
            switch (toneType)
            {
                case ToneType.Silence:
                    // Buffer já é zero por padrão
                    break;
                    
                case ToneType.Click:
                    GenerateClick(buffer, sampleRate);
                    break;
                    
                case ToneType.Hover:
                    GenerateSimpleTone(buffer, sampleRate, 800, 0.1f);
                    break;
                    
                case ToneType.Success:
                    GenerateChord(buffer, sampleRate, new double[] { 523, 659, 784 }, 0.3f); // C-E-G
                    break;
                    
                case ToneType.Error:
                    GenerateSimpleTone(buffer, sampleRate, 200, 0.5f);
                    break;
                    
                case ToneType.Hit:
                    GenerateHit(buffer, sampleRate);
                    break;
                    
                case ToneType.Notification:
                    GenerateNotification(buffer, sampleRate);
                    break;
                    
                case ToneType.Rising:
                    GenerateSweep(buffer, sampleRate, 200, 800);
                    break;
                    
                case ToneType.Falling:
                    GenerateSweep(buffer, sampleRate, 800, 200);
                    break;
                    
                case ToneType.Fanfare:
                    GenerateFanfare(buffer, sampleRate);
                    break;
                    
                case ToneType.GameOver:
                    GenerateGameOver(buffer, sampleRate);
                    break;
                    
                case ToneType.Victory:
                    GenerateVictory(buffer, sampleRate);
                    break;
                    
                case ToneType.LevelUp:
                    GenerateLevelUp(buffer, sampleRate);
                    break;
                    
                case ToneType.Countdown:
                    GenerateCountdown(buffer, sampleRate);
                    break;
                    
                case ToneType.Capture:
                    GenerateCapture(buffer, sampleRate);
                    break;
                    
                case ToneType.Explosion:
                    GenerateExplosion(buffer, sampleRate);
                    break;
                    
                case ToneType.Note:
                    GenerateSimpleTone(buffer, sampleRate, 440, 0.5f); // A4
                    break;
                    
                case ToneType.Tick:
                    GenerateTick(buffer, sampleRate);
                    break;
                    
                case ToneType.Lightning:
                    GenerateLightning(buffer, sampleRate);
                    break;
                    
                case ToneType.Shot:
                    GenerateShot(buffer, sampleRate);
                    break;
                    
                case ToneType.Matrix:
                    GenerateMatrix(buffer, sampleRate);
                    break;
                    
                case ToneType.Pulse:
                    GeneratePulse(buffer, sampleRate);
                    break;
                    
                case ToneType.Fireworks:
                    GenerateFireworks(buffer, sampleRate);
                    break;
                    
                case ToneType.Demo:
                    GenerateDemo(buffer, sampleRate);
                    break;
            }
        }

        private static void GenerateSimpleTone(short[] buffer, int sampleRate, double frequency, float amplitude)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var time = (double)i / sampleRate;
                var envelope = Math.Exp(-time * 3); // Decay exponencial
                var sample = amplitude * envelope * Math.Sin(2 * Math.PI * frequency * time);
                buffer[i] = (short)(sample * short.MaxValue * 0.5);
            }
        }

        private static void GenerateClick(short[] buffer, int sampleRate)
        {
            // Som de clique curto e nítido
            var clickLength = Math.Min(buffer.Length, sampleRate / 100); // 10ms
            for (int i = 0; i < clickLength; i++)
            {
                var envelope = 1.0 - (double)i / clickLength;
                var noise = (Random.Shared.NextDouble() - 0.5) * 2;
                buffer[i] = (short)(noise * envelope * short.MaxValue * 0.3);
            }
        }

        private static void GenerateChord(short[] buffer, int sampleRate, double[] frequencies, float amplitude)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var time = (double)i / sampleRate;
                var envelope = Math.Exp(-time * 2);
                double sample = 0;
                
                foreach (var freq in frequencies)
                {
                    sample += Math.Sin(2 * Math.PI * freq * time);
                }
                
                sample = sample / frequencies.Length * amplitude * envelope;
                buffer[i] = (short)(sample * short.MaxValue * 0.3);
            }
        }

        private static void GenerateHit(short[] buffer, int sampleRate)
        {
            // Som de impacto - ruído breve com envelope rápido
            var hitLength = Math.Min(buffer.Length, sampleRate / 20); // 50ms
            for (int i = 0; i < hitLength; i++)
            {
                var envelope = Math.Exp(-i * 0.001);
                var tone = Math.Sin(2 * Math.PI * 1000 * i / sampleRate);
                var noise = (Random.Shared.NextDouble() - 0.5) * 0.5;
                buffer[i] = (short)((tone + noise) * envelope * short.MaxValue * 0.4);
            }
        }

        private static void GenerateNotification(short[] buffer, int sampleRate)
        {
            // Duas notas rápidas
            var noteLength = buffer.Length / 2;
            GenerateSimpleTone(buffer.AsSpan(0, noteLength).ToArray(), sampleRate, 800, 0.3f);
            GenerateSimpleTone(buffer.AsSpan(noteLength).ToArray(), sampleRate, 1000, 0.3f);
        }

        private static void GenerateSweep(short[] buffer, int sampleRate, double startFreq, double endFreq)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var time = (double)i / sampleRate;
                var progress = (double)i / buffer.Length;
                var frequency = startFreq + (endFreq - startFreq) * progress;
                var envelope = 1.0 - progress * 0.5; // Fade out gradual
                var sample = envelope * Math.Sin(2 * Math.PI * frequency * time);
                buffer[i] = (short)(sample * short.MaxValue * 0.3);
            }
        }

        private static void GenerateFanfare(short[] buffer, int sampleRate)
        {
            // Sequência de acordes ascendentes
            var chordLength = buffer.Length / 3;
            var chord1 = new double[] { 261, 329, 392 }; // C-E-G
            var chord2 = new double[] { 293, 369, 440 }; // D-F#-A
            var chord3 = new double[] { 329, 415, 493 }; // E-G#-B
            
            GenerateChord(buffer.AsSpan(0, chordLength).ToArray(), sampleRate, chord1, 0.4f);
            GenerateChord(buffer.AsSpan(chordLength, chordLength).ToArray(), sampleRate, chord2, 0.4f);
            GenerateChord(buffer.AsSpan(chordLength * 2).ToArray(), sampleRate, chord3, 0.5f);
        }

        private static void GenerateGameOver(short[] buffer, int sampleRate)
        {
            // Tom descendente dramático
            GenerateSweep(buffer, sampleRate, 400, 100);
        }

        private static void GenerateVictory(short[] buffer, int sampleRate)
        {
            // Melodia vitoriosa ascendente
            var noteLength = buffer.Length / 5;
            var notes = new double[] { 523, 587, 659, 698, 784 }; // C-D-E-F-G
            
            for (int i = 0; i < notes.Length && i * noteLength < buffer.Length; i++)
            {
                var start = i * noteLength;
                var length = Math.Min(noteLength, buffer.Length - start);
                var noteBuffer = new short[length];
                GenerateSimpleTone(noteBuffer, sampleRate, notes[i], 0.6f);
                Array.Copy(noteBuffer, 0, buffer, start, length);
            }
        }

        private static void GenerateLevelUp(short[] buffer, int sampleRate)
        {
            // Arpejo ascendente
            GenerateSweep(buffer, sampleRate, 261, 1046); // C4 to C6
        }

        private static void GenerateCountdown(short[] buffer, int sampleRate)
        {
            // Tom metronômico
            GenerateSimpleTone(buffer, sampleRate, 880, 0.8f);
        }

        private static void GenerateCapture(short[] buffer, int sampleRate)
        {
            // Som de captura - sweep rápido
            GenerateSweep(buffer, sampleRate, 500, 1500);
        }

        private static void GenerateExplosion(short[] buffer, int sampleRate)
        {
            // Explosão - ruído com envelope
            for (int i = 0; i < buffer.Length; i++)
            {
                var envelope = Math.Exp(-i * 0.00001);
                var noise = (Random.Shared.NextDouble() - 0.5) * 2;
                buffer[i] = (short)(noise * envelope * short.MaxValue * 0.7);
            }
        }

        private static void GenerateTick(short[] buffer, int sampleRate)
        {
            // Clique metálico breve
            var tickLength = Math.Min(buffer.Length, sampleRate / 50); // 20ms
            for (int i = 0; i < tickLength; i++)
            {
                var envelope = 1.0 - (double)i / tickLength;
                var tone = Math.Sin(2 * Math.PI * 2000 * i / sampleRate);
                buffer[i] = (short)(tone * envelope * short.MaxValue * 0.2);
            }
        }

        private static void GenerateLightning(short[] buffer, int sampleRate)
        {
            // Estalo de raio
            GenerateClick(buffer, sampleRate);
            // Adicionar reverb simulado
            for (int i = sampleRate / 100; i < buffer.Length; i++)
            {
                buffer[i] = (short)(buffer[i] + buffer[i - sampleRate / 100] * 0.3);
            }
        }

        private static void GenerateShot(short[] buffer, int sampleRate)
        {
            // Som de tiro - pop seguido de eco
            var popLength = sampleRate / 50; // 20ms
            GenerateClick(buffer.AsSpan(0, Math.Min(popLength, buffer.Length)).ToArray(), sampleRate);
        }

        private static void GenerateMatrix(short[] buffer, int sampleRate)
        {
            // Efeito digital - tons modulados
            for (int i = 0; i < buffer.Length; i++)
            {
                var time = (double)i / sampleRate;
                var mod = Math.Sin(2 * Math.PI * 5 * time); // Modulação 5Hz
                var carrier = Math.Sin(2 * Math.PI * (440 + mod * 100) * time);
                var envelope = Math.Exp(-time * 0.5);
                buffer[i] = (short)(carrier * envelope * short.MaxValue * 0.3);
            }
        }

        private static void GeneratePulse(short[] buffer, int sampleRate)
        {
            // Pulso rítmico
            var pulseRate = 2.0; // 2 Hz
            for (int i = 0; i < buffer.Length; i++)
            {
                var time = (double)i / sampleRate;
                var pulse = Math.Sin(2 * Math.PI * pulseRate * time);
                if (pulse > 0)
                {
                    var tone = Math.Sin(2 * Math.PI * 220 * time);
                    buffer[i] = (short)(tone * pulse * short.MaxValue * 0.4);
                }
            }
        }

        private static void GenerateFireworks(short[] buffer, int sampleRate)
        {
            // Múltiplas explosões pequenas
            var explosionCount = 5;
            var explosionLength = buffer.Length / explosionCount;
            
            for (int exp = 0; exp < explosionCount; exp++)
            {
                var start = exp * explosionLength;
                var length = Math.Min(explosionLength, buffer.Length - start);
                
                for (int i = 0; i < length / 4; i++) // Explosões mais curtas
                {
                    var envelope = Math.Exp(-i * 0.0001);
                    var noise = (Random.Shared.NextDouble() - 0.5) * 2;
                    var index = start + i;
                    if (index < buffer.Length)
                        buffer[index] = (short)(noise * envelope * short.MaxValue * 0.5);
                }
            }
        }

        private static void GenerateDemo(short[] buffer, int sampleRate)
        {
            // Melodia simples demonstrativa
            var notes = new double[] { 261, 293, 329, 349, 392, 440, 493, 523 }; // Escala C
            var noteLength = buffer.Length / notes.Length;
            
            for (int i = 0; i < notes.Length; i++)
            {
                var start = i * noteLength;
                var length = Math.Min(noteLength, buffer.Length - start);
                var noteBuffer = new short[length];
                GenerateSimpleTone(noteBuffer, sampleRate, notes[i], 0.5f);
                Array.Copy(noteBuffer, 0, buffer, start, length);
            }
        }

        private record AudioDef(string FilePath, ToneType Type, float Duration);
    }

    public enum ToneType
    {
        Silence,
        Click,
        Hover,
        Success,
        Error,
        Hit,
        Notification,
        Rising,
        Falling,
        Fanfare,
        GameOver,
        Victory,
        LevelUp,
        Countdown,
        Capture,
        Explosion,
        Note,
        Tick,
        Lightning,
        Shot,
        Matrix,
        Pulse,
        Fireworks,
        Demo
    }
}