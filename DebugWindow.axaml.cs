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

        public DebugWindow()
        {
            InitializeComponent();
        }

        public void AddDebugMessage(string message, bool isDebug = false)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var prefix = isDebug ? "[DEBUG]" : "[INFO]";
            var formattedMessage = $"[{timestamp}]{prefix} {message}";

            lock (_messagesLock)
            {
                _debugMessages.Add(formattedMessage);
                
                // Manter apenas as últimas 500 mensagens para evitar uso excessivo de memória
                if (_debugMessages.Count > 500)
                {
                    _debugMessages.RemoveAt(0);
                }
            }

            // Atualizar UI na thread principal
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateDebugDisplay();
                ScrollToBottom();
            });
        }

        public void AddMessage(string message, bool isDebug = false)
        {
            lock (_messagesLock)
            {
                _debugMessages.Add(message);
                
                // Manter apenas as últimas 500 mensagens para evitar uso excessivo de memória
                if (_debugMessages.Count > 500)
                {
                    _debugMessages.RemoveAt(0);
                }
            }

            // Atualizar UI na thread principal
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateDebugDisplay();
                ScrollToBottom();
            });
        }

        private void UpdateDebugDisplay()
        {
            lock (_messagesLock)
            {
                var sb = new StringBuilder();
                foreach (var message in _debugMessages)
                {
                    sb.AppendLine(message);
                }
                DebugTextBlock.Text = sb.ToString();
            }
        }

        private void ScrollToBottom()
        {
            DebugScrollViewer.ScrollToEnd();
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

                lock (_messagesLock)
                {
                    var content = string.Join(Environment.NewLine, _debugMessages);
                    File.WriteAllText(filePath, content);
                }

                // Mostrar confirmação
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
                // Em caso de erro, mostrar no próprio console de debug
                AddDebugMessage($"Erro ao salvar log: {ex.Message}", true);
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Hide(); // Usar Hide() ao invés de Close() para poder reabrir a janela
        }

        protected override void OnClosed(EventArgs e)
        {
            // Não fazer nada no close - deixar a janela principal gerenciar
        }
    }
}