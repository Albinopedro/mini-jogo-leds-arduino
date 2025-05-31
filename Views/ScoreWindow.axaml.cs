using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using miniJogo.Models;
using miniJogo.Services;
using Avalonia.Threading;
using System.IO;
using System.Text;

namespace miniJogo.Views
{
    public partial class ScoreWindow : Window
    {
        private readonly ScoreService _scoreService;
        private List<PlayerScore> _allScores;
        private List<string> _allPlayers;

        public ScoreWindow()
        {
            InitializeComponent();
            _scoreService = new ScoreService();
            _allScores = new List<PlayerScore>();
            _allPlayers = new List<string>();

            LoadData();
        }

        private async void LoadData()
        {
            await Task.Run(() =>
            {
                _allScores = _scoreService.GetAllScores();
                _allPlayers = _allScores.Select(s => s.PlayerName).Distinct().OrderBy(n => n).ToList();
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PopulatePlayerComboBox();
                LoadHighScores();
                LoadRecentGames();

                // Select first game filter
                GameFilterComboBox.SelectedIndex = 0;

                FooterStatusText.Text = $"Carregadas {_allScores.Count} pontuações de {_allPlayers.Count} jogadores";
            });
        }

        private void PopulatePlayerComboBox()
        {
            PlayerFilterComboBox.Items.Clear();
            PlayerFilterComboBox.Items.Add("Selecione um jogador...");

            foreach (var player in _allPlayers)
            {
                PlayerFilterComboBox.Items.Add(player);
            }

            PlayerFilterComboBox.SelectedIndex = 0;
        }

