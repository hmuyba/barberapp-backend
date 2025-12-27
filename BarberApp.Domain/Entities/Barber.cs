namespace BarberApp.Domain.Entities
{
    public class Barber
    {
        public int Id { get; set; }

        // User relationship (a barber IS a user)
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Barber-specific data
        public string? Specialty { get; set; }
        public int YearsOfExperience { get; set; }
        public decimal? Rating { get; set; } // 0-5 stars

        // For RBAC: indicates if barber is a manager
        public bool IsManager { get; set; } = false;

        // Availability (JSON format: days and hours)
        public string? Availability { get; set; }

        // Appointments relationship
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}