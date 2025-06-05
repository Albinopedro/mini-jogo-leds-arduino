using System;
using System.Threading.Tasks;
using miniJogo.Models;

namespace miniJogo.Services
{
    public class GameService : IDisposable
    {
        private GameMode _currentGameMode;
        private bool _isGameRunning;
        private int _currentScore;
        private int _currentLevel;
        private bool _disposed;

        public GameService()
        {
            _currentGameMode = GameMode.Menu;
            _isGameRunning = false;
            _currentScore = 0;
            _currentLevel = 1;
        }

        public bool IsGameRunning => _isGameRunning;
        public int CurrentScore => _currentScore;
        public int CurrentLevel => _currentLevel;
        public GameMode CurrentGameMode => _currentGameMode;

        public void StartGame(GameMode gameMode)
        {
            try
            {
                _currentGameMode = gameMode;
                _isGameRunning = true;
                _currentScore = 0;
                _currentLevel = 1;
                
                System.Diagnostics.Debug.WriteLine($"Game started with mode: {gameMode}");
                
                // Initialize game based on mode
                InitializeGameMode(gameMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting game: {ex.Message}");
                _isGameRunning = false;
            }
        }

        public void StopGame()
        {
            try
            {
                _isGameRunning = false;
                System.Diagnostics.Debug.WriteLine("Game stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping game: {ex.Message}");
            }
        }

        public void PauseGame()
        {
            try
            {
                _isGameRunning = false;
                System.Diagnostics.Debug.WriteLine("Game paused");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error pausing game: {ex.Message}");
            }
        }

        public void ResumeGame()
        {
            try
            {
                _isGameRunning = true;
                System.Diagnostics.Debug.WriteLine("Game resumed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resuming game: {ex.Message}");
            }
        }

        public void UpdateGame()
        {
            try
            {
                if (!_isGameRunning) return;

                // Update game logic based on current mode
                switch (_currentGameMode)
                {
                    case GameMode.PegaLuz:
                        UpdatePegaLuz();
                        break;
                    case GameMode.SequenciaMaluca:
                        UpdateSequenciaMaluca();
                        break;
                    case GameMode.GatoRato:
                        UpdateGatoRato();
                        break;
                    case GameMode.EsquivaMeteoros:
                        UpdateEsquivaMeteoros();
                        break;
                    case GameMode.GuitarHero:
                        UpdateGuitarHero();
                        break;

                    case GameMode.LightningStrike:
                        UpdateLightningStrike();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating game: {ex.Message}");
            }
        }

        public void HandleInput(string input)
        {
            try
            {
                if (!_isGameRunning) return;

                System.Diagnostics.Debug.WriteLine($"Handling input: {input}");
                
                // Process input based on current game mode
                switch (_currentGameMode)
                {
                    case GameMode.PegaLuz:
                        HandlePegaLuzInput(input);
                        break;
                    case GameMode.SequenciaMaluca:
                        HandleSequenciaMalucaInput(input);
                        break;
                    case GameMode.GatoRato:
                        HandleGatoRatoInput(input);
                        break;
                    case GameMode.EsquivaMeteoros:
                        HandleEsquivaMeterosInput(input);
                        break;
                    case GameMode.GuitarHero:
                        HandleGuitarHeroInput(input);
                        break;

                    case GameMode.LightningStrike:
                        HandleLightningStrikeInput(input);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling input: {ex.Message}");
            }
        }

        public void HandleKeyUp(string key)
        {
            try
            {
                if (!_isGameRunning) return;
                
                System.Diagnostics.Debug.WriteLine($"Key up: {key}");
                // Handle key up events if needed for specific games
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling key up: {ex.Message}");
            }
        }

        private void InitializeGameMode(GameMode gameMode)
        {
            System.Diagnostics.Debug.WriteLine($"Initializing game mode: {gameMode}");
            
            // Initialize specific game mode settings
            switch (gameMode)
            {
                case GameMode.PegaLuz:
                    // Initialize Pega Luz game
                    break;
                case GameMode.SequenciaMaluca:
                    // Initialize Sequencia Maluca game
                    break;
                case GameMode.GatoRato:
                    // Initialize Gato Rato game
                    break;
                case GameMode.EsquivaMeteoros:
                    // Initialize Esquiva Meteoros game
                    break;
                case GameMode.GuitarHero:
                    // Initialize Guitar Hero game
                    break;

                case GameMode.LightningStrike:
                    // Initialize Lightning Strike game
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown game mode: {gameMode}");
                    break;
            }
        }

        // Game-specific update methods
        private void UpdatePegaLuz()
        {
            // Update Pega Luz game logic
        }

        private void UpdateSequenciaMaluca()
        {
            // Update Sequencia Maluca game logic
        }

        private void UpdateGatoRato()
        {
            // Update Gato Rato game logic
        }

        private void UpdateEsquivaMeteoros()
        {
            // Update Esquiva Meteoros game logic
        }

        private void UpdateGuitarHero()
        {
            // Update Guitar Hero game logic
        }



        private void UpdateLightningStrike()
        {
            // Update Lightning Strike game logic
        }

        // Game-specific input handlers
        private void HandlePegaLuzInput(string input)
        {
            // Handle Pega Luz input
            if (input == "space")
            {
                _currentScore += 10;
            }
        }

        private void HandleSequenciaMalucaInput(string input)
        {
            // Handle Sequencia Maluca input
        }

        private void HandleGatoRatoInput(string input)
        {
            // Handle Gato Rato input
        }

        private void HandleEsquivaMeterosInput(string input)
        {
            // Handle Esquiva Meteoros input
        }

        private void HandleGuitarHeroInput(string input)
        {
            // Handle Guitar Hero input
        }



        private void HandleLightningStrikeInput(string input)
        {
            // Handle Lightning Strike input
        }

        public void AddScore(int points)
        {
            _currentScore += points;
            System.Diagnostics.Debug.WriteLine($"Score updated: {_currentScore}");
        }

        public void NextLevel()
        {
            _currentLevel++;
            System.Diagnostics.Debug.WriteLine($"Level up: {_currentLevel}");
        }

        public void ResetGame()
        {
            _currentScore = 0;
            _currentLevel = 1;
            _isGameRunning = false;
            System.Diagnostics.Debug.WriteLine("Game reset");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    StopGame();
                    System.Diagnostics.Debug.WriteLine("GameService disposed");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing GameService: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}