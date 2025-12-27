using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BarberApp.API.DTOs;
using BarberApp.Domain.Entities;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Security;
using BCrypt.Net;

namespace BarberApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly TwoFactorService _twoFactorService;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context,
            JwtService jwtService,
            TwoFactorService twoFactorService,
            EmailService emailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _twoFactorService = twoFactorService;
            _emailService = emailService;
            _logger = logger;
        }

        // ===== REGISTER ENDPOINT =====
        // INTEGRITY (CIA): Logs all user registrations with timestamp
        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate email is unique
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                // Validate role exists
                var role = await _context.Roles.FindAsync(request.RoleId);
                if (role == null)
                {
                    return BadRequest(new { message = "Invalid role" });
                }

                // CONFIDENTIALITY (CIA): Hash password using BCrypt
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create user
                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    PasswordHash = passwordHash,
                    RoleId = request.RoleId,
                    TwoFactorEnabled = request.EnableTwoFactor,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // If 2FA enabled, generate secret
                if (request.EnableTwoFactor)
                {
                    user.TwoFactorSecret = _twoFactorService.GenerateSecret();
                }
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // INTEGRITY (CIA): Log registration
                _logger.LogInformation($"New user registered: {user.Email} at {DateTime.UtcNow}");

                return Ok(new RegisterResponse
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = role.Name,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    Message = "User registered successfully"
                });
            }
            catch (Exception ex)
            {
                // AVAILABILITY (CIA): Proper error handling
                _logger.LogError($"Registration error: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error during registration" });
            }
        }

        // ===== LOGIN ENDPOINT =====
        // CONFIDENTIALITY (CIA): Returns JWT token only after successful authentication
        // MULTI-FACTOR: Sends 2FA code if enabled
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Barber)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    // SECURITY: Don't reveal if user exists or not
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // CONFIDENTIALITY (CIA): Verify password hash
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    // INTEGRITY: Log failed login attempt
                    _logger.LogWarning($"Failed login attempt for: {request.Email} at {DateTime.UtcNow}");
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return Unauthorized(new { message = "Account is inactive" });
                }

                // MULTI-FACTOR AUTHENTICATION
                if (user.TwoFactorEnabled)
                {
                    // Generate 2FA code
                    var (code, expiry) = _twoFactorService.GenerateCodeWithExpiry();

                    user.TwoFactorCode = code;
                    user.TwoFactorExpiry = expiry;
                    await _context.SaveChangesAsync();

                    // Send code via email
                    await _emailService.SendTwoFactorCodeAsync(user.Email, user.FullName, code);

                    // INTEGRITY: Log 2FA code generation
                    _logger.LogInformation($"2FA code sent to: {user.Email} at {DateTime.UtcNow}");

                    return Ok(new LoginResponse
                    {
                        RequiresTwoFactor = true,
                        Message = "2FA code sent to your email"
                    });
                }

                // If 2FA not enabled, generate token directly
                var token = _jwtService.GenerateToken(user);

                // Update last login (INTEGRITY - audit trail)
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // INTEGRITY: Log successful login
                _logger.LogInformation($"User logged in: {user.Email} at {DateTime.UtcNow}");

                return Ok(new LoginResponse
                {
                    RequiresTwoFactor = false,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.Role.Name,
                        RoleId = user.RoleId,
                        IsVIP = user.IsVIP,
                        TwoFactorEnabled = user.TwoFactorEnabled,
                        BarberId = user.Barber?.Id,
                        IsManager = user.Barber?.IsManager ?? false
                    },
                    Message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                // AVAILABILITY (CIA): Proper error handling
                _logger.LogError($"Login error: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error during login" });
            }
        }

        // ===== VERIFY 2FA CODE ENDPOINT =====
        // MULTI-FACTOR AUTHENTICATION: Validates 2FA code
        [HttpPost("verify-2fa")]
        public async Task<ActionResult<VerifyTwoFactorResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .Include(u => u.Barber)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid request" });
                }

                // Validate 2FA code
                if (!_twoFactorService.ValidateCode(user, request.Code))
                {
                    // INTEGRITY: Log failed 2FA attempt
                    _logger.LogWarning($"Failed 2FA attempt for: {user.Email} at {DateTime.UtcNow}");
                    return Unauthorized(new { message = "Invalid or expired code" });
                }

                // Clear 2FA code after successful validation
                user.TwoFactorCode = null;
                user.TwoFactorExpiry = null;
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                // INTEGRITY: Log successful 2FA verification
                _logger.LogInformation($"2FA verified for: {user.Email} at {DateTime.UtcNow}");

                return Ok(new VerifyTwoFactorResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Email = user.Email,
                        Phone = user.Phone,
                        Role = user.Role.Name,
                        RoleId = user.RoleId,
                        IsVIP = user.IsVIP,
                        TwoFactorEnabled = user.TwoFactorEnabled,
                        BarberId = user.Barber?.Id,
                        IsManager = user.Barber?.IsManager ?? false
                    },
                    Message = "2FA verification successful"
                });
            }
            catch (Exception ex)
            {
                // AVAILABILITY (CIA): Proper error handling
                _logger.LogError($"2FA verification error: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error during 2FA verification" });
            }
        }

        // ===== RESEND 2FA CODE ENDPOINT =====
        [HttpPost("resend-2fa")]
        public async Task<ActionResult> ResendTwoFactorCode([FromBody] string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null || !user.TwoFactorEnabled)
                {
                    return BadRequest(new { message = "Invalid request" });
                }

                // Generate new code
                var (code, expiry) = _twoFactorService.GenerateCodeWithExpiry();

                user.TwoFactorCode = code;
                user.TwoFactorExpiry = expiry;
                await _context.SaveChangesAsync();

                // Send code
                await _emailService.SendTwoFactorCodeAsync(user.Email, user.FullName, code);

                return Ok(new { message = "New code sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Resend 2FA error: {ex.Message}");
                return StatusCode(500, new { message = "Error resending code" });
            }
        }

        // ===== VALIDATE TOKEN ENDPOINT =====
        // Used to check if token is still valid
        [HttpPost("validate-token")]
        public ActionResult ValidateToken([FromBody] string token)
        {
            try
            {
                var principal = _jwtService.ValidateToken(token);

                if (principal == null)
                {
                    return Unauthorized(new { message = "Invalid or expired token" });
                }

                return Ok(new { message = "Token is valid", claims = principal.Claims });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Token validation error: {ex.Message}");
                return StatusCode(500, new { message = "Error validating token" });
            }
        }
    }
}