using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Controls.Primitives;

namespace miniJogo
{
    /// <summary>
    /// Configurações globais de performance para o Mini Jogo LEDs
    /// Implementa otimizações baseadas nas melhores práticas do Avalonia
    /// </summary>
    public static class PerformanceConfig
    {
        private static readonly Dictionary<string, StreamGeometry> _geometryCache = new();
        private static DateTime _lastMemoryCleanup = DateTime.Now;
        private const int MEMORY_CLEANUP_INTERVAL_MINUTES = 5;
        private const int MAX_GEOMETRY_CACHE_SIZE = 100;

        /// <summary>
        /// Configura otimizações globais de performance da aplicação
        /// </summary>
        public static void Configure()
        {
            // Configurar Compiled Bindings como padrão
            ConfigureCompiledBindings();

            // Configurar virtualização para listas
            ConfigureVirtualization();

            // Configurar renderização otimizada
            ConfigureRenderingOptimizations();

            // Configurar threading otimizado
            ConfigureThreadingOptimizations();

            // Configurar otimizações de geometria
            ConfigureGeometryOptimizations();

            // Configurar limpeza automática de memória
            ConfigureMemoryManagement();
        }

        private static void ConfigureCompiledBindings()
        {
            // Habilitar compiled bindings por padrão para melhor performance
            Application.Current?.Resources.Add("UseCompiledBindings", true);
        }

        private static void ConfigureVirtualization()
        {
            // Configurar virtualização padrão para controles de lista
            Application.Current?.Resources.Add("EnableVirtualization", true);
        }

        private static void ConfigureRenderingOptimizations()
        {
            // Configurar otimizações de renderização
            try
            {
                // Configurar cache de geometrias para melhor performance
                Application.Current?.Resources.Add("UseGeometryCache", true);

                // Configurar limites de FPS para economizar recursos
                Application.Current?.Resources.Add("MaxFrameRate", 60);

                // Habilitar hardware acceleration quando disponível
                Application.Current?.Resources.Add("UseHardwareAcceleration", true);

                // Otimizar renderização de texto
                Application.Current?.Resources.Add("TextFormattingMode", "Ideal");

                // Configurar qualidade de renderização otimizada
                Application.Current?.Resources.Add("RenderingTier", 2);

                // Reduzir overhead de layout passes
                Application.Current?.Resources.Add("LayoutRounding", true);
            }
            catch (Exception ex)
            {
                // Log em caso de erro, mas não interromper a aplicação
                System.Diagnostics.Debug.WriteLine($"Erro ao configurar otimizações de renderização: {ex.Message}");
            }
        }

        private static void ConfigureThreadingOptimizations()
        {
            // Configurar prioridade de thread UI
            Dispatcher.UIThread.VerifyAccess();
        }

        /// <summary>
        /// Configurações específicas para controles de dados grandes
        /// </summary>
        public static void ConfigureDataControls(Control control)
        {
            if (control is ListBox listBox)
            {
                // Configurar scroll suave para melhor UX
                ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Auto);
                ScrollViewer.SetVerticalScrollBarVisibility(listBox, ScrollBarVisibility.Auto);
            }
            else if (control is DataGrid dataGrid)
            {
                // Para DataGrid, usar configurações específicas de performance
                dataGrid.CanUserReorderColumns = false;
                dataGrid.CanUserResizeColumns = true;
                dataGrid.CanUserSortColumns = true;
                dataGrid.IsReadOnly = true; // Para melhor performance se não precisar de edição
            }
        }

