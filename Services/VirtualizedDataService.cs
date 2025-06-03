using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using miniJogo.Models;

namespace miniJogo.Services
{
    /// <summary>
    /// Serviço otimizado para carregamento virtualizado de dados grandes
    /// Implementa paginação, cache e carregamento assíncrono
    /// </summary>
    public class VirtualizedDataService
    {
        private readonly ScoreService _scoreService;
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private readonly Dictionary<string, List<ScoreItemViewModel>> _cache = new();
        private readonly int _pageSize;
        private readonly int _maxCacheSize;
        
        private List<GameScore>? _allScores;
        private DateTime _lastCacheRefresh = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public VirtualizedDataService(int pageSize = 50, int maxCacheSize = 10)
        {
            _scoreService = new ScoreService();
            _pageSize = pageSize;
            _maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// Obtém dados paginados de forma assíncrona
        /// </summary>
        public async Task<VirtualizedResult<ScoreItemViewModel>> GetPagedDataAsync(
            int pageIndex, 
            string gameFilter = "", 
            string playerFilter = "",
            CancellationToken cancellationToken = default)
        {
            await _loadSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Verificar se precisa recarregar os dados
                await EnsureDataLoadedAsync(cancellationToken);
                
                if (_allScores == null)
                {
                    return new VirtualizedResult<ScoreItemViewModel>
                    {
                        Items = new List<ScoreItemViewModel>(),
                        TotalCount = 0,
                        PageIndex = pageIndex,
                        PageSize = _pageSize
                    };
                }

                // Aplicar filtros
                var filteredScores = ApplyFilters(_allScores, gameFilter, playerFilter);
                
                // Gerar chave do cache
                var cacheKey = GenerateCacheKey(pageIndex, gameFilter, playerFilter);
                
                // Verificar cache
                if (_cache.TryGetValue(cacheKey, out var cachedItems))
                {
                    return new VirtualizedResult<ScoreItemViewModel>
                    {
                        Items = cachedItems,
                        TotalCount = filteredScores.Count,
                        PageIndex = pageIndex,
                        PageSize = _pageSize
                    };
                }

                // Calcular paginação
                var skip = pageIndex * _pageSize;
                var pagedScores = filteredScores
                    .Skip(skip)
                    .Take(_pageSize)
                    .ToList();

                // Converter para ViewModels com posições corretas
                var items = new List<ScoreItemViewModel>();
                for (int i = 0; i < pagedScores.Count; i++)
                {
                    var globalPosition = skip + i + 1;
                    var viewModel = ScoreItemViewModel.FromGameScore(pagedScores[i], globalPosition);
                    items.Add(viewModel);
                }

                // Adicionar ao cache
                AddToCache(cacheKey, items);

                return new VirtualizedResult<ScoreItemViewModel>
                {
                    Items = items,
                    TotalCount = filteredScores.Count,
                    PageIndex = pageIndex,
                    PageSize = _pageSize
                };
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        /// <summary>
        /// Obtém dados para exibição em lote (útil para scroll rápido)
        /// </summary>
        public async Task<List<ScoreItemViewModel>> GetBatchDataAsync(
            int startIndex, 
            int count,
            string gameFilter = "",
            string playerFilter = "",
            CancellationToken cancellationToken = default)
        {
            await EnsureDataLoadedAsync(cancellationToken);
            
            if (_allScores == null)
                return new List<ScoreItemViewModel>();

            var filteredScores = ApplyFilters(_allScores, gameFilter, playerFilter);
            var batchScores = filteredScores
                .Skip(startIndex)
                .Take(count)
                .ToList();

            var result = new List<ScoreItemViewModel>();
            for (int i = 0; i < batchScores.Count; i++)
            {
                var globalPosition = startIndex + i + 1;
                var viewModel = ScoreItemViewModel.FromGameScore(batchScores[i], globalPosition);
                result.Add(viewModel);
            }

            return result;
        }

        /// <summary>
        /// Obtém contagem total de itens com filtros aplicados
        /// </summary>
        public async Task<int> GetTotalCountAsync(
            string gameFilter = "",
            string playerFilter = "",
            CancellationToken cancellationToken = default)
        {
            await EnsureDataLoadedAsync(cancellationToken);
            
            if (_allScores == null)
                return 0;

            var filteredScores = ApplyFilters(_allScores, gameFilter, playerFilter);
            return filteredScores.Count;
        }

        /// <summary>
        /// Obtém estatísticas rápidas sem carregar todos os dados
        /// </summary>
        public async Task<DataStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureDataLoadedAsync(cancellationToken);
            
            if (_allScores == null || !_allScores.Any())
            {
                return new DataStatistics();
            }

            return await Task.Run(() =>
            {
                var stats = new DataStatistics
                {
                    TotalGames = _allScores.Count,
                    UniquePlayers = _allScores.Select(s => s.PlayerName).Distinct().Count(),
                    UniqueGameModes = _allScores.Select(s => s.GameMode).Distinct().Count(),
                    AverageScore = (int)_allScores.Average(s => s.Score),
                    HighestScore = _allScores.Max(s => s.Score),
                    MostRecentGame = _allScores.Max(s => s.Timestamp),
                    TopPlayer = _allScores
                        .GroupBy(s => s.PlayerName)
                        .OrderByDescending(g => g.Sum(s => s.Score))
                        .FirstOrDefault()?.Key ?? "N/A"
                };

                return stats;
            }, cancellationToken);
        }

        /// <summary>
        /// Força atualização dos dados
        /// </summary>
        public async Task RefreshDataAsync(CancellationToken cancellationToken = default)
        {
            await _loadSemaphore.WaitAsync(cancellationToken);
            
            try
            {
                _allScores = null;
                _cache.Clear();
                _lastCacheRefresh = DateTime.MinValue;
                
                await EnsureDataLoadedAsync(cancellationToken);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        /// <summary>
        /// Limpa o cache manualmente
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Verifica se os dados estão carregados e os carrega se necessário
        /// </summary>
        private async Task EnsureDataLoadedAsync(CancellationToken cancellationToken)
        {
            if (_allScores != null && DateTime.Now - _lastCacheRefresh < _cacheExpiry)
                return;

            _allScores = await Task.Run(async () =>
            {
                await Task.Delay(10, cancellationToken); // Simular operação I/O
                var scores = _scoreService.GetGameScores();
                return scores.OrderByDescending(s => s.Score)
                           .ThenByDescending(s => s.Level)
                           .ThenBy(s => s.Duration)
                           .ToList();
            }, cancellationToken);

            _lastCacheRefresh = DateTime.Now;
        }

        /// <summary>
        /// Aplica filtros aos dados
        /// </summary>
        private static List<GameScore> ApplyFilters(List<GameScore> scores, string gameFilter, string playerFilter)
        {
            var filtered = scores.AsEnumerable();

            if (!string.IsNullOrEmpty(gameFilter))
            {
                filtered = filtered.Where(s => s.GameMode.Equals(gameFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(playerFilter))
            {
                filtered = filtered.Where(s => s.PlayerName.Equals(playerFilter, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.ToList();
        }

        /// <summary>
        /// Gera chave para o cache
        /// </summary>
        private static string GenerateCacheKey(int pageIndex, string gameFilter, string playerFilter)
        {
            return $"{pageIndex}_{gameFilter}_{playerFilter}";
        }

        /// <summary>
        /// Adiciona itens ao cache com controle de tamanho
        /// </summary>
        private void AddToCache(string key, List<ScoreItemViewModel> items)
        {
            if (_cache.Count >= _maxCacheSize)
            {
                // Remove entrada mais antiga (FIFO)
                var oldestKey = _cache.Keys.First();
                _cache.Remove(oldestKey);
            }

            _cache[key] = items;
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            _loadSemaphore?.Dispose();
            _cache.Clear();
        }
    }

    /// <summary>
    /// Resultado de dados virtualizados
    /// </summary>
    public class VirtualizedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageIndex < TotalPages - 1;
        public bool HasPreviousPage => PageIndex > 0;
    }

    /// <summary>
    /// Estatísticas dos dados
    /// </summary>
    public class DataStatistics
    {
        public int TotalGames { get; set; }
        public int UniquePlayers { get; set; }
        public int UniqueGameModes { get; set; }
        public int AverageScore { get; set; }
        public int HighestScore { get; set; }
        public DateTime MostRecentGame { get; set; }
        public string TopPlayer { get; set; } = string.Empty;
    }
}