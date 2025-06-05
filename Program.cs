using Avalonia;
using System;
using System.Threading.Tasks;
using miniJogo.Services;

namespace miniJogo;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Gerar arquivos de áudio temporários se não existirem
        Task.Run(async () =>
        {
            try
            {
                await AudioTempGenerator.GenerateAllTempAudioFilesAsync();
                Console.WriteLine("🎵 Arquivos de áudio temporários gerados com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao gerar arquivos de áudio: {ex.Message}");
            }
        });

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