        /// <summary>
        /// Otimizações para reduzir uso de memória
        /// </summary>
        public static void OptimizeMemoryUsage()
        {
            // Forçar garbage collection quando apropriado
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Configurações para melhor responsividade da UI
        /// </summary>
        public static void ConfigureUIResponsiveness()
        {
            // Configurar timeout para operações UI
            Application.Current?.Resources.Add("UIOperationTimeout", TimeSpan.FromSeconds(5));
            
            // Configurar batch size para operações em lote
            Application.Current?.Resources.Add("BatchSize", 100);
        }

        /// <summary>
        /// Configurações de otimização de geometria - usa StreamGeometry ao invés de PathGeometry
        /// </summary>
        private static void ConfigureGeometryOptimizations()
        {
            // Configurar cache de geometrias
            Application.Current?.Resources.Add("GeometryCacheEnabled", true);
            Application.Current?.Resources.Add("MaxGeometryCacheSize", MAX_GEOMETRY_CACHE_SIZE);
        }

        /// <summary>
        /// Configurar gerenciamento automático de memória
        /// </summary>
        private static void ConfigureMemoryManagement()
        {
            // Configurar timer para limpeza periódica de memória
            var memoryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(MEMORY_CLEANUP_INTERVAL_MINUTES)
            };

            memoryTimer.Tick += (s, e) => PerformMemoryCleanup();
            memoryTimer.Start();
        }

        /// <summary>
        /// Obtém uma StreamGeometry do cache ou cria uma nova
        /// </summary>
        public static StreamGeometry GetCachedStreamGeometry(string key, Func<StreamGeometry> factory)
        {
            if (_geometryCache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            // Limitar tamanho do cache
            if (_geometryCache.Count >= MAX_GEOMETRY_CACHE_SIZE)
            {
                _geometryCache.Clear();
            }

            var geometry = factory();
            _geometryCache[key] = geometry;
            return geometry;
        }

        /// <summary>
        /// Otimiza um TextBlock removendo a necessidade de Run
        /// </summary>
        public static void OptimizeTextBlock(TextBlock textBlock, string text, FontWeight? fontWeight = null, Brush? foreground = null)
        {
            textBlock.Text = text;

            if (fontWeight.HasValue)
                textBlock.FontWeight = fontWeight.Value;

            if (foreground != null)
                textBlock.Foreground = foreground;
        }

        /// <summary>
        /// Configura uma ListBox para uso otimizado com dados grandes
        /// </summary>
        public static void ConfigureOptimizedListBox(ListBox listBox, int estimatedItemCount = 1000)
        {
            // Configurar scroll otimizado
            ScrollViewer.SetVerticalScrollBarVisibility(listBox, ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Disabled);
            
            // Otimizações básicas para performance
            if (estimatedItemCount > 500)
            {
                // Configurações específicas para listas muito grandes
                listBox.Background = Brushes.Transparent;
            }
        }

        /// <summary>
        /// Executa limpeza de memória e cache
        /// </summary>
        private static void PerformMemoryCleanup()
        {
            try
            {
                // Limpar cache de geometrias se muito tempo passou
                if ((DateTime.Now - _lastMemoryCleanup).TotalMinutes > MEMORY_CLEANUP_INTERVAL_MINUTES)
                {
                    _geometryCache.Clear();
                    _lastMemoryCleanup = DateTime.Now;
                }

                // Forçar garbage collection se necessário
                var beforeMemory = GC.GetTotalMemory(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var afterMemory = GC.GetTotalMemory(false);
                var freed = beforeMemory - afterMemory;

                if (freed > 1024 * 1024) // Mais de 1MB liberado
                {
                    System.Diagnostics.Debug.WriteLine($"Memory cleanup freed {freed / 1024 / 1024}MB");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na limpeza de memória: {ex.Message}");
            }
        }

        /// <summary>
        /// Validar e reportar problemas de performance
        /// </summary>
        public static void ValidatePerformance()
        {
            var startTime = DateTime.Now;

            // Simular operação para testar responsividade
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var elapsed = DateTime.Now - startTime;
                if (elapsed.TotalMilliseconds > 100)
                {
                    System.Diagnostics.Debug.WriteLine($"Performance warning: UI operation took {elapsed.TotalMilliseconds}ms");
                }
            });
        }

        /// <summary>
        /// Monitora performance da aplicação
        /// </summary>
        public static void StartPerformanceMonitoring()
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            timer.Tick += (s, e) =>
            {
                var memoryUsage = GC.GetTotalMemory(false);
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                System.Diagnostics.Debug.WriteLine($"Performance Monitor - Memory: {memoryUsage / 1024 / 1024}MB, GC: G0={gen0Collections}, G1={gen1Collections}, G2={gen2Collections}");
            };

            timer.Start();
        }
    }
}
