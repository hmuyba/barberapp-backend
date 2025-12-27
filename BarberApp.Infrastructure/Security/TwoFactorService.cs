using System.Security.Cryptography;
using BarberApp.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace BarberApp.Infrastructure.Security
{
    // ===== MULTI-FACTOR AUTHENTICATION (2FA) =====
    // Adds extra layer of security beyond password
    public class TwoFactorService
    {
        private readonly IConfiguration _configuration;

        public TwoFactorService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateCode()
        {
            var codeLength = int.Parse(_configuration["TwoFactorSettings:CodeLength"] ?? "6");

            // Generate random 6-digit code
            var randomNumber = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, codeLength));
            return randomNumber.ToString($"D{codeLength}");
        }

        public (string code, DateTime expiry) GenerateCodeWithExpiry()
        {
            var code = GenerateCode();
            var expirationMinutes = int.Parse(_configuration["TwoFactorSettings:CodeExpirationMinutes"] ?? "5");
            var expiry = DateTime.UtcNow.AddMinutes(expirationMinutes);

            return (code, expiry);
        }

        public bool ValidateCode(User user, string inputCode)
        {
            // Check if 2FA is enabled
            if (!user.TwoFactorEnabled)
                return true; // Skip validation if 2FA not enabled

            // Check if code exists
            if (string.IsNullOrEmpty(user.TwoFactorCode))
                return false;

            // Check if code expired
            if (user.TwoFactorExpiry == null || user.TwoFactorExpiry < DateTime.UtcNow)
                return false;

            // Check if code matches
            return user.TwoFactorCode == inputCode;
        }

        public string GenerateSecret()
        {
            // Generate a random secret for TOTP (Time-based One-Time Password)
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }
    }
}