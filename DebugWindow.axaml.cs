using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace miniJogo
{
    public partial class DebugWindow : Window
    {
        private List<string> _debugMessages = new();
        private readonly object _messagesLock = new object();
        
        // Delegate for refreshing client info
        public Action? OnRefreshClientInfo { get; set; }

        public DebugWindow()
        {
            InitializeComponent();
        }

        public void SetRefreshButtonVisibility(bool visible)
        {
            RefreshInfoButton.IsVisible = visible;
        }

        public void AddDebugMessage(string message, bool isDebug = false)
        {
            // Use performance config to filter messages
            if (!PerformanceConfig.ShouldLog(message, isDebug))
                return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = isDebug ? "[DEBUG]" : "[INFO]";
            var formattedMessage = $"[{timestamp}]{prefix} {message}";

            lock (_messagesLock)
            {
                _debugMessages.Add(formattedMessage);
                
                // Use performance config for max messages limit
                if (_debugMessages.Count > PerformanceConfig.MaxDebugMessages)
                {
                    // Remove multiple messages at once for better performance
                    var removeCount = _debugMessages.Count - PerformanceConfig.MaxDebugMessages;
                    _debugMessages.RemoveRange(0, removeCount);
                }
            }

            // Only update UI if not in high performance mode or for critical messages
            if (!PerformanceConfig.IsHighPerformanceMode() || message.Contains("ERRO") || message.Contains("CRITICAL"))
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateDebugDisplay();
                    ScrollToBottom();
                });
            }
        }

        public void AddMessage(string message, bool isDebug = false)
        {
            // Use performance config to filter messages
            if (!PerformanceConfig.ShouldLog(message, isDebug))
                return;

            lock (_messagesLock)
            {
                _debugMessages.Add(message);
                
                // Use performance config for max messages limit
                if (_debugMessages.Count > PerformanceConfig.MaxDebugMessages)
                {
                    var removeCount = _debugMessages.Count - PerformanceConfig.MaxDebugMessages;
                    _debugMessages.RemoveRange(0, removeCount);
                }
            }

            // Batch UI updates for better performance
            if (!PerformanceConfig.IsHighPerformanceMode() || message.Contains("ERRO") || message.Contains("CRITICAL"))
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateDebugDisplay();
                    ScrollToBottom();
                });
            }
        }

        private void UpdateDebugDisplay()
        {
            lock (_messagesLock)
            {
                // Optimize string building for better performance
                var sb = new StringBuilder(_debugMessages.Count * 50); // Pre-allocate capacity
                
                // Only show last messages if in high performance mode
                var messagesToShow = PerformanceConfig.IsHighPerformanceMode() ? 
                    Math.Min(_debugMessages.Count, 50) : _debugMessages.Count;
                
                var startIndex = Math.Max(0, _debugMessages.Count - messagesToShow);
                
                for (int i = startIndex; i < _debugMessages.Count; i++)
                {
                    sb.AppendLine(_debugMessages[i]);
                }
                
                DebugTextBlock.Text = sb.ToString();
            }
        }

        private void ScrollToBottom()
        {
            try
            {
                DebugScrollViewer.ScrollToEnd();
            }
            catch
            {
                // Silent error handling for better performance
            }
        }

        private void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            lock (_messagesLock)
            {
                _debugMessages.Clear();
            }
            DebugTextBlock.Text = "Console limpo - aguardando mensagens...";
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"debug_log_{timestamp}.txt";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                string content;
                lock (_messagesLock)
                {
                    content = string.Join(Environment.NewLine, _debugMessages);
                }

                // Write file asynchronously for better performance
                await File.WriteAllTextAsync(filePath, content);

                // Simplified confirmation dialog for better performance
                var dialog = new Window()
                {
                    Title = "Log Salvo",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Background = Avalonia.Media.Brushes.DarkSlateGray
                };

                var panel = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                panel.Children.Add(new TextBlock
                {
                    Text = "✅ Log salvo com sucesso!",
                    FontSize = 14,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = Avalonia.Media.Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });

                panel.Children.Add(new TextBlock
                {
                    Text = fileName,
                    FontSize = 12,
                    Foreground = Avalonia.Media.Brushes.LightGray,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Padding = new Avalonia.Thickness(20, 5)
                };

                okButton.Click += (s, e) => dialog.Close();
                panel.Children.Add(okButton);

                dialog.Content = panel;
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                // Only show critical errors
                if (PerformanceConfig.ShouldLog(ex.Message, false))
                {
                    AddDebugMessage($"Erro ao salvar log: {ex.Message}", true);
                }
            }
        }

        private void RefreshInfoButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                AddMessage("🔄 Atualizando informações...", false);
                OnRefreshClientInfo?.Invoke();
            }
            catch (Exception ex)
            {
                if (PerformanceConfig.ShouldLog(ex.Message, false))
                {
                    AddMessage($"❌ Erro ao atualizar informações: {ex.Message}", false);
                }
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Hide(); // Use Hide() instead of Close() to be able to reopen the window
        }

        protected override void OnClosed(EventArgs e)
        {
            // Let the main window manage closing
        }

        /// <summary>
        /// Force update display for critical messages
        /// </summary>
        public void ForceUpdate()
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateDebugDisplay();
                ScrollToBottom();
            });
        }

        /// <summary>
        /// Get current message count for monitoring
        /// </summary>
        public int GetMessageCount()
        {
            lock (_messagesLock)
            {
                return _debugMessages.Count;
            }
        }
    }
}