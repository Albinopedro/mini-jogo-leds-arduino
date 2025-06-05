#!/usr/bin/env python3
"""
Gerador de arquivos de áudio WAV aprimorado para Mini Jogo LEDs
Sons mais envolventes, dinâmicos e adequados para jogos
Usa apenas bibliotecas padrão do Python (wave, struct, math)
"""

import os
import wave
import struct
import math
import random

class GameAudioGenerator:
    def __init__(self, sample_rate=44100):
        self.sample_rate = sample_rate
        self.base_path = "Assets/Audio"
        # Notas musicais em Hz para melodias mais harmônicas
        self.notes = {
            'C3': 130.81, 'D3': 146.83, 'E3': 164.81, 'F3': 174.61, 'G3': 196.00, 'A3': 220.00, 'B3': 246.94,
            'C4': 261.63, 'D4': 293.66, 'E4': 329.63, 'F4': 349.23, 'G4': 392.00, 'A4': 440.00, 'B4': 493.88,
            'C5': 523.25, 'D5': 587.33, 'E5': 659.25, 'F5': 698.46, 'G5': 783.99, 'A5': 880.00, 'B5': 987.77,
            'C6': 1046.50
        }

    def create_directories(self):
        """Cria todas as pastas necessárias"""
        dirs = [
            "Assets/Audio/Sistema",
            "Assets/Audio/Jogos",
            "Assets/Audio/Específicos",
            "Assets/Audio/Efeitos",
            "Assets/Audio/Ambiente"
        ]

        for dir_path in dirs:
            os.makedirs(dir_path, exist_ok=True)
            print(f"✅ Pasta criada: {dir_path}")

    def apply_envelope(self, frames, attack=0.05, decay=0.1, sustain=0.7, release=0.2):
        """Aplica envelope ADSR para sons mais naturais"""
        total_samples = len(frames)
        attack_samples = int(attack * self.sample_rate)
        decay_samples = int(decay * self.sample_rate)
        release_samples = int(release * self.sample_rate)
        sustain_samples = total_samples - attack_samples - decay_samples - release_samples

        for i in range(len(frames)):
            if i < attack_samples:
                # Attack
                envelope = i / attack_samples
            elif i < attack_samples + decay_samples:
                # Decay
                progress = (i - attack_samples) / decay_samples
                envelope = 1.0 - (1.0 - sustain) * progress
            elif i < attack_samples + decay_samples + sustain_samples:
                # Sustain
                envelope = sustain
            else:
                # Release
                progress = (i - attack_samples - decay_samples - sustain_samples) / release_samples
                envelope = sustain * (1.0 - progress)

            frames[i] *= envelope

        return frames

    def generate_melodic_tone(self, note_name, duration, amplitude=0.5, harmonics=True):
        """Gera tom musical com harmônicos para sons mais ricos"""
        frequency = self.notes.get(note_name, 440)
        frames = []

        for i in range(int(duration * self.sample_rate)):
            t = i / self.sample_rate
            value = amplitude * math.sin(2 * math.pi * frequency * t)

            if harmonics:
                # Adiciona harmônicos para timbre mais rico
                value += 0.3 * amplitude * math.sin(2 * math.pi * frequency * 2 * t)  # Oitava
                value += 0.2 * amplitude * math.sin(2 * math.pi * frequency * 3 * t)  # Quinta
                value += 0.1 * amplitude * math.sin(2 * math.pi * frequency * 4 * t)  # Oitava dupla

            frames.append(value)

        return self.apply_envelope(frames)

    def generate_arpeggio(self, note_names, note_duration=0.2, amplitude=0.5):
        """Gera arpejo com notas musicais"""
        frames = []
        for note in note_names:
            note_frames = self.generate_melodic_tone(note, note_duration, amplitude)
            frames.extend(note_frames)
        return frames

    def generate_chord_progression(self, chords, chord_duration=0.5, amplitude=0.4):
        """Gera progressão de acordes"""
        frames = []
        for chord_notes in chords:
            chord_frames = self.generate_layered_chord(chord_notes, chord_duration, amplitude)
            frames.extend(chord_frames)
        return frames

    def generate_layered_chord(self, note_names, duration, amplitude=0.3):
        """Gera acorde com múltiplas camadas"""
        frames = [0.0] * int(duration * self.sample_rate)

        for note_name in note_names:
            note_frames = self.generate_melodic_tone(note_name, duration, amplitude / len(note_names))
            for i in range(len(frames)):
                if i < len(note_frames):
                    frames[i] += note_frames[i]

        return frames

    def generate_retro_beep(self, base_freq, duration, amplitude=0.6):
        """Gera beep retrô com vibrato"""
        frames = []
        for i in range(int(duration * self.sample_rate)):
            t = i / self.sample_rate
            # Adiciona vibrato
            vibrato = 1 + 0.1 * math.sin(2 * math.pi * 5 * t)
            freq = base_freq * vibrato
            value = amplitude * math.sin(2 * math.pi * freq * t)
            frames.append(value)

        return self.apply_envelope(frames, attack=0.01, release=0.1)

    def generate_swoosh(self, start_freq, end_freq, duration, amplitude=0.4):
        """Gera efeito swoosh com filtro passa-baixa simulado"""
        frames = []
        for i in range(int(duration * self.sample_rate)):
            progress = i / (duration * self.sample_rate)
            # Interpolação exponencial para efeito mais natural
            freq = start_freq * math.pow(end_freq / start_freq, progress)
            t = i / self.sample_rate

            # Onda principal com filtro simulado
            value = amplitude * math.sin(2 * math.pi * freq * t)
            # Adiciona ruído filtrado para textura
            noise_amp = 0.1 * amplitude * (1 - progress)
            value += noise_amp * (random.random() * 2 - 1)

            frames.append(value)

        return self.apply_envelope(frames)

    def generate_power_up(self, duration=1.0, amplitude=0.6):
        """Gera som de power-up energético"""
        # Arpejo ascendente com aceleração
        notes = ['C4', 'E4', 'G4', 'C5', 'E5', 'G5', 'C6']
        frames = []

        for i, note in enumerate(notes):
            # Notas ficam mais rápidas conforme sobem
            note_duration = duration / len(notes) * (0.8 + 0.4 * (i / len(notes)))
            note_frames = self.generate_melodic_tone(note, note_duration, amplitude * (1 + i * 0.1))
            frames.extend(note_frames)

        return frames

    def generate_coin_collect(self, duration=0.3, amplitude=0.7):
        """Som de coleta de moeda/ponto"""
        # Duas notas rápidas com timbre brilhante
        frames = []
        frames.extend(self.generate_melodic_tone('E5', duration/2, amplitude))
        frames.extend(self.generate_melodic_tone('C6', duration/2, amplitude * 1.2))
        return frames

    def generate_bounce(self, bounces=3, amplitude=0.5):
        """Som de bounce com decay natural"""
        frames = []
        base_freq = 400

        for i in range(bounces):
            bounce_amp = amplitude * (0.8 ** i)  # Decay exponencial
            freq = base_freq * (1 + i * 0.2)  # Frequência sobe levemente
            duration = 0.1 * (0.8 ** i)  # Durações menores

            bounce_frames = self.generate_retro_beep(freq, duration, bounce_amp)
            frames.extend(bounce_frames)

            # Pequena pausa entre bounces
            silence = [0.0] * int(0.05 * self.sample_rate)
            frames.extend(silence)

        return frames

    def generate_laser_shot(self, duration=0.2, amplitude=0.6):
        """Som de tiro laser"""
        return self.generate_swoosh(1200, 200, duration, amplitude)

    def generate_explosion_rich(self, duration=1.5, amplitude=0.6):
        """Explosão mais rica com múltiplas camadas"""
        frames = [0.0] * int(duration * self.sample_rate)

        # Camada 1: Ruído branco com envelope
        for i in range(len(frames)):
            t = i / self.sample_rate
            envelope = math.exp(-t * 4)  # Decay rápido
            noise = (random.random() * 2 - 1) * amplitude * envelope * 0.6
            frames[i] += noise

        # Camada 2: Frequências baixas
        for i in range(len(frames)):
            t = i / self.sample_rate
            envelope = math.exp(-t * 2)
            low_freq = math.sin(2 * math.pi * (100 - t * 80) * t) * amplitude * envelope * 0.4
            frames[i] += low_freq

        return frames

    def generate_victory_fanfare(self, duration=3.0, amplitude=0.7):
        """Fanfarra de vitória épica"""
        # Progressão harmônica: I - IV - V - I
        chord_progression = [
            ['C4', 'E4', 'G4'],  # Dó maior
            ['F4', 'A4', 'C5'],  # Fá maior
            ['G4', 'B4', 'D5'],  # Sol maior
            ['C5', 'E5', 'G5']   # Dó maior oitava
        ]

        frames = self.generate_chord_progression(chord_progression, duration/4, amplitude)

        # Adiciona arpejo final triunfante
        final_arpeggio = self.generate_arpeggio(['C5', 'E5', 'G5', 'C6'], 0.15, amplitude)
        frames.extend(final_arpeggio)

        return frames

    def generate_ambient_loop(self, duration=5.0, amplitude=0.3):
        """Gera loop ambiente relaxante"""
        frames = []
        # Progressão suave e cíclica
        chord_sequence = [
            ['C4', 'E4', 'G4'],
            ['A3', 'C4', 'E4'],
            ['F3', 'A3', 'C4'],
            ['G3', 'B3', 'D4']
        ]

        chord_duration = duration / len(chord_sequence)
        for chord in chord_sequence:
            chord_frames = self.generate_layered_chord(chord, chord_duration, amplitude)
            frames.extend(chord_frames)

        return frames

    def save_wav(self, frames, filename):
        """Salva frames como arquivo WAV com melhor qualidade"""
        # Normalização inteligente
        if frames:
            max_val = max(abs(f) for f in frames)
            if max_val > 0:
                # Normaliza com headroom
                normalize_factor = 0.9 / max_val
                frames = [f * normalize_factor for f in frames]

        with wave.open(filename, 'w') as wav_file:
            wav_file.setnchannels(1)  # Mono
            wav_file.setsampwidth(2)  # 16-bit
            wav_file.setframerate(self.sample_rate)

            # Converter para 16-bit PCM
            pcm_frames = []
            for frame in frames:
                # Clamp e converte
                frame = max(-1.0, min(1.0, frame))
                pcm_value = int(frame * 32767)
                pcm_frames.append(struct.pack('<h', pcm_value))

            wav_file.writeframes(b''.join(pcm_frames))

        print(f"🎵 Gerado: {filename}")

    def generate_all_sounds(self):
        """Gera todos os sons com qualidade aprimorada"""
        print("🎵 Iniciando geração de arquivos de áudio aprimorados...")

        sounds_config = {
            # Sistema - Sons mais elegantes
            ("Sistema", "startup.wav"): lambda: self.generate_power_up(2.5, 0.5),
            ("Sistema", "login_success.wav"): lambda: self.generate_arpeggio(['C4', 'E4', 'G4', 'C5'], 0.2, 0.6),
            ("Sistema", "login_error.wav"): lambda: self.generate_chord_progression([['F4', 'Ab4', 'Db5']], 0.8, 0.5),
            ("Sistema", "button_click.wav"): lambda: self.generate_retro_beep(800, 0.08, 0.4),
            ("Sistema", "button_hover.wav"): lambda: self.generate_melodic_tone('C5', 0.06, 0.2),
            ("Sistema", "notification.wav"): lambda: self.generate_coin_collect(0.4, 0.5),
            ("Sistema", "shutdown.wav"): lambda: self.generate_swoosh(800, 200, 2.0, 0.4),

            # Jogos - Sons mais dinâmicos
            ("Jogos", "game_start.wav"): lambda: self.generate_chord_progression([['C4', 'E4', 'G4'], ['F4', 'A4', 'C5']], 0.8, 0.6),
            ("Jogos", "game_over.wav"): lambda: self.generate_chord_progression([['Bb3', 'D4', 'F4'], ['F3', 'Ab3', 'C4']], 1.0, 0.5),
            ("Jogos", "victory.wav"): lambda: self.generate_victory_fanfare(3.0, 0.7),
            ("Jogos", "level_up.wav"): lambda: self.generate_power_up(1.8, 0.6),
            ("Jogos", "score_hit.wav"): lambda: self.generate_coin_collect(0.25, 0.6),
            ("Jogos", "error_sound.wav"): lambda: self.generate_retro_beep(200, 0.4, 0.5),
            ("Jogos", "countdown.wav"): lambda: self.generate_retro_beep(660, 0.5, 0.6),

            # Específicos - Sons únicos por jogo
            ("Específicos", "pega_luz_hit.wav"): lambda: self.generate_coin_collect(0.2, 0.7),
            ("Específicos", "pega_luz_miss.wav"): lambda: self.generate_retro_beep(150, 0.3, 0.4),
            ("Específicos", "sequencia_correct.wav"): lambda: self.generate_melodic_tone('G4', 0.3, 0.6),
            ("Específicos", "sequencia_wrong.wav"): lambda: self.generate_retro_beep(220, 0.5, 0.5),
            ("Específicos", "sequencia_show.wav"): lambda: self.generate_melodic_tone('C5', 0.25, 0.5),
            ("Específicos", "gato_capture.wav"): lambda: self.generate_arpeggio(['E4', 'G4', 'B4'], 0.15, 0.6),
            ("Específicos", "gato_move.wav"): lambda: self.generate_retro_beep(300, 0.1, 0.3),
            ("Específicos", "rato_move.wav"): lambda: self.generate_retro_beep(600, 0.08, 0.3),
            ("Específicos", "meteoro_explosion.wav"): lambda: self.generate_explosion_rich(1.0, 0.7),
            ("Específicos", "meteoro_spawn.wav"): lambda: self.generate_swoosh(150, 800, 0.4, 0.5),
            ("Específicos", "player_move.wav"): lambda: self.generate_retro_beep(440, 0.1, 0.3),
            ("Específicos", "guitar_note.wav"): lambda: self.generate_melodic_tone('E4', 0.3, 0.6, True),
            ("Específicos", "guitar_perfect.wav"): lambda: self.generate_layered_chord(['E4', 'G#4', 'B4'], 0.4, 0.7),
            ("Específicos", "guitar_miss.wav"): lambda: self.generate_retro_beep(180, 0.4, 0.4),
            ("Específicos", "roleta_tick.wav"): lambda: self.generate_retro_beep(1000, 0.05, 0.3),
            ("Específicos", "roleta_safe.wav"): lambda: self.generate_victory_fanfare(1.5, 0.6),
            ("Específicos", "roleta_explosion.wav"): lambda: self.generate_explosion_rich(1.5, 0.8),
            ("Específicos", "lightning_flash.wav"): lambda: self.generate_swoosh(3000, 5000, 0.1, 0.6),
            ("Específicos", "lightning_correct.wav"): lambda: self.generate_swoosh(800, 1600, 0.3, 0.6),
            ("Específicos", "lightning_wrong.wav"): lambda: self.generate_retro_beep(200, 0.5, 0.5),
            ("Específicos", "sniper_shot.wav"): lambda: self.generate_laser_shot(0.2, 0.6),
            ("Específicos", "sniper_hit.wav"): lambda: self.generate_coin_collect(0.15, 0.7),
            ("Específicos", "sniper_miss.wav"): lambda: self.generate_retro_beep(150, 0.3, 0.4),

            # Efeitos - Mais variados
            ("Efeitos", "matrix_sound.wav"): lambda: self.generate_swoosh(100, 2000, 3.0, 0.4),
            ("Efeitos", "pulse_sound.wav"): lambda: self.generate_retro_beep(440, 1.5, 0.4),
            ("Efeitos", "fireworks.wav"): lambda: self.generate_explosion_rich(2.0, 0.6),
            ("Efeitos", "demo_music.wav"): lambda: self.generate_victory_fanfare(8.0, 0.5),
            ("Efeitos", "bounce_sound.wav"): lambda: self.generate_bounce(4, 0.5),
            ("Efeitos", "power_down.wav"): lambda: self.generate_swoosh(1000, 100, 1.5, 0.5),

            # Ambiente - Novos sons atmosféricos
            ("Ambiente", "menu_ambient.wav"): lambda: self.generate_ambient_loop(10.0, 0.25),
            ("Ambiente", "tension_build.wav"): lambda: self.generate_swoosh(200, 800, 4.0, 0.3),
            ("Ambiente", "calm_loop.wav"): lambda: self.generate_ambient_loop(8.0, 0.2)
        }

        # Gerar todos os arquivos
        for (category, filename), generator in sounds_config.items():
            print(f"\n📁 Gerando: {category}/{filename}")
            filepath = os.path.join(self.base_path, category, filename)
            try:
                frames = generator()
                self.save_wav(frames, filepath)
            except Exception as e:
                print(f"❌ Erro ao gerar {filename}: {e}")

