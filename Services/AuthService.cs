using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using miniJogo.Models.Auth;

namespace miniJogo.Services
{
    public class AuthService
    {
        private const string ADMIN_CODE = "ADMIN2024";
        private const string CODES_FILE = "client_codes.json";
        private const string USED_CODES_FILE = "used_codes.json";
        
        private List<string> _validCodes;
        private HashSet<string> _usedCodes;

        public AuthService()
        {
            _validCodes = LoadValidCodes();
            _usedCodes = LoadUsedCodes();
        }

        public AuthResult Authenticate(string code, string? name = null)
        {
            // Admin authentication
            if (code.ToUpper() == ADMIN_CODE)
            {
                return new AuthResult
                {
                    Success = true,
                    User = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Administrador",
                        Type = UserType.Admin,
                        Code = code,
                        CreatedAt = DateTime.Now,
                        LastLogin = DateTime.Now
                    },
                    Message = "Login de administrador realizado com sucesso!"
                };
            }

            // Client authentication
            if (string.IsNullOrWhiteSpace(name))
            {
                return new AuthResult
                {
                    Success = false,
                    Message = "Nome é obrigatório para clientes!"
                };
            }

            if (IsValidClientCode(code))
            {
                if (_usedCodes.Contains(code.ToUpper()))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Este código já foi utilizado!"
                    };
                }

                // Mark code as used
                _usedCodes.Add(code.ToUpper());
                SaveUsedCodes();

                return new AuthResult
                {
                    Success = true,
                    User = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name.Trim(),
                        Type = UserType.Client,
                        Code = code.ToUpper(),
                        CreatedAt = DateTime.Now,
                        LastLogin = DateTime.Now
                    },
                    Message = $"Bem-vindo, {name}!"
                };
            }

            return new AuthResult
            {
                Success = false,
                Message = "Código inválido!"
            };
        }

        private bool IsValidClientCode(string code)
        {
            return _validCodes.Contains(code.ToUpper());
        }

        public List<string> GenerateClientCodes(int count)
        {
            var codes = new HashSet<string>(); // Use HashSet to prevent duplicates
            var maxAttempts = count * 10; // Prevent infinite loops
            var attempts = 0;
            
            while (codes.Count < count && attempts < maxAttempts)
            {
                var code = GenerateRandomCode();
                if (!_validCodes.Contains(code) && !codes.Contains(code))
                {
                    codes.Add(code);
                }
                attempts++;
            }

            var codesList = codes.ToList();
            
            // Add to valid codes and save
            _validCodes.AddRange(codesList);
            SaveValidCodes();

            return codesList;
        }

        private string GenerateRandomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var code = new StringBuilder();

            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[6];
                rng.GetBytes(bytes);

                // Generate 6-character code with specific pattern: 2 letters + 4 numbers
                for (int i = 0; i < 2; i++)
                {
                    code.Append(chars[bytes[i] % 26]); // Letters only
                }
                for (int i = 2; i < 6; i++)
                {
                    code.Append(chars[26 + (bytes[i] % 10)]); // Numbers only
                }
            }

            return code.ToString();
        }

        public string GenerateSecureCode()
        {
            // Use the same secure method as GenerateRandomCode for consistency
            return GenerateRandomCode();
        }

        private List<string> LoadValidCodes()
        {
            try
            {
                if (File.Exists(CODES_FILE))
                {
                    var json = File.ReadAllText(CODES_FILE);
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar códigos válidos: {ex.Message}");
            }

            return new List<string>();
        }

        private HashSet<string> LoadUsedCodes()
        {
            try
            {
                if (File.Exists(USED_CODES_FILE))
                {
                    var json = File.ReadAllText(USED_CODES_FILE);
                    var codes = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    return new HashSet<string>(codes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar códigos usados: {ex.Message}");
            }

            return new HashSet<string>();
        }

        private void SaveValidCodes()
        {
            try
            {
                var json = JsonSerializer.Serialize(_validCodes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CODES_FILE, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar códigos válidos: {ex.Message}");
            }
        }

        private void SaveUsedCodes()
        {
            try
            {
                var json = JsonSerializer.Serialize(_usedCodes.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(USED_CODES_FILE, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar códigos usados: {ex.Message}");
            }
        }

        public void SaveCodesToFile(List<string> codes, string fileName = "bilhetes_jogo.txt")
        {
            try
            {
                var content = new StringBuilder();
                content.AppendLine("=== BILHETES DE JOGO ===");
                content.AppendLine($"Gerados em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                content.AppendLine($"Total de códigos: {codes.Count}");
                content.AppendLine(new string('-', 50));
                content.AppendLine();

                for (int i = 0; i < codes.Count; i++)
                {
                    content.AppendLine($"Bilhete #{i + 1:D4}: {codes[i]}");
                    
                    // Add separator every 10 codes for easy cutting
                    if ((i + 1) % 10 == 0)
                    {
                        content.AppendLine(new string('-', 30));
                    }
                }

                File.WriteAllText(fileName, content.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar bilhetes: {ex.Message}");
            }
        }

        public int GetTotalValidCodes() => _validCodes.Count;
        public int GetUsedCodesCount() => _usedCodes.Count;
        public int GetAvailableCodesCount() => _validCodes.Count - _usedCodes.Count;
    }
}