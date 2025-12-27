namespace BarberApp.Domain.Entities
{
    public class Appointment
    {
        public int Id { get; set; }

        // Client (User) relationship
        public int ClientId { get; set; }
        public User Client { get; set; } = null!;

        // Barber relationship
        public int BarberId { get; set; }
        public Barber Barber { get; set; } = null!;

        // Service relationship
        public int ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        // Appointment data
        public DateTime DateTime { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled
        public string? Notes { get; set; }

        // Audit (Integrity - CIA)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public int CreatedBy { get; set; } // User ID who created
        public int? ModifiedBy { get; set; } // User ID who modified
    }
}