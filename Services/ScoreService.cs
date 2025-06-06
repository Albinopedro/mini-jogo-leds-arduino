using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using miniJogo.Models;

namespace miniJogo.Services
{
    public class ScoreService
    {
        private readonly string _scoresFilePath;
        private readonly string _gameScoresFilePath;
        private List<PlayerScore> _scores;
        private List<GameScore> _gameScores;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly object _lockObject = new();

        public event EventHandler<GameScore>? ScoreSaved;

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
            await _semaphore.WaitAsync();
            try
            {
                lock (_lockObject)
                {
                    _scores.Add(score);
                }
                await SaveScoresAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SaveScoreAsync(GameScore score)
        {
            await _semaphore.WaitAsync();
            try
            {
                lock (_lockObject)
                {
                    _gameScores.Add(score);
                }
                await SaveGameScoresAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void SaveScore(GameScore score)
        {
            lock (_lockObject)
            {
                _gameScores.Add(score);
            }
            SaveGameScores();
            
            // Notify subscribers that a new score was saved
            ScoreSaved?.Invoke(this, score);
        }

        public async Task<List<GameScore>> GetGameScoresAsync(string? gameMode = null, string? playerName = null)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var query = _gameScores.AsQueryable();
                    
                    if (!string.IsNullOrEmpty(gameMode))
                        query = query.Where(s => s.GameMode == gameMode);
                    
                    if (!string.IsNullOrEmpty(playerName))
                        query = query.Where(s => s.PlayerName.Contains(playerName, StringComparison.OrdinalIgnoreCase));
                    
                    return query.OrderByDescending(s => s.Score).ToList();
                }
            });
        }

        public List<GameScore> GetGameScores(string? gameMode = null, string? playerName = null)
        {
            lock (_lockObject)
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

        public List<PlayerScore> GetHighScores(Models.GameMode gameMode, int count = 10)
        {
            return _scores
                .Where(s => s.Game.Equals(gameMode))
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.Level)
                .ThenBy(s => s.Duration)
                .Take(count)
                .ToList();
        }

        public async Task<List<GameScore>> GetTopScoresAsync(int count = 10)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return _gameScores
                        .OrderByDescending(s => s.Score)
                        .ThenByDescending(s => s.Level)
                        .ThenBy(s => s.Duration)
                        .Take(count)
                        .ToList();
                }
            });
        }

        public async Task<List<PlayerScore>> GetHighScoresAsync(Models.GameMode gameMode, int count = 10)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return _scores
                        .Where(s => s.Game.Equals(gameMode))
                        .OrderByDescending(s => s.Score)
                        .ThenByDescending(s => s.Level)
                        .ThenBy(s => s.Duration)
                        .Take(count)
                        .ToList();
                }
            });
        }

        public async Task<List<PlayerScore>> GetPlayerScoresAsync(string playerName, Models.GameMode? gameMode = null)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var query = _scores.Where(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                    
                    if (gameMode.HasValue)
                    {
                        query = query.Where(s => s.Game.Equals(gameMode.Value));
                    }
                    
                    return query.OrderByDescending(s => s.Score).ToList();
                }
            });
        }

        public List<PlayerScore> GetPlayerScores(string playerName, Models.GameMode? gameMode = null)
        {
            lock (_lockObject)
            {
                var query = _scores.Where(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                
                if (gameMode.HasValue)
                {
                    query = query.Where(s => s.Game.Equals(gameMode.Value));
                }
                
                return query
                .OrderByDescending(s => s.Date)
                .ToList();
            }
        }

        public List<PlayerScore> GetAllScores()
        {
            return _scores.OrderByDescending(s => s.Date).ToList();
        }

        public Dictionary<Models.GameMode, PlayerScore> GetBestScoresByGame()
        {
            var result = new Dictionary<Models.GameMode, PlayerScore>();
            
            foreach (Models.GameMode game in Enum.GetValues<Models.GameMode>())
            {
                if (game == Models.GameMode.Menu) continue;
                
                var bestScore = _scores
                    .Where(s => s.Game.Equals(game))
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
            foreach (Models.GameMode game in Enum.GetValues<Models.GameMode>())
            {
                if (game == Models.GameMode.Menu) continue;
                
                var gameScores = playerScores.Where(s => s.Game.Equals(game)).ToList();
                if (gameScores.Any())
                {
                    stats.GameStats[game] = new GameStats
                    {
                        GamesPlayed = gameScores.Count(),
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

        private async Task LoadScoresAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (File.Exists(_scoresFilePath))
                {
                    var json = await File.ReadAllTextAsync(_scoresFilePath);
                    var scores = JsonSerializer.Deserialize<List<PlayerScore>>(json) ?? new List<PlayerScore>();
                    lock (_lockObject)
                    {
                        _scores = scores;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading scores: {ex.Message}");
                lock (_lockObject)
                {
                    _scores = new List<PlayerScore>();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void LoadScores()
        {
            try
            {
                if (File.Exists(_scoresFilePath))
                {
                    var json = File.ReadAllText(_scoresFilePath);
                    lock (_lockObject)
                    {
                        _scores = JsonSerializer.Deserialize<List<PlayerScore>>(json) ?? new List<PlayerScore>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading scores: {ex.Message}");
                lock (_lockObject)
                {
                    _scores = new List<PlayerScore>();
                }
            }
        }

        private async Task LoadGameScoresAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (File.Exists(_gameScoresFilePath))
                {
                    var json = await File.ReadAllTextAsync(_gameScoresFilePath);
                    var scores = JsonSerializer.Deserialize<List<GameScore>>(json) ?? new List<GameScore>();
                    lock (_lockObject)
                    {
                        _gameScores = scores;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading game scores: {ex.Message}");
                lock (_lockObject)
                {
                    _gameScores = new List<GameScore>();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void LoadGameScores()
        {
            try
            {
                if (File.Exists(_gameScoresFilePath))
                {
                    var json = File.ReadAllText(_gameScoresFilePath);
                    lock (_lockObject)
                    {
                        _gameScores = JsonSerializer.Deserialize<List<GameScore>>(json) ?? new List<GameScore>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading game scores: {ex.Message}");
                lock (_lockObject)
                {
                    _gameScores = new List<GameScore>();
                }
            }
        }

        private async Task SaveScoresAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                List<PlayerScore> scoresToSave;
                lock (_lockObject)
                {
                    scoresToSave = new List<PlayerScore>(_scores);
                }
                
                var json = JsonSerializer.Serialize(scoresToSave, options);
                await File.WriteAllTextAsync(_scoresFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving scores: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task SaveGameScoresAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                List<GameScore> scoresToSave;
                lock (_lockObject)
                {
                    scoresToSave = new List<GameScore>(_gameScores);
                }
                
                var json = JsonSerializer.Serialize(scoresToSave, options);
                await File.WriteAllTextAsync(_gameScoresFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving game scores: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
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
                
                List<GameScore> scoresToSave;
                lock (_lockObject)
                {
                    scoresToSave = new List<GameScore>(_gameScores);
                }
                
                var json = JsonSerializer.Serialize(scoresToSave, options);
                File.WriteAllText(_gameScoresFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving game scores: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
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
        public Dictionary<Models.GameMode, GameStats> GameStats { get; set; } = new();

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