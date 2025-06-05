using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace miniJogo.Models
{
    public enum GameMode
    {
        Menu = 0,
        PegaLuz = 1,
        SequenciaMaluca = 2,
        GatoRato = 3,
        EsquivaMeteoros = 4,
        GuitarHero = 5,
        LightningStrike = 6,
        SniperMode = 7
    }

    public class GameState : INotifyPropertyChanged
    {
        private GameMode _currentMode = GameMode.Menu;
        private bool _gameActive = false;
        private int _score = 0;
        private int _level = 1;
        private string _playerName = "";
        private DateTime _gameStartTime;
        private TimeSpan _elapsedTime;

        public GameMode CurrentMode
        {
            get => _currentMode;
            set => SetProperty(ref _currentMode, value);
        }

        public bool GameActive
        {
            get => _gameActive;
            set => SetProperty(ref _gameActive, value);
        }

        public int Score
        {
            get => _score;
            set => SetProperty(ref _score, value);
        }

        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public DateTime GameStartTime
        {
            get => _gameStartTime;
            set => SetProperty(ref _gameStartTime, value);
        }

        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set => SetProperty(ref _elapsedTime, value);
        }

        public string FormattedElapsedTime => $"{_elapsedTime:mm\\:ss}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class PlayerScore
    {
        public string PlayerName { get; set; } = "";
        public GameMode Game { get; set; }
        public int Score { get; set; }
        public int Level { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }

        public string FormattedDate => Date.ToString("dd/MM/yyyy HH:mm");
        public string FormattedDuration => $"{Duration:mm\\:ss}";
        public string GameName => Game switch
        {
            GameMode.PegaLuz => "Pega-Luz",
            GameMode.SequenciaMaluca => "Sequência Maluca",
            GameMode.GatoRato => "Gato e Rato",
            GameMode.EsquivaMeteoros => "Esquiva Meteoros",
            GameMode.GuitarHero => "Guitar Hero",
            GameMode.LightningStrike => "Lightning Strike",
            GameMode.SniperMode => "Sniper Mode",
            _ => "Desconhecido"
        };
    }

    // New GameScore class compatible with the updated system
    public class GameScore
    {
        public string PlayerName { get; set; } = "";
        public string GameMode { get; set; } = "";
        public int Score { get; set; }
        public int Level { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime PlayedAt { get; set; }
        public DateTime Timestamp { get => PlayedAt; set => PlayedAt = value; }

        public string FormattedDate => PlayedAt.ToString("dd/MM/yyyy HH:mm");
        public string FormattedDuration => $"{Duration:mm\\:ss}";
    }

    public class GameEvent
    {
        public string Type { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();

        public GameEvent()
        {
            Timestamp = DateTime.Now;
        }

        public GameEvent(string type, params object[] values) : this()
        {
            Type = type;
            for (int i = 0; i < values.Length; i++)
            {
                Data[$"value{i}"] = values[i];
            }
        }
    }

    public static class GameModeExtensions
    {
        public static string GetDisplayName(this GameMode mode)
        {
            return mode switch
            {
                GameMode.PegaLuz => "Pega-Luz",
                GameMode.SequenciaMaluca => "Sequência Maluca",
                GameMode.GatoRato => "Gato e Rato",
                GameMode.EsquivaMeteoros => "Esquiva Meteoros",
                GameMode.GuitarHero => "Guitar Hero",
                GameMode.LightningStrike => "Lightning Strike",
                GameMode.SniperMode => "Sniper Mode",
                _ => "Menu"
            };
        }

        public static string GetIcon(this GameMode mode)
        {
            return mode switch
            {
                GameMode.PegaLuz => "⚡",
                GameMode.SequenciaMaluca => "🧠",
                GameMode.GatoRato => "🐱",
                GameMode.EsquivaMeteoros => "☄️",
                GameMode.GuitarHero => "🎸",
                GameMode.LightningStrike => "⚡",
                GameMode.SniperMode => "🎯",
                _ => "🎮"
            };
        }

        public static string GetDescription(this GameMode mode)
        {
            return mode switch
            {
                GameMode.PegaLuz => "Reaja rapidamente aos LEDs que acendem!",
                GameMode.SequenciaMaluca => "Memorize e repita sequências cada vez maiores.",
                GameMode.GatoRato => "Controle o gato para pegar o rato em movimento.",
                GameMode.EsquivaMeteoros => "Desvie dos meteoros e sobreviva o máximo possível!",
                GameMode.GuitarHero => "Toque as notas no ritmo certo como um verdadeiro guitarrista.",
                GameMode.LightningStrike => "Memorize padrões rápidos de LEDs com velocidade extrema.",
                GameMode.SniperMode => "Mire e atire nos alvos que aparecem rapidamente!",
                _ => "Selecione um jogo para começar."
            };
        }
    }
}
