namespace BarberApp.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // Sensitive data (for MAC - Mandatory Access Control)
        public string? SensitiveData { get; set; } // JSON with medical info, allergies, etc.
        public bool IsVIP { get; set; } = false; // For MAC control

        // Two-Factor Authentication (2FA)
        public bool TwoFactorEnabled { get; set; } = false;
        public string? TwoFactorSecret { get; set; }
        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorExpiry { get; set; }

        // Role relationship
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        // Barber relationship (if user is a barber)
        public Barber? Barber { get; set; }

        // Appointments as client
        public ICollection<Appointment> AppointmentsAsClient { get; set; } = new List<Appointment>();

        // Audit (Integrity - CIA)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }
}