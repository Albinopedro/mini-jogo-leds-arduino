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

                InitializeGameMode(gameMode);
            }
            catch (Exception)
            {
                _isGameRunning = false;
            }
        }

        public void StopGame()
        {
            _isGameRunning = false;
        }

        public void PauseGame()
        {
            _isGameRunning = false;
        }

        public void ResumeGame()
        {
            _isGameRunning = true;
        }

        public void UpdateGame()
        {
            if (!_isGameRunning) return;

            try
            {
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
                }
            }
            catch (Exception)
            {
                // Silent error handling to avoid performance impact
            }
        }

        public void HandleInput(string input)
        {
            if (!_isGameRunning) return;

            try
            {
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
                }
            }
            catch (Exception)
            {
                // Silent error handling
            }
        }

        public void HandleKeyUp(string key)
        {
            if (!_isGameRunning) return;
            // Handle key up events if needed for specific games
        }

        private void InitializeGameMode(GameMode gameMode)
        {
            // Initialize specific game mode settings
            switch (gameMode)
            {
                case GameMode.PegaLuz:
                case GameMode.SequenciaMaluca:
                case GameMode.GatoRato:
                case GameMode.EsquivaMeteoros:
                case GameMode.GuitarHero:
                case GameMode.LightningStrike:
                    // Game initialization handled by Arduino
                    break;
            }
        }

        // Game-specific update methods - optimized for performance
        private void UpdatePegaLuz() { }
        private void UpdateSequenciaMaluca() { }
        private void UpdateGatoRato() { }
        private void UpdateEsquivaMeteoros() { }
        private void UpdateGuitarHero() { }
        private void UpdateLightningStrike() { }

        // Game-specific input handlers - optimized for performance
        private void HandlePegaLuzInput(string input)
        {
            if (input == "space")
            {
                _currentScore += 10;
            }
        }

        private void HandleSequenciaMalucaInput(string input) { }
        private void HandleGatoRatoInput(string input) { }
        private void HandleEsquivaMeterosInput(string input) { }
        private void HandleGuitarHeroInput(string input) { }
        private void HandleLightningStrikeInput(string input) { }

        public void AddScore(int points)
        {
            _currentScore += points;
        }

        public void NextLevel()
        {
            _currentLevel++;
        }

        public void ResetGame()
        {
            _currentScore = 0;
            _currentLevel = 1;
            _isGameRunning = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopGame();
                _disposed = true;
            }
        }
    }
}