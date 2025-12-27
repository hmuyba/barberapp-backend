using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberApp.API.DTOs;
using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using System.Security.Claims;

namespace BarberApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarbersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BarbersController> _logger;

        public BarbersController(AppDbContext context, ILogger<BarbersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== GET: Listar todos los barberos activos =====
        [HttpGet]
        public async Task<ActionResult<List<BarberResponse>>> GetAllBarbers()
        {
            var barbers = await _context.Barbers
                .Include(b => b.User)
                .Where(b => b.IsActive && b.User.IsActive)
                .OrderBy(b => b.User.FullName)
                .Select(b => new BarberResponse
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    FullName = b.User.FullName,
                    Email = b.User.Email,
                    Phone = b.User.Phone,
                    Specialty = b.Specialty,
                    YearsOfExperience = b.YearsOfExperience,
                    Rating = b.Rating,
                    IsManager = b.IsManager,
                    Availability = b.Availability,
                    IsActive = b.IsActive
                })
                .ToListAsync();

            return Ok(barbers);
        }

        // ===== GET: Obtener barbero por ID =====
        [HttpGet("{id}")]
        public async Task<ActionResult<BarberResponse>> GetBarber(int id)
        {
            var barber = await _context.Barbers
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (barber == null)
            {
                return NotFound(new { message = "Barber not found" });
            }

            return Ok(new BarberResponse
            {
                Id = barber.Id,
                UserId = barber.UserId,
                FullName = barber.User.FullName,
                Email = barber.User.Email,
                Phone = barber.User.Phone,
                Specialty = barber.Specialty,
                YearsOfExperience = barber.YearsOfExperience,
                Rating = barber.Rating,
                IsManager = barber.IsManager,
                Availability = barber.Availability,
                IsActive = barber.IsActive
            });
        }

        // ===== GET: Obtener horarios disponibles de un barbero =====
        [HttpGet("{id}/available-slots")]
        public async Task<ActionResult<List<AvailableSlotResponse>>> GetAvailableSlots(
            int id,
            [FromQuery] DateTime date,
            [FromQuery] int serviceDuration = 30)
        {
            var barber = await _context.Barbers.FindAsync(id);
            if (barber == null || !barber.IsActive)
            {
                return NotFound(new { message = "Barber not found" });
            }

            // Bolivia es UTC-4
            var boliviaOffset = TimeSpan.FromHours(-4);
            var utcNow = DateTime.UtcNow;
            var localNow = utcNow.Add(boliviaOffset);

            // Fecha local seleccionada
            var localDate = date.Date;

            // Convertir inicio y fin del día a UTC
            var dayStartUtc = DateTime.SpecifyKind(localDate.Add(-boliviaOffset), DateTimeKind.Utc);
            var dayEndUtc = DateTime.SpecifyKind(localDate.AddDays(1).Add(-boliviaOffset), DateTimeKind.Utc);

            // Horario de atención extendido (9am - 10pm)
            var startHour = 9;
            var endHour = 22;  // Cambiado de 19 a 22
            var slotDurationMinutes = 30;

            // Citas existentes
            var existingAppointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.BarberId == id
                            && a.DateTime >= dayStartUtc
                            && a.DateTime < dayEndUtc
                            && a.Status != "Cancelled")
                .ToListAsync();

            var slots = new List<AvailableSlotResponse>();

            // Generar slots en hora local de Bolivia
            for (int hour = startHour; hour < endHour; hour++)
            {
                for (int minute = 0; minute < 60; minute += slotDurationMinutes)
                {
                    var slotLocal = localDate.AddHours(hour).AddMinutes(minute);
                    var slotUtc = DateTime.SpecifyKind(slotLocal.Add(-boliviaOffset), DateTimeKind.Utc);
                    var slotEndUtc = slotUtc.AddMinutes(serviceDuration);

                    // Verificar si el slot ya pasó (para hoy)
                    bool isPast = localDate.Date == localNow.Date && slotLocal <= localNow;

                    // Verificar conflicto con citas existentes
                    bool hasConflict = existingAppointments.Any(a =>
                    {
                        var appointmentEndUtc = a.DateTime.AddMinutes(a.Service?.DurationMinutes ?? 30);
                        return slotUtc < appointmentEndUtc && slotEndUtc > a.DateTime;
                    });

                    // Disponible solo si no pasó y no tiene conflicto
                    bool isAvailable = !isPast && !hasConflict;

                    slots.Add(new AvailableSlotResponse
                    {
                        DateTime = slotLocal,
                        TimeString = slotLocal.ToString("HH:mm"),
                        IsAvailable = isAvailable
                    });
                }
            }

            return Ok(slots);
        }

        // ===== PUT: Actualizar disponibilidad del barbero =====
        [HttpPut("my-availability")]
        [Authorize(Roles = "Barber,Manager")]
        public async Task<ActionResult> UpdateMyAvailability([FromBody] string availability)
        {
            var userId = GetCurrentUserId();
            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);

            if (barber == null)
            {
                return BadRequest(new { message = "User is not a barber" });
            }

            barber.Availability = availability;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Barber {barber.Id} updated availability");

            return Ok(new { message = "Availability updated successfully" });
        }

        // ===== POST: Crear perfil de barbero (Admin) =====
        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<BarberResponse>> CreateBarber([FromBody] CreateBarberRequest request)
        {
            // Verificar que el usuario existe y no es ya un barbero
            var user = await _context.Users
                .Include(u => u.Barber)
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
            {
                return BadRequest(new { message = "User not found" });
            }

            if (user.Barber != null)
            {
                return BadRequest(new { message = "User is already a barber" });
            }

            var barber = new Barber
            {
                UserId = request.UserId,
                Specialty = request.Specialty,
                YearsOfExperience = request.YearsOfExperience,
                IsManager = request.IsManager,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Actualizar rol del usuario a Barber
            user.RoleId = request.IsManager ? 3 : 2; // 3 = Manager, 2 = Barber

            _context.Barbers.Add(barber);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"New barber created: UserId={request.UserId}");

            return CreatedAtAction(nameof(GetBarber), new { id = barber.Id }, new BarberResponse
            {
                Id = barber.Id,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Specialty = barber.Specialty,
                YearsOfExperience = barber.YearsOfExperience,
                Rating = barber.Rating,
                IsManager = barber.IsManager,
                IsActive = barber.IsActive
            });
        }

        // ===== PUT: Actualizar barbero (Admin) =====
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> UpdateBarber(int id, [FromBody] UpdateBarberRequest request)
        {
            var barber = await _context.Barbers.FindAsync(id);
            if (barber == null)
            {
                return NotFound(new { message = "Barber not found" });
            }

            if (request.Specialty != null)
                barber.Specialty = request.Specialty;

            if (request.YearsOfExperience.HasValue)
                barber.YearsOfExperience = request.YearsOfExperience.Value;

            if (request.IsManager.HasValue)
                barber.IsManager = request.IsManager.Value;

            if (request.IsActive.HasValue)
                barber.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Barber updated successfully" });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            return int.Parse(userIdClaim!);
        }
    }

    // DTOs adicionales para este controller
    public class CreateBarberRequest
    {
        public int UserId { get; set; }
        public string? Specialty { get; set; }
        public int YearsOfExperience { get; set; }
        public bool IsManager { get; set; }
    }

    public class UpdateBarberRequest
    {
        public string? Specialty { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? IsManager { get; set; }
        public bool? IsActive { get; set; }
    }
}