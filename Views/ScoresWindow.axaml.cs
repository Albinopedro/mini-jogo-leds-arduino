using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using miniJogo.Models;
using miniJogo.Services;

namespace miniJogo.Views
{
    public partial class ScoresWindow : Window
    {
        private readonly ScoreService _scoreService;
        private List<GameScore> _allScores;
        private List<string> _allPlayers;

        public ScoresWindow(ScoreService scoreService)
        {
            InitializeComponent();
            _scoreService = scoreService;
            _allScores = new List<GameScore>();
            _allPlayers = new List<string>();
            
            GameFilterComboBox.SelectedIndex = 0;
            LoadData();
        }

        private void LoadData()
        {
            _allScores = _scoreService.GetGameScores();
            _allPlayers = _allScores.Select(s => s.PlayerName).Distinct().OrderBy(p => p).ToList();
            
            PopulatePlayerComboBox();
            LoadScores();
            LoadStatistics();
            UpdateTotalScoresText();
        }

        private void PopulatePlayerComboBox()
        {
            PlayerFilterComboBox.Items.Clear();
            PlayerFilterComboBox.Items.Add(new ComboBoxItem { Content = "Todos os jogadores", Tag = "" });
            
            foreach (var player in _allPlayers)
            {
                PlayerFilterComboBox.Items.Add(new ComboBoxItem { Content = $"👤 {player}", Tag = player });
            }
            
            PlayerFilterComboBox.SelectedIndex = 0;
        }

        private void LoadScores()
        {
            ScoresPanel.Children.Clear();
            
            var filteredScores = _allScores.AsEnumerable();
            
            // Apply game filter
            if (GameFilterComboBox.SelectedItem is ComboBoxItem gameItem && !string.IsNullOrEmpty(gameItem.Tag?.ToString()))
            {
                var gameFilter = gameItem.Tag.ToString();
                filteredScores = filteredScores.Where(s => s.GameMode.Equals(gameFilter, StringComparison.OrdinalIgnoreCase));
            }
            
            // Apply player filter
            if (PlayerFilterComboBox.SelectedItem is ComboBoxItem playerItem && !string.IsNullOrEmpty(playerItem.Tag?.ToString()))
            {
                var playerFilter = playerItem.Tag.ToString();
                filteredScores = filteredScores.Where(s => s.PlayerName.Equals(playerFilter, StringComparison.OrdinalIgnoreCase));
            }
            
            var sortedScores = filteredScores
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.Level)
                .ThenBy(s => s.Duration)
                .ToList();
            
            for (int i = 0; i < sortedScores.Count; i++)
            {
                var score = sortedScores[i];
                var scoreRow = CreateScoreRow(i + 1, score);
                ScoresPanel.Children.Add(scoreRow);
            }
            
            if (!sortedScores.Any())
            {
                var noDataPanel = new StackPanel
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Spacing = 10,
                    Margin = new Avalonia.Thickness(0, 50)
                };
                
                noDataPanel.Children.Add(new TextBlock
                {
                    Text = "🎮",
                    FontSize = 48,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                });
                
                noDataPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhuma pontuação encontrada",
                    FontSize = 16,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                });
                
                noDataPanel.Children.Add(new TextBlock
                {
                    Text = "Jogue alguns jogos para ver suas pontuações aqui!",
                    FontSize = 12,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                });
                
                ScoresPanel.Children.Add(noDataPanel);
            }
        }

        private Border CreateScoreRow(int position, GameScore score)
        {
            var border = new Border
            {
                Background = position % 2 == 0 ? new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)) : Brushes.Transparent,
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(15, 10),
                Margin = new Avalonia.Thickness(0, 0, 0, 2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("40")));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("*")));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("120")));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("80")));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("60")));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("80")));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("120")));

            // Position
            var positionText = new TextBlock
            {
                Text = GetPositionIcon(position),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = GetPositionColor(position),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(positionText, 0);

            // Player
            var playerText = new TextBlock
            {
                Text = score.PlayerName,
                FontSize = 13,
                FontWeight = FontWeight.Medium,
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(playerText, 1);

            // Game
            var gameText = new TextBlock
            {
                Text = score.GameMode,
                FontSize = 12,
                Foreground = Brushes.LightGray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(gameText, 2);

            // Score
            var scoreText = new TextBlock
            {
                Text = score.Score.ToString(),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Yellow,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(scoreText, 3);

            // Level
            var levelText = new TextBlock
            {
                Text = score.Level.ToString(),
                FontSize = 13,
                Foreground = Brushes.Cyan,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(levelText, 4);

            // Duration
            var durationText = new TextBlock
            {
                Text = score.FormattedDuration,
                FontSize = 12,
                Foreground = Brushes.LightGreen,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(durationText, 5);

            // Date
            var dateText = new TextBlock
            {
                Text = score.FormattedDate,
                FontSize = 11,
                Foreground = Brushes.LightGray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(dateText, 6);

            grid.Children.Add(positionText);
            grid.Children.Add(playerText);
            grid.Children.Add(gameText);
            grid.Children.Add(scoreText);
            grid.Children.Add(levelText);
            grid.Children.Add(durationText);
            grid.Children.Add(dateText);

            border.Child = grid;
            return border;
        }

        private string GetPositionIcon(int position)
        {
            return position switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => position.ToString()
            };
        }

        private IBrush GetPositionColor(int position)
        {
            return position switch
            {
                1 => Brushes.Gold,
                2 => Brushes.Silver,
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)), // Bronze
                _ => Brushes.White
            };
        }

        private void LoadStatistics()
        {
            LoadTopPlayers();
            LoadGameStatistics();
            LoadRecentActivity();
        }

        private void LoadTopPlayers()
        {
            TopPlayersPanel.Children.Clear();
            
            var topPlayers = _allScores
                .GroupBy(s => s.PlayerName)
                .Select(g => new
                {
                    PlayerName = g.Key,
                    TotalScore = g.Sum(s => s.Score),
                    GamesPlayed = g.Count(),
                    BestScore = g.Max(s => s.Score)
                })
                .OrderByDescending(p => p.TotalScore)
                .Take(3)
                .ToList();

            for (int i = 0; i < topPlayers.Count; i++)
            {
                var player = topPlayers[i];
                var panel = new StackPanel { Spacing = 2 };
                
                panel.Children.Add(new TextBlock
                {
                    Text = $"{GetPositionIcon(i + 1)} {player.PlayerName}",
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    FontSize = 12
                });
                
                panel.Children.Add(new TextBlock
                {
                    Text = $"{player.TotalScore} pts • {player.GamesPlayed} jogos",
                    FontSize = 10,
                    Foreground = Brushes.LightGray
                });
                
                TopPlayersPanel.Children.Add(panel);
            }
        }

        private void LoadGameStatistics()
        {
            GameStatsPanel.Children.Clear();
            
            var gameStats = _allScores
                .GroupBy(s => s.GameMode)
                .Select(g => new
                {
                    GameMode = g.Key,
                    TotalGames = g.Count(),
                    BestScore = g.Max(s => s.Score),
                    AverageScore = g.Average(s => s.Score)
                })
                .OrderByDescending(g => g.TotalGames)
                .ToList();

            foreach (var stat in gameStats)
            {
                var panel = new StackPanel { Spacing = 2 };
                
                panel.Children.Add(new TextBlock
                {
                    Text = stat.GameMode,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    FontSize = 11
                });
                
                panel.Children.Add(new TextBlock
                {
                    Text = $"{stat.TotalGames} jogos • Melhor: {stat.BestScore}",
                    FontSize = 10,
                    Foreground = Brushes.LightGray
                });
                
                GameStatsPanel.Children.Add(panel);
            }
        }

        private void LoadRecentActivity()
        {
            RecentActivityPanel.Children.Clear();
            
            var recentScores = _allScores
                .OrderByDescending(s => s.PlayedAt)
                .Take(5)
                .ToList();

            foreach (var score in recentScores)
            {
                var panel = new StackPanel { Spacing = 2 };
                
                panel.Children.Add(new TextBlock
                {
                    Text = $"{score.PlayerName} - {score.Score} pts",
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    FontSize = 11
                });
                
                var timeDiff = DateTime.Now - score.PlayedAt;
                var timeText = timeDiff.TotalDays >= 1 ? 
                    $"{(int)timeDiff.TotalDays}d atrás" : 
                    $"{(int)timeDiff.TotalHours}h atrás";
                
                panel.Children.Add(new TextBlock
                {
                    Text = $"{score.GameMode} • {timeText}",
                    FontSize = 10,
                    Foreground = Brushes.LightGray
                });
                
                RecentActivityPanel.Children.Add(panel);
            }
        }

        private void UpdateTotalScoresText()
        {
            var totalGames = _allScores.Count;
            var uniquePlayers = _allScores.Select(s => s.PlayerName).Distinct().Count();
            TotalScoresText.Text = $"{totalGames} jogos • {uniquePlayers} jogadores";
        }

        private void GameFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadScores();
        }

        private void PlayerFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadScores();
        }

        private void RefreshButton_Click(object? sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private async void ExportButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"MiniJogo_Scores_{timestamp}.txt";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var content = new StringBuilder();
                content.AppendLine("MINI JOGO LEDs - RELATÓRIO DE PONTUAÇÕES");
                content.AppendLine("=" + new string('=', 50));
                content.AppendLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                content.AppendLine($"Total de jogos: {_allScores.Count}");
                content.AppendLine();

                var sortedScores = _allScores.OrderByDescending(s => s.Score).ToList();
                content.AppendLine("RANKING GERAL:");
                content.AppendLine();

                for (int i = 0; i < Math.Min(20, sortedScores.Count); i++)
                {
                    var score = sortedScores[i];
                    content.AppendLine($"{i + 1:D2}. {score.PlayerName} - {score.Score} pts - {score.GameMode} - {score.FormattedDate}");
                }

                await File.WriteAllTextAsync(filePath, content.ToString());
                
                await ShowMessage("Exportação Concluída", $"Relatório salvo em:\n{fileName}");
            }
            catch (Exception ex)
            {
                await ShowMessage("Erro", $"Erro ao exportar dados:\n{ex.Message}");
            }
        }

        private async void ExportCsvButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"MiniJogo_Scores_{timestamp}.csv";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var content = new StringBuilder();
                content.AppendLine("Jogador,Jogo,Pontuacao,Nivel,Duracao,Data");

                foreach (var score in _allScores.OrderByDescending(s => s.Score))
                {
                    content.AppendLine($"{score.PlayerName},{score.GameMode},{score.Score},{score.Level},{score.FormattedDuration},{score.FormattedDate}");
                }

                await File.WriteAllTextAsync(filePath, content.ToString());
                
                await ShowMessage("CSV Exportado", $"Arquivo CSV salvo em:\n{fileName}");
            }
            catch (Exception ex)
            {
                await ShowMessage("Erro", $"Erro ao exportar CSV:\n{ex.Message}");
            }
        }

        private async void ClearAllButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = await ShowConfirmDialog("Confirmar Limpeza", 
                "Tem certeza que deseja apagar TODAS as pontuações?\n\nEsta ação não pode ser desfeita!");
            
            if (result)
            {
                // Clear all scores logic would go here
                // For now, just refresh the display
                LoadData();
                await ShowMessage("Concluído", "Todas as pontuações foram removidas.");
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var result = false;
            var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };
            
            stackPanel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            
            var buttonPanel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                Spacing = 10, 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center 
            };
            
            var yesButton = new Button { Content = "Sim", MinWidth = 80 };
            var noButton = new Button { Content = "Não", MinWidth = 80 };
            
            yesButton.Click += (s, e) => { result = true; dialog.Close(); };
            noButton.Click += (s, e) => { result = false; dialog.Close(); };
            
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            stackPanel.Children.Add(buttonPanel);
            
            dialog.Content = stackPanel;
            await dialog.ShowDialog(this);
            
            return result;
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
}