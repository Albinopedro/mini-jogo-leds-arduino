using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace miniJogo.Models
{
    /// <summary>
    /// ViewModel otimizado para exibição de scores em controles virtualizados
    /// Implementa INotifyPropertyChanged para data binding eficiente
    /// </summary>
    public class ScoreItemViewModel : INotifyPropertyChanged
    {
        private int _position;
        private string _playerName = string.Empty;
        private string _gameMode = string.Empty;
        private int _score;
        private int _level;
        private TimeSpan _duration;
        private DateTime _timestamp;
        private string _positionIcon = string.Empty;
        private IBrush? _positionColor;
        private string _formattedDuration = string.Empty;
        private string _formattedDate = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Posição no ranking
        /// </summary>
        public int Position
        {
            get => _position;
            set
            {
                if (SetProperty(ref _position, value))
                {
                    UpdatePositionDisplay();
                }
            }
        }

        /// <summary>
        /// Nome do jogador
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        /// <summary>
        /// Modo de jogo
        /// </summary>
        public string GameMode
        {
            get => _gameMode;
            set => SetProperty(ref _gameMode, value);
        }

        /// <summary>
        /// Pontuação
        /// </summary>
        public int Score
        {
            get => _score;
            set => SetProperty(ref _score, value);
        }

        /// <summary>
        /// Nível alcançado
        /// </summary>
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        /// <summary>
        /// Duração da partida
        /// </summary>
        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (SetProperty(ref _duration, value))
                {
                    FormattedDuration = FormatDuration(value);
                }
            }
        }

        /// <summary>
        /// Data e hora da partida
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                if (SetProperty(ref _timestamp, value))
                {
                    FormattedDate = FormatDate(value);
                }
            }
        }

        /// <summary>
        /// Ícone da posição (emoji)
        /// </summary>
        public string PositionIcon
        {
            get => _positionIcon;
            private set => SetProperty(ref _positionIcon, value);
        }

        /// <summary>
        /// Cor da posição
        /// </summary>
        public IBrush? PositionColor
        {
            get => _positionColor;
            private set => SetProperty(ref _positionColor, value);
        }

        /// <summary>
        /// Duração formatada para exibição
        /// </summary>
        public string FormattedDuration
        {
            get => _formattedDuration;
            private set => SetProperty(ref _formattedDuration, value);
        }

        /// <summary>
        /// Data formatada para exibição
        /// </summary>
        public string FormattedDate
        {
            get => _formattedDate;
            private set => SetProperty(ref _formattedDate, value);
        }

        /// <summary>
        /// Construtor padrão
        /// </summary>
        public ScoreItemViewModel()
        {
            UpdatePositionDisplay();
        }

        /// <summary>
        /// Construtor com GameScore
        /// </summary>
        public ScoreItemViewModel(GameScore gameScore, int position = 0)
        {
            Position = position;
            PlayerName = gameScore.PlayerName;
            GameMode = gameScore.GameMode;
            Score = gameScore.Score;
            Level = gameScore.Level;
            Duration = gameScore.Duration;
            Timestamp = gameScore.Timestamp;
        }

        /// <summary>
        /// Cria uma instância otimizada a partir de GameScore
        /// </summary>
        public static ScoreItemViewModel FromGameScore(GameScore gameScore, int position)
        {
            return new ScoreItemViewModel(gameScore, position);
        }

        /// <summary>
        /// Atualiza a exibição da posição (ícone e cor)
        /// </summary>
        private void UpdatePositionDisplay()
        {
            PositionIcon = GetPositionIcon(_position);
            PositionColor = GetPositionColor(_position);
        }

        /// <summary>
        /// Obtém o ícone da posição
        /// </summary>
        private static string GetPositionIcon(int position)
        {
            return position switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{position}"
            };
        }

        /// <summary>
        /// Obtém a cor da posição
        /// </summary>
        private static IBrush GetPositionColor(int position)
        {
            return position switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)), // Gold
                2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // Silver
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)), // Bronze
                _ => new SolidColorBrush(Color.FromRgb(255, 255, 255)) // White
            };
        }

        /// <summary>
        /// Formata a duração para exibição
        /// </summary>
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
            else
            {
                return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
        }

        /// <summary>
        /// Formata a data para exibição
        /// </summary>
        private static string FormatDate(DateTime date)
        {
            var now = DateTime.Now;
            var diff = now - date;

            if (diff.TotalDays < 1)
            {
                return "Hoje";
            }
            else if (diff.TotalDays < 2)
            {
                return "Ontem";
            }
            else if (diff.TotalDays < 7)
            {
                return $"{(int)diff.TotalDays} dias atrás";
            }
            else
            {
                return date.ToString("dd/MM/yyyy");
            }
        }

        /// <summary>
        /// Implementação otimizada de SetProperty
        /// </summary>
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Dispara evento PropertyChanged
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Comparação otimizada para evitar re-renderização desnecessária
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not ScoreItemViewModel other)
                return false;

            return Position == other.Position &&
                   PlayerName == other.PlayerName &&
                   GameMode == other.GameMode &&
                   Score == other.Score &&
                   Level == other.Level &&
                   Duration == other.Duration &&
                   Timestamp == other.Timestamp;
        }

        /// <summary>
        /// Hash code otimizado
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Position, PlayerName, GameMode, Score, Level, Duration, Timestamp);
        }
    }
}