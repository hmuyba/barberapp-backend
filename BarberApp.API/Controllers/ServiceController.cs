using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BarberApp.API.DTOs;
using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;

namespace BarberApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(AppDbContext context, ILogger<ServicesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===== GET: Listar todos los servicios activos =====
        [HttpGet]
        public async Task<ActionResult<List<ServiceResponse>>> GetAllServices()
        {
            var services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new ServiceResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Price = s.Price,
                    DurationMinutes = s.DurationMinutes,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(services);
        }

        // ===== GET: Obtener servicio por ID =====
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceResponse>> GetService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound(new { message = "Service not found" });
            }

            return Ok(new ServiceResponse
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                IsActive = service.IsActive
            });
        }

        // ===== POST: Crear servicio (Admin) =====
        [HttpPost]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult<ServiceResponse>> CreateService([FromBody] CreateServiceRequest request)
        {
            // Validar que no exista un servicio con el mismo nombre
            if (await _context.Services.AnyAsync(s => s.Name.ToLower() == request.Name.ToLower()))
            {
                return BadRequest(new { message = "A service with this name already exists" });
            }

            var service = new Service
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                DurationMinutes = request.DurationMinutes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"New service created: {service.Name}");

            return CreatedAtAction(nameof(GetService), new { id = service.Id }, new ServiceResponse
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                Price = service.Price,
                DurationMinutes = service.DurationMinutes,
                IsActive = service.IsActive
            });
        }

        // ===== PUT: Actualizar servicio (Admin) =====
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound(new { message = "Service not found" });
            }

            if (!string.IsNullOrEmpty(request.Name))
                service.Name = request.Name;

            if (request.Description != null)
                service.Description = request.Description;

            if (request.Price.HasValue)
                service.Price = request.Price.Value;

            if (request.DurationMinutes.HasValue)
                service.DurationMinutes = request.DurationMinutes.Value;

            if (request.IsActive.HasValue)
                service.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Service updated: {service.Name}");

            return Ok(new { message = "Service updated successfully" });
        }

        // ===== DELETE: Desactivar servicio (Admin) =====
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator,Manager")]
        public async Task<ActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound(new { message = "Service not found" });
            }

            // Soft delete - solo desactivar
            service.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Service deactivated: {service.Name}");

            return Ok(new { message = "Service deactivated successfully" });
        }
    }

    // DTOs para este controller
    public class CreateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class UpdateServiceRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? DurationMinutes { get; set; }
        public bool? IsActive { get; set; }
    }
}