        private void LoadHighScores(GameMode? filterGame = null)
        {
            var scores = filterGame.HasValue ?
                _scoreService.GetHighScores(filterGame.Value, 50) :
                _allScores.OrderByDescending(s => s.Score).Take(50).ToList();

            HighScoresPanel.Children.Clear();

            for (int i = 0; i < scores.Count; i++)
            {
                var score = scores[i];
                var border = new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#434C5E")),
                    CornerRadius = new Avalonia.CornerRadius(5),
                    Padding = new Avalonia.Thickness(10),
                    Margin = new Avalonia.Thickness(0, 2)
                };

                var grid = new Grid();
                for (int j = 0; j < 7; j++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                }

                var rankText = new TextBlock { Text = $"#{i + 1}", Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var playerText = new TextBlock { Text = score.PlayerName, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var gameText = new TextBlock { Text = score.GameName, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var scoreText = new TextBlock { Text = score.Score.ToString(), Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A3BE8C")) };
                var levelText = new TextBlock { Text = score.Level.ToString(), Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EBCB8B")) };
                var durationText = new TextBlock { Text = score.FormattedDuration, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var dateText = new TextBlock { Text = score.FormattedDate, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };

                Grid.SetColumn(rankText, 0);
                Grid.SetColumn(playerText, 1);
                Grid.SetColumn(gameText, 2);
                Grid.SetColumn(scoreText, 3);
                Grid.SetColumn(levelText, 4);
                Grid.SetColumn(durationText, 5);
                Grid.SetColumn(dateText, 6);

                grid.Children.Add(rankText);
                grid.Children.Add(playerText);
                grid.Children.Add(gameText);
                grid.Children.Add(scoreText);
                grid.Children.Add(levelText);
                grid.Children.Add(durationText);
                grid.Children.Add(dateText);

                border.Child = grid;
                HighScoresPanel.Children.Add(border);
            }

            ScoreCountText.Text = $"{scores.Count} pontuações encontradas";
        }

        private void LoadRecentGames()
        {
            var recentGames = _allScores.OrderByDescending(s => s.Date).Take(100).ToList();

            RecentGamesPanel.Children.Clear();

            foreach (var game in recentGames)
            {
                var border = new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#434C5E")),
                    CornerRadius = new Avalonia.CornerRadius(5),
                    Padding = new Avalonia.Thickness(10),
                    Margin = new Avalonia.Thickness(0, 2)
                };

                var grid = new Grid();
                for (int j = 0; j < 6; j++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                }

                var dateText = new TextBlock { Text = game.FormattedDate, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var playerText = new TextBlock { Text = game.PlayerName, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var gameText = new TextBlock { Text = game.GameName, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };
                var scoreText = new TextBlock { Text = game.Score.ToString(), Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#A3BE8C")) };
                var levelText = new TextBlock { Text = game.Level.ToString(), Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EBCB8B")) };
                var durationText = new TextBlock { Text = game.FormattedDuration, Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White) };

                Grid.SetColumn(dateText, 0);
                Grid.SetColumn(playerText, 1);
                Grid.SetColumn(gameText, 2);
                Grid.SetColumn(scoreText, 3);
                Grid.SetColumn(levelText, 4);
                Grid.SetColumn(durationText, 5);

                grid.Children.Add(dateText);
                grid.Children.Add(playerText);
                grid.Children.Add(gameText);
                grid.Children.Add(scoreText);
                grid.Children.Add(levelText);
                grid.Children.Add(durationText);

                border.Child = grid;
                RecentGamesPanel.Children.Add(border);
            }
        }

        private void GameFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (GameFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (int.TryParse(tag, out int gameMode))
                {
                    if (gameMode == -1)
                    {
                        LoadHighScores();
                    }
                    else
                    {
                        LoadHighScores((GameMode)gameMode);
                    }
                }
            }
        }

        private void PlayerFilterComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (PlayerFilterComboBox.SelectedIndex > 0 && PlayerFilterComboBox.SelectedItem is string playerName)
            {
                LoadPlayerStats(playerName);
            }
            else
            {
                HidePlayerStats();
            }
        }

        private void LoadPlayerStats(string playerName)
        {
            var stats = _scoreService.GetPlayerStats(playerName);

            if (stats.TotalGames == 0)
            {
                HidePlayerStats();
                return;
            }

            // Show overview
            PlayerStatsOverview.IsVisible = true;
            GameStatsScrollViewer.IsVisible = true;
            NoPlayerSelectedText.IsVisible = false;

            TotalGamesText.Text = stats.TotalGames.ToString();
            BestScoreText.Text = stats.BestScore.ToString();
            AverageScoreText.Text = stats.FormattedAverageScore;
            TotalTimeText.Text = stats.FormattedTotalPlayTime;

            // Populate game-specific stats
            GameStatsPanel.Children.Clear();

            foreach (var gameStatPair in stats.GameStats)
            {
                var game = gameStatPair.Key;
                var gameStat = gameStatPair.Value;

                var border = new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#434C5E")),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Padding = new Avalonia.Thickness(15),
                    Margin = new Avalonia.Thickness(0, 0, 0, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                // Game name
                var gameNamePanel = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                gameNamePanel.Children.Add(new TextBlock
                {
                    Text = $"{game.GetIcon()} {game.GetDisplayName()}",
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White),
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
                Grid.SetColumn(gameNamePanel, 0);
                grid.Children.Add(gameNamePanel);

                // Games played
                var gamesPanel = CreateStatPanel("Jogos", gameStat.GamesPlayed.ToString(), "#88C0D0");
                Grid.SetColumn(gamesPanel, 1);
                grid.Children.Add(gamesPanel);

                // Best score
                var scorePanel = CreateStatPanel("Melhor", gameStat.BestScore.ToString(), "#A3BE8C");
                Grid.SetColumn(scorePanel, 2);
                grid.Children.Add(scorePanel);

                // Best level
                var levelPanel = CreateStatPanel("Nível Max", gameStat.BestLevel.ToString(), "#EBCB8B");
                Grid.SetColumn(levelPanel, 3);
                grid.Children.Add(levelPanel);

                // Total time
                var timePanel = CreateStatPanel("Tempo", gameStat.FormattedTotalTime, "#BF616A");
                Grid.SetColumn(timePanel, 4);
                grid.Children.Add(timePanel);

                border.Child = grid;
                GameStatsPanel.Children.Add(border);
            }
        }

        private StackPanel CreateStatPanel(string label, string value, string color)
        {
            var panel = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(color)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontSize = 12
            });

            panel.Children.Add(new TextBlock
            {
                Text = value,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                FontSize = 16
            });

            return panel;
        }

        private void HidePlayerStats()
        {
            PlayerStatsOverview.IsVisible = false;
            GameStatsScrollViewer.IsVisible = false;
            NoPlayerSelectedText.IsVisible = true;
        }

        private async void ExportButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var storageProvider = StorageProvider;
                if (storageProvider == null) return;

                var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Exportar Pontuações",
                    DefaultExtension = "csv",
                    SuggestedFileName = "pontuacoes.csv",
                    FileTypeChoices = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("Text Files") { Patterns = new[] { "*.txt" } }
                    }
                });

                if (file != null)
                {
                    await ExportScoresToCsv(file.Path.LocalPath);
                    FooterStatusText.Text = $"Pontuações exportadas para: {file.Name}";
                }
            }
            catch (Exception ex)
            {
                FooterStatusText.Text = $"Erro ao exportar: {ex.Message}";
            }
        }

        private async Task ExportScoresToCsv(string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Data,Jogador,Jogo,Pontuação,Nível,Duração");

            foreach (var score in _allScores.OrderByDescending(s => s.Date))
            {
                csv.AppendLine($"{score.FormattedDate},{score.PlayerName},{score.GameName},{score.Score},{score.Level},{score.FormattedDuration}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        private async void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            // Simple confirmation without external dependencies
            var confirmWindow = new Window
            {
                Title = "Confirmar Limpeza",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var stack = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 20 };
            stack.Children.Add(new TextBlock
            {
                Text = "Tem certeza que deseja apagar TODAS as pontuações?\nEsta ação não pode ser desfeita.",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

            var yesButton = new Button { Content = "Sim, Apagar Tudo" };
            var noButton = new Button { Content = "Cancelar" };

            bool confirmed = false;
            yesButton.Click += (s, e) => { confirmed = true; confirmWindow.Close(); };
            noButton.Click += (s, e) => confirmWindow.Close();

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            stack.Children.Add(buttonPanel);

            confirmWindow.Content = stack;

            await confirmWindow.ShowDialog(this);

            if (confirmed)
            {
                await _scoreService.ClearScoresAsync();
                LoadData();
                FooterStatusText.Text = "Todas as pontuações foram apagadas.";
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }

}
