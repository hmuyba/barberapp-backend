# 💈 BarberApp Backend

API REST para el Sistema de Gestión de Citas para Barberías, desarrollado como proyecto final del Diplomado en Desarrollo Web y Móvil Full Stack.

## 🚀 Tecnologías

- **Framework:** ASP.NET Core 8.0
- **Base de Datos:** PostgreSQL 16
- **ORM:** Entity Framework Core 8.0
- **Autenticación:** JWT Bearer Tokens + 2FA
- **Encriptación:** BCrypt
- **Email:** MailKit (Gmail SMTP)
- **Arquitectura:** Clean Architecture

## 📁 Estructura del Proyecto

```
BarberAppBackend/
├── BarberApp.API/           # Capa de presentación (Controllers, DTOs)
├── BarberApp.Domain/        # Capa de dominio (Entidades)
├── BarberApp.Infrastructure/# Capa de infraestructura (Data, Security)
└── BarberApp.sln           # Solución
```

## ⚙️ Configuración

### 1. Requisitos Previos
- .NET 8.0 SDK
- PostgreSQL 16
- Visual Studio 2022 / VS Code

### 2. Base de Datos
Crear base de datos en PostgreSQL:
```sql
CREATE DATABASE barberapp;
```

### 3. Configurar Connection String
Editar `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=barberapp;Username=postgres;Password=TU_PASSWORD"
  }
}
```

### 4. Aplicar Migraciones
```bash
cd BarberApp.API
dotnet ef database update
```

### 5. Ejecutar
```bash
dotnet run
```

La API estará disponible en: `http://localhost:5199`

## 🔐 Seguridad Implementada

| Característica | Implementación |
|----------------|----------------|
| Autenticación | JWT con expiración de 60 minutos |
| Contraseñas | Hash con BCrypt |
| MFA | 2FA por correo electrónico |
| Autorización | RBAC (Cliente, Barbero, Admin) |
| Headers | X-Frame-Options, X-XSS-Protection, CSP |
| Logging | Auditoría con ILogger |

## 📡 Endpoints Principales

### Autenticación
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/auth/register` | Registro de usuario |
| POST | `/api/auth/login` | Inicio de sesión |
| POST | `/api/auth/verify-2fa` | Verificar código 2FA |

### Citas
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/appointments/my-appointments` | Mis citas (Cliente) |
| GET | `/api/appointments/barber-appointments` | Citas del día (Barbero) |
| POST | `/api/appointments` | Crear cita |
| PUT | `/api/appointments/{id}/status` | Actualizar estado |
| DELETE | `/api/appointments/{id}` | Cancelar cita |

### Barberos
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/barbers` | Listar barberos |
| GET | `/api/barbers/{id}/available-slots` | Horarios disponibles |

### Servicios
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/services` | Listar servicios |

## 👤 Credenciales de Prueba

| Rol | Email | Contraseña |
|-----|-------|------------|
| Cliente | cliente@barberia.com | Cliente123! |
| Barbero | barbero@barberia.com | Barbero123! |
| Admin | admin@barberia.com | Admin123! |

## 🔗 Frontend

Repositorio del frontend: [barberapp-frontend](https://github.com/hmuyba/barberapp-frontend)
