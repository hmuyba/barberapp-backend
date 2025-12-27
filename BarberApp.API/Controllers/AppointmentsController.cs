using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberApp.API.DTOs;
using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Security;
using System.Security.Claims;

namespace BarberApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            AppDbContext context,
            EmailService emailService,
            ILogger<AppointmentsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // ===== GET: Obtener citas del cliente autenticado =====
        [HttpGet("my-appointments")]
        public async Task<ActionResult<List<AppointmentResponse>>> GetMyAppointments()
        {
            var userId = GetCurrentUserId();

            var appointments = await _context.Appointments
                .Include(a => a.Barber).ThenInclude(b => b.User)
                .Include(a => a.Service)
                .Include(a => a.Client)
                .Where(a => a.ClientId == userId)
                .OrderByDescending(a => a.DateTime)
                .Select(a => MapToResponse(a))
                .ToListAsync();

            return Ok(appointments);
        }

        // ===== GET: Obtener citas del barbero autenticado =====
        [HttpGet("barber-appointments")]
        public async Task<ActionResult<List<AppointmentResponse>>> GetBarberAppointments([FromQuery] DateTime? date)
        {
            var userId = GetCurrentUserId();

            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
            if (barber == null)
            {
                return BadRequest(new { message = "User is not a barber" });
            }

            var query = _context.Appointments
                .Include(a => a.Barber).ThenInclude(b => b.User)
                .Include(a => a.Service)
                .Include(a => a.Client)
                .Where(a => a.BarberId == barber.Id);

            if (date.HasValue)
            {
                // Convertir fecha Bolivia a rango UTC
                var boliviaOffset = TimeSpan.FromHours(-4);
                var localDate = date.Value.Date;
                var startUtc = DateTime.SpecifyKind(localDate.Add(-boliviaOffset), DateTimeKind.Utc);
                var endUtc = startUtc.AddDays(1);

                query = query.Where(a => a.DateTime >= startUtc && a.DateTime < endUtc);
            }

            var appointments = await query
                .OrderBy(a => a.DateTime)
                .Select(a => MapToResponse(a))
                .ToListAsync();

            return Ok(appointments);
        }

        // ===== GET: Obtener todas las citas (Admin) =====
        [HttpGet("all")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<List<AppointmentResponse>>> GetAllAppointments(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Appointments
                .Include(a => a.Barber).ThenInclude(b => b.User)
                .Include(a => a.Service)
                .Include(a => a.Client)
                .AsQueryable();

            // Bolivia es UTC-4
            var boliviaOffset = TimeSpan.FromHours(-4);

            if (startDate.HasValue)
            {
                // Convertir fecha local Bolivia a UTC
                var startUtc = DateTime.SpecifyKind(startDate.Value.Date.Add(-boliviaOffset), DateTimeKind.Utc);
                query = query.Where(a => a.DateTime >= startUtc);
            }

            if (endDate.HasValue)
            {
                // Convertir fecha local Bolivia a UTC
                var endUtc = DateTime.SpecifyKind(endDate.Value.Date.Add(-boliviaOffset), DateTimeKind.Utc);
                query = query.Where(a => a.DateTime < endUtc);
            }

            var appointments = await query
                .OrderByDescending(a => a.DateTime)
                .ToListAsync();

            // Convertir a response con hora Bolivia
            var response = appointments.Select(a => MapToResponse(a)).ToList();

            return Ok(response);
        }

        // ===== POST: Crear nueva cita =====
        [HttpPost]
        public async Task<ActionResult<AppointmentResponse>> CreateAppointment([FromBody] CreateAppointmentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Validar que el barbero existe
                var barber = await _context.Barbers
                    .Include(b => b.User)
                    .FirstOrDefaultAsync(b => b.Id == request.BarberId);

                if (barber == null || !barber.IsActive)
                {
                    return BadRequest(new { message = "Barber not found or inactive" });
                }

                // Validar que el servicio existe
                var service = await _context.Services.FindAsync(request.ServiceId);
                if (service == null || !service.IsActive)
                {
                    return BadRequest(new { message = "Service not found or inactive" });
                }

                // IMPORTANTE: Convertir hora Bolivia a UTC para guardar
                // La fecha viene como hora local de Bolivia (UTC-4)
                var boliviaOffset = TimeSpan.FromHours(-4);
                var dateTimeUtc = DateTime.SpecifyKind(request.DateTime, DateTimeKind.Unspecified)
                    .Add(-boliviaOffset); // Convertir Bolivia -> UTC
                dateTimeUtc = DateTime.SpecifyKind(dateTimeUtc, DateTimeKind.Utc);

                // Validar que el horario está disponible
                var endTimeUtc = dateTimeUtc.AddMinutes(service.DurationMinutes);
                var conflictingAppointment = await _context.Appointments
                    .AnyAsync(a => a.BarberId == request.BarberId
                        && a.Status != "Cancelled"
                        && a.DateTime < endTimeUtc
                        && a.DateTime.AddMinutes(a.Service.DurationMinutes) > dateTimeUtc);

                if (conflictingAppointment)
                {
                    return BadRequest(new { message = "Time slot is not available" });
                }

                // Obtener datos del cliente
                var client = await _context.Users.FindAsync(userId);

                // Crear la cita con fecha en UTC
                var appointment = new Appointment
                {
                    ClientId = userId,
                    BarberId = request.BarberId,
                    ServiceId = request.ServiceId,
                    DateTime = dateTimeUtc,  // Guardar en UTC
                    Status = "Confirmed",
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // ===== OE4: ENVIAR NOTIFICACIÓN POR EMAIL =====
                // Usar hora Bolivia para el email (más legible para el usuario)
                var dateTimeBolivia = dateTimeUtc.Add(boliviaOffset);
                try
                {
                    await _emailService.SendAppointmentConfirmationAsync(
                        client!.Email,
                        client.FullName,
                        barber.User.FullName,
                        service.Name,
                        dateTimeBolivia,  // Enviar hora Bolivia en el email
                        service.Price
                    );
                    _logger.LogInformation($"Confirmation email sent to {client.Email}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning($"Failed to send confirmation email: {emailEx.Message}");
                }

                _logger.LogInformation($"Appointment created: ID={appointment.Id}, Client={userId}, Barber={request.BarberId}, DateTime(UTC)={dateTimeUtc}");

                // Recargar con includes para la respuesta
                var createdAppointment = await _context.Appointments
                    .Include(a => a.Barber).ThenInclude(b => b.User)
                    .Include(a => a.Service)
                    .Include(a => a.Client)
                    .FirstAsync(a => a.Id == appointment.Id);

                return CreatedAtAction(nameof(GetMyAppointments), MapToResponse(createdAppointment));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating appointment: {ex.Message}");
                return StatusCode(500, new { message = "Error creating appointment" });
            }
        }

        // ===== PUT: Actualizar estado de cita =====
        [HttpPut("{id}/status")]
        public async Task<ActionResult> UpdateAppointmentStatus(int id, [FromBody] string status)
        {
            var userId = GetCurrentUserId();
            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found" });
            }

            // Validar que el usuario puede modificar esta cita
            var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);
            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);

            bool canModify = appointment.ClientId == userId
                || (barber != null && appointment.BarberId == barber.Id)
                || user.Role.Name == "Administrator"
                || user.Role.Name == "Manager";

            if (!canModify)
            {
                return Forbid();
            }

            var validStatuses = new[] { "Pending", "Confirmed", "Completed", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest(new { message = "Invalid status" });
            }

            appointment.Status = status;
            appointment.ModifiedAt = DateTime.UtcNow;
            appointment.ModifiedBy = userId;
            await _context.SaveChangesAsync();

            // Enviar notificación si se cancela
            if (status == "Cancelled")
            {
                try
                {
                    await _emailService.SendAppointmentCancellationAsync(
                        appointment.Client.Email,
                        appointment.Client.FullName,
                        appointment.DateTime
                    );
                }
                catch { /* Log but don't fail */ }
            }

            return Ok(new { message = $"Appointment status updated to {status}" });
        }

        // ===== DELETE: Cancelar cita =====
        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            return await UpdateAppointmentStatus(id, "Cancelled");
        }

        // ===== GET: Estadísticas del dashboard =====
        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetStats()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId);

            // Calcular inicio y fin del día en Bolivia, convertido a UTC
            var boliviaOffset = TimeSpan.FromHours(-4);
            var nowBolivia = DateTime.UtcNow.Add(boliviaOffset);
            var todayBolivia = nowBolivia.Date;

            var todayStartUtc = DateTime.SpecifyKind(todayBolivia.Add(-boliviaOffset), DateTimeKind.Utc);
            var todayEndUtc = todayStartUtc.AddDays(1);

            IQueryable<Appointment> query = _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.DateTime >= todayStartUtc && a.DateTime < todayEndUtc);

            // Si es barbero, filtrar solo sus citas
            var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
            if (barber != null && user.Role.Name != "Administrator")
            {
                query = query.Where(a => a.BarberId == barber.Id);
            }

            var appointments = await query.ToListAsync();

            var stats = new DashboardStatsResponse
            {
                TotalAppointmentsToday = appointments.Count,
                CompletedToday = appointments.Count(a => a.Status == "Completed"),
                PendingToday = appointments.Count(a => a.Status == "Confirmed" || a.Status == "Pending"),
                IncomeToday = appointments.Where(a => a.Status == "Completed").Sum(a => a.Service.Price),
                TotalClients = await _context.Users.CountAsync(u => u.RoleId == 1),
                TotalBarbers = await _context.Barbers.CountAsync(b => b.IsActive)
            };

            return Ok(stats);
        }

        // ===== Helpers =====
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            return int.Parse(userIdClaim!);
        }

        private static AppointmentResponse MapToResponse(Appointment a)
        {
            // Convertir UTC a hora Bolivia para mostrar
            var boliviaOffset = TimeSpan.FromHours(-4);
            var dateTimeBolivia = a.DateTime.Add(boliviaOffset);
            var createdAtBolivia = a.CreatedAt.Add(boliviaOffset);

            return new AppointmentResponse
            {
                Id = a.Id,
                DateTime = dateTimeBolivia,  // Devolver hora Bolivia
                Status = a.Status,
                Notes = a.Notes,
                ClientId = a.ClientId,
                ClientName = a.Client.FullName,
                ClientPhone = a.Client.Phone,
                BarberId = a.BarberId,
                BarberName = a.Barber.User.FullName,
                ServiceId = a.ServiceId,
                ServiceName = a.Service.Name,
                ServicePrice = a.Service.Price,
                ServiceDuration = a.Service.DurationMinutes,
                CreatedAt = createdAtBolivia
            };
        }
    }
}