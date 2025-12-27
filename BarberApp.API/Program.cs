using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BarberApp.Infrastructure.Data;
using BarberApp.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ========== DATABASE CONFIGURATION ==========
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========== SECURITY SERVICES ==========
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<TwoFactorService>();
builder.Services.AddScoped<EmailService>();

// ========== JWT AUTHENTICATION ==========
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

builder.Services.AddAuthorization();

// ========== CONTROLLERS ==========
builder.Services.AddControllers();

// ========== SWAGGER/OPENAPI ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========== A05: CORS CONFIGURADO DE FORMA SEGURA ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("SecurePolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")  // Solo orígenes permitidos
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ========== A05: HEADERS DE SEGURIDAD (OWASP) ==========
app.Use(async (context, next) =>
{
    // Prevenir Clickjacking - no permite que la página se cargue en iframes
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Prevenir MIME Type Sniffing - evita que el navegador interprete archivos incorrectamente
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Habilitar protección XSS del navegador
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // Política de Referrer - controla qué información se envía en el header Referer
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy - previene XSS y ataques de inyección
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");

    // Permissions Policy - restringe acceso a APIs del navegador
    context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    await next();
});

// ========== MIDDLEWARE PIPELINE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("SecurePolicy");

app.UseHttpsRedirection();

// IMPORTANT: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();