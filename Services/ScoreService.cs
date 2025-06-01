using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using miniJogo.Models;

namespace miniJogo.Services
{
    public class ScoreService
    {
        private readonly string _scoresFilePath;
        private readonly string _gameScoresFilePath;
        private List<PlayerScore> _scores;
        private List<GameScore> _gameScores;

        public ScoreService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiniJogo");
            _scoresFilePath = Path.Combine(appDataPath, "scores.json");
            _gameScoresFilePath = Path.Combine(appDataPath, "game_scores.json");
            _scores = new List<PlayerScore>();
            _gameScores = new List<GameScore>();
            
            // Ensure directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            LoadScores();
            LoadGameScores();
        }

        public async Task SaveScoreAsync(PlayerScore score)
        {
            _scores.Add(score);
            await SaveScoresAsync();
        }

        public void SaveScore(GameScore score)
        {
            _gameScores.Add(score);
            SaveGameScores();
        }

        public List<GameScore> GetGameScores(string? gameMode = null, string? playerName = null)
        {
            var query = _gameScores.AsQueryable();
            
            if (!string.IsNullOrEmpty(gameMode))
            {
                query = query.Where(s => s.GameMode.Equals(gameMode, StringComparison.OrdinalIgnoreCase));
            }
            
            if (!string.IsNullOrEmpty(playerName))
            {
                query = query.Where(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            }
            
            return query.OrderByDescending(s => s.Score).ThenByDescending(s => s.PlayedAt).ToList();
        }

        public List<GameScore> GetTopScores(int count = 10)
        {
            return _gameScores
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.Level)
                .ThenBy(s => s.Duration)
                .Take(count)
                .ToList();
        }

        public List<PlayerScore> GetHighScores(GameMode gameMode, int count = 10)
        {
            return _scores
                .Where(s => s.Game == gameMode)
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.Level)
                .ThenBy(s => s.Duration)
                .Take(count)
                .ToList();
        }

        public List<PlayerScore> GetPlayerScores(string playerName, GameMode? gameMode = null)
        {
            var query = _scores.Where(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            
            if (gameMode.HasValue)
            {
                query = query.Where(s => s.Game == gameMode.Value);
            }
            
            return query
                .OrderByDescending(s => s.Date)
                .ToList();
        }

        public List<PlayerScore> GetAllScores()
        {
            return _scores.OrderByDescending(s => s.Date).ToList();
        }

        public Dictionary<GameMode, PlayerScore> GetBestScoresByGame()
        {
            var result = new Dictionary<GameMode, PlayerScore>();
            
            foreach (GameMode game in Enum.GetValues<GameMode>())
            {
                if (game == GameMode.Menu) continue;
                
                var bestScore = _scores
                    .Where(s => s.Game == game)
                    .OrderByDescending(s => s.Score)
                    .ThenByDescending(s => s.Level)
                    .ThenBy(s => s.Duration)
                    .FirstOrDefault();
                
                if (bestScore != null)
                {
                    result[game] = bestScore;
                }
            }
            
            return result;
        }

        public PlayerScoreStats GetPlayerStats(string playerName)
        {
            var playerScores = GetPlayerScores(playerName);
            
            if (!playerScores.Any())
            {
                return new PlayerScoreStats { PlayerName = playerName };
            }

            var stats = new PlayerScoreStats
            {
                PlayerName = playerName,
                TotalGames = playerScores.Count,
                TotalScore = playerScores.Sum(s => s.Score),
                BestScore = playerScores.Max(s => s.Score),
                AverageScore = playerScores.Average(s => s.Score),
                TotalPlayTime = TimeSpan.FromTicks(playerScores.Sum(s => s.Duration.Ticks)),
                FirstGameDate = playerScores.Min(s => s.Date),
                LastGameDate = playerScores.Max(s => s.Date)
            };

            // Game-specific stats
            foreach (GameMode game in Enum.GetValues<GameMode>())
            {
                if (game == GameMode.Menu) continue;
                
                var gameScores = playerScores.Where(s => s.Game == game).ToList();
                if (gameScores.Any())
                {
                    stats.GameStats[game] = new GameStats
                    {
                        GamesPlayed = gameScores.Count,
                        BestScore = gameScores.Max(s => s.Score),
                        AverageScore = gameScores.Average(s => s.Score),
                        BestLevel = gameScores.Max(s => s.Level),
                        TotalTime = TimeSpan.FromTicks(gameScores.Sum(s => s.Duration.Ticks))
                    };
                }
            }

            return stats;
        }

        public async Task ClearScoresAsync()
        {
            _scores.Clear();
            await SaveScoresAsync();
        }

        public async Task RemovePlayerScoresAsync(string playerName)
        {
            _scores.RemoveAll(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            await SaveScoresAsync();
        }

        private void LoadScores()
        {
            try
            {
                if (File.Exists(_scoresFilePath))
                {
                    var json = File.ReadAllText(_scoresFilePath);
                    var scores = JsonSerializer.Deserialize<List<PlayerScore>>(json);
                    _scores = scores ?? new List<PlayerScore>();
                }
            }
            catch
            {
                _scores = new List<PlayerScore>();
            }
        }

        private void LoadGameScores()
        {
            try
            {
                if (File.Exists(_gameScoresFilePath))
                {
                    var json = File.ReadAllText(_gameScoresFilePath);
                    var scores = JsonSerializer.Deserialize<List<GameScore>>(json);
                    _gameScores = scores ?? new List<GameScore>();
                }
            }
            catch
            {
                _gameScores = new List<GameScore>();
            }
        }

        private async Task SaveScoresAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(_scores, options);
                await File.WriteAllTextAsync(_scoresFilePath, json);
            }
            catch
            {
                // Silently fail - scores won't persist but app continues working
            }
        }

        private void SaveGameScores()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var json = JsonSerializer.Serialize(_gameScores, options);
                File.WriteAllText(_gameScoresFilePath, json);
            }
            catch
            {
                // Silently fail - scores won't persist but app continues working
            }
        }
    }

    public class PlayerScoreStats
    {
        public string PlayerName { get; set; } = "";
        public int TotalGames { get; set; }
        public int TotalScore { get; set; }
        public int BestScore { get; set; }
        public double AverageScore { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public DateTime FirstGameDate { get; set; }
        public DateTime LastGameDate { get; set; }
        public Dictionary<GameMode, GameStats> GameStats { get; set; } = new();

        public string FormattedTotalPlayTime => $"{TotalPlayTime:hh\\:mm\\:ss}";
        public string FormattedAverageScore => $"{AverageScore:F1}";
    }

    public class GameStats
    {
        public int GamesPlayed { get; set; }
        public int BestScore { get; set; }
        public double AverageScore { get; set; }
        public int BestLevel { get; set; }
        public TimeSpan TotalTime { get; set; }

        public string FormattedTotalTime => $"{TotalTime:hh\\:mm\\:ss}";
        public string FormattedAverageScore => $"{AverageScore:F1}";
    }
}