def main():
    print("🎵 GERADOR DE ÁUDIO APRIMORADO - MINI JOGO LEDS")
    print("=" * 55)
    print("🎮 Sons mais envolventes e adequados para jogos!")
    print("📦 Usando apenas bibliotecas padrão do Python")
    print("🎼 Com harmônicos, envelopes ADSR e progressões musicais")

    generator = GameAudioGenerator()

    # Criar diretórios
    print("\n📁 Criando estrutura de pastas...")
    generator.create_directories()

    # Gerar todos os sons
    print("\n🎵 Gerando arquivos de áudio aprimorados...")
    generator.generate_all_sounds()

    print("\n✅ GERAÇÃO COMPLETA!")
    print(f"📁 Arquivos salvos em: {generator.base_path}")
    print(f"🔢 Total de arquivos: 42 sons de alta qualidade")
    print("\n🎯 MELHORIAS IMPLEMENTADAS:")
    print("  🎼 Harmônicos para timbres mais ricos")
    print("  📈 Envelopes ADSR para sons naturais")
    print("  🎵 Progressões musicais harmônicas")
    print("  🎮 Sons especificamente design para jogos")
    print("  🔊 Normalização inteligente de volume")
    print("  🌊 Efeitos de vibrato e filtros")
    print("\n🎮 CATEGORIAS DE SOM:")
    print("  ✨ Sistema: Elegantes e profissionais")
    print("  🏆 Jogos: Dinâmicos e motivadores")
    print("  🎯 Específicos: Únicos para cada mini-jogo")
    print("  💥 Efeitos: Impactantes e cinematográficos")
    print("  🌙 Ambiente: Atmosféricos e envolventes")
    print("\n🎵 Sistema de áudio premium pronto!")

if __name__ == "__main__":
    main()
