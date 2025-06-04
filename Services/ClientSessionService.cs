using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using miniJogo.Models;
using miniJogo.Models.Auth;

namespace miniJogo.Services
{
    public class ClientSessionService : IDisposable
    {
        private const string SESSIONS_FILE = "client_sessions.json";
        private Dictionary<string, ClientSession> _activeSessions = new();
        private readonly ReaderWriterLockSlim _sessionsLock = new ReaderWriterLockSlim();
        private readonly object _fileLock = new object();

        public ClientSessionService()
        {
            LoadSessions();
        }

        public ClientSession CreateSession(User user, GameMode selectedGame)
        {
            if (user.Type != UserType.Client)
                throw new ArgumentException("Apenas clientes podem ter sessões limitadas");

            var session = new ClientSession(selectedGame)
            {
                ClientId = user.Id,
                ClientName = user.Name,
                SessionStart = DateTime.Now,
                IsActive = true
            };

            _sessionsLock.EnterWriteLock();
            try
            {
                _activeSessions[user.Id] = session;
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }

            SaveSessions();
            return session;
        }

        public ClientSession? GetSession(string clientId)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _activeSessions.ContainsKey(clientId) ? _activeSessions[clientId] : null;
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public bool CanClientPlayGame(string clientId)
        {
            var session = GetSession(clientId);
            return session?.CanPlayGame() ?? false;
        }

        public void RecordGameError(string clientId)
        {
            _sessionsLock.EnterWriteLock();
            try
            {
                if (_activeSessions.ContainsKey(clientId))
                {
                    _activeSessions[clientId].RecordError();
                }
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }
            
            SaveSessions();
        }

        public int GetRemainingRounds(string clientId)
        {
            var session = GetSession(clientId);
            return session?.GetRemainingRounds() ?? 0;
        }

        public bool HasClientReachedErrorLimit(string clientId)
        {
            var session = GetSession(clientId);
            return session?.HasReachedErrorLimit() ?? false;
        }

        public bool ShouldEndClientSession(string clientId)
        {
            var session = GetSession(clientId);
            if (session == null || !session.IsActive)
                return false;

            // Check if client has exhausted all chances in the selected game
            return session.IsSessionComplete();
        }

        public GameMode GetClientSelectedGame(string clientId)
        {
            var session = GetSession(clientId);
            return session?.SelectedGame ?? GameMode.Menu;
        }

        public string GetClientSessionStatus(string clientId)
        {
            var session = GetSession(clientId);
            return session?.GetSessionStatus() ?? "Sessão não encontrada";
        }

        public void EndSession(string clientId)
        {
            _sessionsLock.EnterWriteLock();
            try
            {
                if (_activeSessions.ContainsKey(clientId))
                {
                    _activeSessions[clientId].IsActive = false;
                }
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }
            
            SaveSessions();
        }

        public void EndSessionByTimeout(string clientId, string reason = "Timeout no jogo")
        {
            _sessionsLock.EnterWriteLock();
            try
            {
                if (_activeSessions.ContainsKey(clientId))
                {
                    var session = _activeSessions[clientId];
                    session.IsActive = false;
                    // Mark session as ended by timeout for business logic
                    session.ErrorsCommitted = session.MaxErrors; // Force max errors to prevent restart
                    Console.WriteLine($"Sessão do cliente {session.ClientName} finalizada por: {reason}");
                }
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }
            
            SaveSessions();
        }

        public bool IsSessionBlockedByTimeout(string clientId)
        {
            var session = GetSession(clientId);
            if (session == null) return false;
            
            // If session is inactive and has max errors, it was likely ended by timeout
            return !session.IsActive && session.ErrorsCommitted >= session.MaxErrors;
        }

        public Dictionary<GameMode, int> GetClientGameSummary(string clientId)
        {
            _sessionsLock.EnterReadLock();
            try
            {
                if (!_activeSessions.ContainsKey(clientId))
                    return new Dictionary<GameMode, int>();

                var session = _activeSessions[clientId];
                var summary = new Dictionary<GameMode, int>();
                // For single game sessions, show only the selected game
                summary[session.SelectedGame] = session.ErrorsCommitted;
                return summary;
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public void CleanupExpiredSessions(TimeSpan maxAge)
        {
            List<string> expiredSessionIds = new List<string>();
            
            _sessionsLock.EnterWriteLock();
            try
            {
                var expiredSessions = _activeSessions.Values
                    .Where(s => DateTime.Now - s.SessionStart > maxAge)
                    .ToList();

                foreach (var session in expiredSessions)
                {
                    _activeSessions.Remove(session.ClientId);
                    expiredSessionIds.Add(session.ClientId);
                }
            }
            finally
            {
                _sessionsLock.ExitWriteLock();
            }

            if (expiredSessionIds.Any())
            {
                SaveSessions();
            }
        }

        private void LoadSessions()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(SESSIONS_FILE))
                    {
                        string json;
                        // Retry mechanism for file access
                        for (int retry = 0; retry < 3; retry++)
                        {
                            try
                            {
                                json = File.ReadAllText(SESSIONS_FILE);
                                break;
                            }
                            catch (IOException) when (retry < 2)
                            {
                                Thread.Sleep(100); // Wait 100ms before retry
                                continue;
                            }
                        }
                        
                        json = File.ReadAllText(SESSIONS_FILE);
                        var sessions = JsonSerializer.Deserialize<Dictionary<string, ClientSession>>(json);
                        
                        _sessionsLock.EnterWriteLock();
                        try
                        {
                            if (sessions != null)
                            {
                                _activeSessions = sessions;
                            }
                        }
                        finally
                        {
                            _sessionsLock.ExitWriteLock();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao carregar sessões: {ex.Message}");
                }
            }
        }

        private void SaveSessions()
        {
            lock (_fileLock)
            {
                try
                {
                    Dictionary<string, ClientSession> sessionsCopy;
                    
                    // Create a copy of sessions under read lock
                    _sessionsLock.EnterReadLock();
                    try
                    {
                        sessionsCopy = new Dictionary<string, ClientSession>(_activeSessions);
                    }
                    finally
                    {
                        _sessionsLock.ExitReadLock();
                    }

                    var json = JsonSerializer.Serialize(sessionsCopy, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });

                    // Retry mechanism for file write
                    for (int retry = 0; retry < 3; retry++)
                    {
                        try
                        {
                            File.WriteAllText(SESSIONS_FILE, json);
                            break;
                        }
                        catch (IOException) when (retry < 2)
                        {
                            Thread.Sleep(100); // Wait 100ms before retry
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao salvar sessões: {ex.Message}");
                }
            }
        }

        public int GetActiveSessionsCount()
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _activeSessions.Values.Count(s => s.IsActive);
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public List<ClientSession> GetAllActiveSessions()
        {
            _sessionsLock.EnterReadLock();
            try
            {
                return _activeSessions.Values.Where(s => s.IsActive).ToList();
            }
            finally
            {
                _sessionsLock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Save any pending session data
                    SaveSessions();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao salvar sessões durante dispose: {ex.Message}");
                }

                _sessionsLock?.Dispose();
            }
        }

        ~ClientSessionService()
        {
            Dispose(false);
        }
    }
}