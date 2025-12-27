namespace BarberApp.API.DTOs
{
    // ===== REQUEST DTOs =====
    public class CreateAppointmentRequest
    {
        public int BarberId { get; set; }
        public int ServiceId { get; set; }
        public DateTime DateTime { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateAppointmentRequest
    {
        public DateTime? DateTime { get; set; }
        public int? ServiceId { get; set; }
        public string? Notes { get; set; }
    }

    // ===== RESPONSE DTOs =====
    public class AppointmentResponse
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // Client info
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        // Barber info
        public int BarberId { get; set; }
        public string BarberName { get; set; } = string.Empty;

        // Service info
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal ServicePrice { get; set; }
        public int ServiceDuration { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class BarberResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Specialty { get; set; }
        public int YearsOfExperience { get; set; }
        public decimal? Rating { get; set; }
        public bool IsManager { get; set; }
        public string? Availability { get; set; }
        public bool IsActive { get; set; }
    }

    public class ServiceResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    public class AvailableSlotResponse
    {
        public DateTime DateTime { get; set; }
        public string TimeString { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }

    public class DashboardStatsResponse
    {
        public int TotalAppointmentsToday { get; set; }
        public int CompletedToday { get; set; }
        public int PendingToday { get; set; }
        public decimal IncomeToday { get; set; }
        public int TotalClients { get; set; }
        public int TotalBarbers { get; set; }
    }
}