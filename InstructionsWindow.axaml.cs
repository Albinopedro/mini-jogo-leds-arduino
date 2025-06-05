using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace miniJogo
{
    public partial class InstructionsWindow : Window
    {
        private readonly Dictionary<int, string> _gameInstructions;

        public InstructionsWindow()
        {
            _gameInstructions = new Dictionary<int, string>();
            InitializeComponent();
        }

        public InstructionsWindow(Dictionary<int, string> gameInstructions)
        {
            _gameInstructions = gameInstructions ?? new Dictionary<int, string>();
            InitializeComponent();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void PrintButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Create a text version of the instructions for printing/saving
                var instructions = new StringBuilder();
                instructions.AppendLine("MINI JOGO LEDs - GUIA COMPLETO DOS JOGOS");
                instructions.AppendLine("=".PadRight(50, '='));
                instructions.AppendLine();

                instructions.AppendLine("1. PEGA-LUZ");
                instructions.AppendLine("Objetivo: Pressione rapidamente a tecla correspondente ao LED que acender.");
                instructions.AppendLine("- Um LED aleatório acenderá na matriz");
                instructions.AppendLine("- Pressione a tecla correspondente o mais rápido possível");
                instructions.AppendLine("- Cada acerto aumenta sua pontuação");
                instructions.AppendLine("- A dificuldade aumenta a cada 5 pontos");
                instructions.AppendLine();

                instructions.AppendLine("2. SEQUÊNCIA MALUCA");
                instructions.AppendLine("Objetivo: Memorize e reproduza sequências cada vez mais longas.");
                instructions.AppendLine("- Observe a sequência de LEDs que acendem");
                instructions.AppendLine("- Memorize a ordem exata");
                instructions.AppendLine("- Reproduza a sequência pressionando as teclas correspondentes na ordem correta");
                instructions.AppendLine("- A cada acerto, a sequência fica um LED mais longa");
                instructions.AppendLine();

                instructions.AppendLine("3. GATO E RATO");
                instructions.AppendLine("Objetivo: Controle o gato para capturar o rato.");
                instructions.AppendLine("- O gato aparece como um LED sempre aceso");
                instructions.AppendLine("- O rato aparece como um LED piscando");
                instructions.AppendLine("- Mova o gato pressionando as teclas correspondentes");
                instructions.AppendLine("- Posicione o gato na mesma posição do rato para capturá-lo");
                instructions.AppendLine();

                instructions.AppendLine("4. ESQUIVA METEOROS");
                instructions.AppendLine("Objetivo: Mova seu personagem para evitar os meteoros.");
                instructions.AppendLine("- Seu personagem é o LED que permanece aceso");
                instructions.AppendLine("- Meteoros aparecem como LEDs piscando");
                instructions.AppendLine("- Mova-se constantemente para evitar colisões");
                instructions.AppendLine("- Cada meteoro evitado dá pontos");
                instructions.AppendLine();

                instructions.AppendLine("5. GUITAR HERO");
                instructions.AppendLine("Objetivo: Pressione as teclas no tempo certo conforme as notas descem.");
                instructions.AppendLine("- Notas aparecem na parte superior da matriz");
                instructions.AppendLine("- Elas 'descem' linha por linha");
                instructions.AppendLine("- Pressione a tecla quando a nota chegar na linha inferior");
                instructions.AppendLine("- Timing perfeito = mais pontos");
                instructions.AppendLine();

                instructions.AppendLine("6. LIGHTNING STRIKE");
                instructions.AppendLine("Objetivo: Memorize padrões que aparecem por milissegundos.");
                instructions.AppendLine("- Padrão de LEDs pisca ultra-rapidamente");
                instructions.AppendLine("- Memorize quais LEDs acenderam");
                instructions.AppendLine("- Reproduza a sequência exata");
                instructions.AppendLine("- A cada nível: +1 LED e -50ms de tempo");
                instructions.AppendLine("- UM ERRO = GAME OVER instantâneo");
                instructions.AppendLine();

                instructions.AppendLine("7. SNIPER MODE");
                instructions.AppendLine("Objetivo: Atire nos alvos que piscam por apenas 0.1 segundo!");
                instructions.AppendLine("- Alvo aparece aleatoriamente por 0.1 segundo");
                instructions.AppendLine("- Pressione a tecla EXATA enquanto pisca");
                instructions.AppendLine("- Muito lento ou tecla errada = perde pontos");
                instructions.AppendLine("- META: 10 acertos seguidos = VITÓRIA IMPOSSÍVEL");
                instructions.AppendLine("- Completar = Bônus x10 na pontuação");
                instructions.AppendLine();

                instructions.AppendLine("MAPEAMENTO DE TECLAS:");
                instructions.AppendLine("Linha 1 (LEDs 0-3): Teclas W, E, R, T");
                instructions.AppendLine("Linha 2 (LEDs 4-7): Teclas S, D, F, G");
                instructions.AppendLine("Linha 3 (LEDs 8-11): Teclas Y, U, I, O");
                instructions.AppendLine("Linha 4 (LEDs 12-15): Teclas H, J, K, L");

                // Save to desktop
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"MiniJogo_Instrucoes_{timestamp}.txt";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                await File.WriteAllTextAsync(filePath, instructions.ToString());

                // Show confirmation and try to open the file
                var dialog = new Window()
                {
                    Title = "Instruções Salvas",
                    Width = 350,
                    Height = 180,
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
                    Text = "✅ Instruções salvas com sucesso!",
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

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                var openButton = new Button
                {
                    Content = "📁 Abrir Arquivo",
                    Padding = new Avalonia.Thickness(15, 5),
                    CornerRadius = new Avalonia.CornerRadius(5)
                };

                var okButton = new Button
                {
                    Content = "OK",
                    Padding = new Avalonia.Thickness(20, 5),
                    CornerRadius = new Avalonia.CornerRadius(5)
                };

                openButton.Click += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // If opening fails, just close the dialog
                    }
                    dialog.Close();
                };

                okButton.Click += (s, e) => dialog.Close();

                buttonPanel.Children.Add(openButton);
                buttonPanel.Children.Add(okButton);
                panel.Children.Add(buttonPanel);

                dialog.Content = panel;
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                // Simple error dialog
                var errorDialog = new Window()
                {
                    Title = "Erro",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var errorPanel = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 15,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                errorPanel.Children.Add(new TextBlock
                {
                    Text = "❌ Erro ao salvar instruções:",
                    FontWeight = Avalonia.Media.FontWeight.Bold
                });

                errorPanel.Children.Add(new TextBlock
                {
                    Text = ex.Message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });

                var errorOkButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                errorOkButton.Click += (s, e) => errorDialog.Close();
                errorPanel.Children.Add(errorOkButton);

                errorDialog.Content = errorPanel;
                await errorDialog.ShowDialog(this);
            }
        }
    }
}