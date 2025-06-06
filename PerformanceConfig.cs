using System;

namespace miniJogo
{
    /// <summary>
    /// Configurações de performance otimizadas para melhor responsividade
    /// </summary>
    public static class PerformanceConfig
    {
        // ===== CONFIGURAÇÕES DE COMUNICAÇÃO SERIAL =====
        
        /// <summary>
        /// Timeout reduzido para leitura serial (ms)
        /// </summary>
        public const int SerialReadTimeout = 300;
        
        /// <summary>
        /// Timeout reduzido para escrita serial (ms)
        /// </summary>
        public const int SerialWriteTimeout = 300;
        
        /// <summary>
        /// Tempo de espera para inicialização do Arduino (ms)
        /// </summary>
        public const int ArduinoInitDelay = 800;
        
        /// <summary>
        /// Intervalo mínimo entre comandos seriais (ms)
        /// </summary>
        public const int MinCommandInterval = 50;

        // ===== CONFIGURAÇÕES DE DEBUG =====
        
        /// <summary>
        /// Habilita apenas logs críticos (erros e game over)
        /// </summary>
        public const bool EnableCriticalLogsOnly = true;
        
        /// <summary>
        /// Habilita logs de debug detalhados (desabilitado para performance)
        /// </summary>
        public const bool EnableVerboseLogging = false;
        
        /// <summary>
        /// Habilita logs de comunicação serial (desabilitado para performance)
        /// </summary>
        public const bool EnableSerialLogging = false;

        // ===== CONFIGURAÇÕES DE UI =====
        
        /// <summary>
        /// Intervalo de atualização da UI (ms)
        /// </summary>
        public const int UIUpdateInterval = 100;
        
        /// <summary>
        /// Intervalo reduzido para timer de status (ms)
        /// </summary>
        public const int StatusTimerInterval = 500;
        
        /// <summary>
        /// Máximo de mensagens de debug na janela de debug
        /// </summary>
        public const int MaxDebugMessages = 100;

        // ===== CONFIGURAÇÕES DE JOGO =====
        
        /// <summary>
        /// Timeout para highlights de LED (ms)
        /// </summary>
        public const int LedHighlightTimeout = 300;
        
        /// <summary>
        /// Intervalo para limpeza automática de timers (ms)
        /// </summary>
        public const int TimerCleanupInterval = 5000;
        
        /// <summary>
        /// Máximo de timers de LED simultâneos
        /// </summary>
        public const int MaxLedTimers = 16;

        // ===== CONFIGURAÇÕES DE AUDIO =====
        
        /// <summary>
        /// Habilita reprodução de áudio otimizada
        /// </summary>
        public const bool EnableOptimizedAudio = true;
        
        /// <summary>
        /// Timeout para carregamento de arquivos de áudio (ms)
        /// </summary>
        public const int AudioLoadTimeout = 2000;

        // ===== MÉTODOS UTILITÁRIOS =====
        
        /// <summary>
        /// Verifica se uma mensagem deve ser logada baseada nas configurações de performance
        /// </summary>
        /// <param name="message">Mensagem a ser verificada</param>
        /// <param name="isDebug">Se é uma mensagem de debug</param>
        /// <returns>True se deve ser logada</returns>
        public static bool ShouldLog(string message, bool isDebug = false)
        {
            if (!EnableVerboseLogging && isDebug)
                return false;
                
            if (EnableCriticalLogsOnly)
            {
                return message.Contains("ERRO") || 
                       message.Contains("ERROR") || 
                       message.Contains("GAME_OVER") ||
                       message.Contains("CRITICAL") ||
                       message.Contains("EXCEPTION");
            }
            
            return !EnableCriticalLogsOnly;
        }
        
        /// <summary>
        /// Verifica se logs de comunicação serial devem ser habilitados
        /// </summary>
        /// <returns>True se deve logar comunicação serial</returns>
        public static bool ShouldLogSerial()
        {
            return EnableSerialLogging && !EnableCriticalLogsOnly;
        }
        
        /// <summary>
        /// Obtém o delay otimizado baseado no tipo de operação
        /// </summary>
        /// <param name="operationType">Tipo de operação</param>
        /// <returns>Delay em milissegundos</returns>
        public static int GetOptimizedDelay(string operationType)
        {
            return operationType.ToLower() switch
            {
                "led_highlight" => LedHighlightTimeout,
                "arduino_init" => ArduinoInitDelay,
                "command_interval" => MinCommandInterval,
                "ui_update" => UIUpdateInterval,
                _ => 100
            };
        }
        
        /// <summary>
        /// Configurações para modo de alta performance (menos logs, timeouts menores)
        /// </summary>
        public static void EnableHighPerformanceMode()
        {
            // Esta configuração é aplicada estaticamente via constantes
            // Em uma implementação mais avançada, poderia usar propriedades dinâmicas
        }
        
        /// <summary>
        /// Verifica se o sistema está em modo de alta performance
        /// </summary>
        /// <returns>True se está em modo de alta performance</returns>
        public static bool IsHighPerformanceMode()
        {
            return EnableCriticalLogsOnly && !EnableVerboseLogging;
        }
    }
}