using System;

namespace miniJogo.Models.Auth
{
    public enum UserType
    {
        Admin,
        Client
    }

    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public UserType Type { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public User? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}