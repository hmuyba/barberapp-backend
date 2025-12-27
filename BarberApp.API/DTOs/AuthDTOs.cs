namespace BarberApp.API.DTOs
{
    // ===== REGISTRATION DTOs =====
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; } = 1; // Default: Client
        public bool EnableTwoFactor { get; set; } = false;
    }

    public class RegisterResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool TwoFactorEnabled { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ===== LOGIN DTOs =====
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool RequiresTwoFactor { get; set; }
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ===== TWO-FACTOR DTOs =====
    public class VerifyTwoFactorRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyTwoFactorResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // ===== USER INFO DTO =====
    public class UserInfo
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public bool IsVIP { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public int? BarberId { get; set; }
        public bool IsManager { get; set; }
    }
}