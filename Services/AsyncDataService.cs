using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;
using miniJogo.Models;

namespace miniJogo.Services
{
    /// <summary>
    /// Serviço de carregamento assíncrono de dados para otimizar performance
    /// </summary>
    public class AsyncDataService
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        /// <summary>
        /// Carrega dados de scores de forma assíncrona
        /// </summary>
        public async Task<List<GameScore>> LoadScoresAsync()
        {
            await _semaphore.WaitAsync(_cancellationTokenSource.Token);
            
            try
            {
                return await Task.Run(async () =>
                {
                    await Task.Delay(50, _cancellationTokenSource.Token); // Simular operação I/O
                    
                    var scoreService = new ScoreService();
                    var scores = scoreService.GetGameScores();
                    
                    return scores;
                }, _cancellationTokenSource.Token);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Carrega dados de usuários de forma assíncrona
        /// </summary>
        public async Task<List<string>> LoadUsersAsync()
        {
            await _semaphore.WaitAsync(_cancellationTokenSource.Token);
            
            try
            {
                return await Task.Run(async () =>
                {
                    await Task.Delay(30, _cancellationTokenSource.Token);
                    
                    var scoreService = new ScoreService();
                    var scores = scoreService.GetGameScores();
                    var users = scores.Select(s => s.PlayerName).Distinct().OrderBy(p => p).ToList();
                    
                    return users;
                }, _cancellationTokenSource.Token);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Carrega configurações do jogo de forma assíncrona
        /// </summary>
        public async Task<Dictionary<string, object>> LoadGameConfigAsync()
        {
            await _semaphore.WaitAsync(_cancellationTokenSource.Token);
            
            try
            {
                return await Task.Run(async () =>
                {
                    await Task.Delay(20, _cancellationTokenSource.Token);
                    
                    var config = new Dictionary<string, object>
                    {
                        ["MaxPlayers"] = 100,
                        ["DefaultGameMode"] = 1,
                        ["AutoSaveEnabled"] = true,
                        ["DebugMode"] = false
                    };
                    
                    return config;
                }, _cancellationTokenSource.Token);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        /// <summary>
        /// Executa operação na UI thread de forma otimizada
        /// </summary>
        public async Task ExecuteOnUIThreadAsync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                action();
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Background);
            }
        }
        
        /// <summary>
        /// Executa operação na UI thread com resultado
        /// </summary>
        public async Task<T> ExecuteOnUIThreadAsync<T>(Func<T> func)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                return func();
            }
            else
            {
                return await Dispatcher.UIThread.InvokeAsync(func, DispatcherPriority.Background);
            }
        }
        
        /// <summary>
        /// Carrega dados em lotes para melhor performance
        /// </summary>
        public async Task<List<T>> LoadDataInBatchesAsync<T>(
            Func<int, int, Task<List<T>>> loadFunction,
            int totalItems,
            int batchSize = 50)
        {
            var result = new List<T>();
            var batches = (int)Math.Ceiling((double)totalItems / batchSize);
            
            for (int i = 0; i < batches; i++)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
                    
                var skip = i * batchSize;
                var take = Math.Min(batchSize, totalItems - skip);
                
                var batch = await loadFunction(skip, take);
                result.AddRange(batch);
                
                // Pequena pausa para não sobrecarregar o sistema
                await Task.Delay(10, _cancellationTokenSource.Token);
            }
            
            return result;
        }
        
        /// <summary>
        /// Pré-carrega dados críticos na inicialização
        /// </summary>
        public async Task PreloadCriticalDataAsync()
        {
            var tasks = new List<Task>
            {
                LoadGameConfigAsync(),
                LoadUsersAsync()
            };
            
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro no pré-carregamento: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Limpa recursos e cancela operações pendentes
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _semaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
        
        /// <summary>
        /// Verifica se há operações em andamento
        /// </summary>
        public bool IsBusy => _semaphore.CurrentCount == 0;
    }
}