using System;
using System.Collections.Generic;

namespace miniJogo.Models
{
    public class ClientSession
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
        public GameMode SelectedGame { get; set; } = GameMode.Menu;
        public int ErrorsCommitted { get; set; } = 0;
        public int MaxErrors { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        public ClientSession()
        {
            SessionStart = DateTime.Now;
        }

        public ClientSession(GameMode selectedGame)
        {
            SessionStart = DateTime.Now;
            SelectedGame = selectedGame;
            MaxErrors = GetMaxErrorsForGame(selectedGame);
        }

        private int GetMaxErrorsForGame(GameMode gameMode)
        {
            return gameMode switch
            {
                GameMode.PegaLuz => 3,
                GameMode.GatoRato => 3, // Corrigido: era 4, agora 3
                GameMode.SequenciaMaluca => 3,
                GameMode.EsquivaMeteoros => 3,
                GameMode.GuitarHero => 3,
                GameMode.RoletaRussa => 3,
                GameMode.LightningStrike => 3,
                GameMode.SniperMode => 3, // Corrigido: era 4, agora 3
                _ => 3
            };
        }

        public bool CanPlayGame()
        {
            if (!IsActive || SelectedGame == GameMode.Menu)
                return false;

            return GetRemainingRounds() > 0;
        }

        public int GetRemainingRounds()
        {
            return Math.Max(0, MaxErrors - ErrorsCommitted);
        }

        public void RecordError()
        {
            ErrorsCommitted++;
        }

        public bool HasReachedErrorLimit()
        {
            return ErrorsCommitted >= MaxErrors;
        }

        public bool IsSessionComplete()
        {
            return HasReachedErrorLimit();
        }

        public string GetGameDisplayName()
        {
            return SelectedGame.GetDisplayName();
        }

        public string GetGameIcon()
        {
            return SelectedGame.GetIcon();
        }

        public string GetSessionStatus()
        {
            var remaining = GetRemainingRounds();
            var gameIcon = GetGameIcon();
            var gameName = GetGameDisplayName();

            if (remaining > 0)
            {
                return $"{gameIcon} {gameName} - Erros: {ErrorsCommitted}/{MaxErrors} (Restam: {remaining})";
            }
            else
            {
                return $"{gameIcon} {gameName} - Sessão finalizada! ({ErrorsCommitted}/{MaxErrors})";
            }
        }
    }
}