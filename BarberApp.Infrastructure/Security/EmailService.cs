using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace BarberApp.Infrastructure.Security
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ===== 2FA CODE EMAIL =====
        public async Task SendTwoFactorCodeAsync(string toEmail, string toName, string code)
        {
            var subject = "Tu código de verificación - BarberApp";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f5f5f5; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px;'>
                        <h2 style='color: #667eea;'>🔐 Código de Verificación</h2>
                        <p>Hola <strong>{toName}</strong>,</p>
                        <p>Tu código de verificación es:</p>
                        <div style='background-color: #667eea; color: white; font-size: 32px; letter-spacing: 8px; text-align: center; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            {code}
                        </div>
                        <p style='color: #666;'>Este código expirará en <strong>5 minutos</strong>.</p>
                        <p style='color: #999; font-size: 12px;'>Si no solicitaste este código, ignora este mensaje.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='color: #667eea;'>✂️ BarberApp - Sistema de Gestión de Citas</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, toName, subject, body);
        }

        // ===== APPOINTMENT CONFIRMATION EMAIL (OE4) =====
        public async Task SendAppointmentConfirmationAsync(
            string toEmail,
            string clientName,
            string barberName,
            string serviceName,
            DateTime dateTime,
            decimal price)
        {
            var formattedDate = dateTime.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
            var formattedTime = dateTime.ToString("HH:mm");

            var subject = "✅ Cita Confirmada - BarberApp";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f5f5f5; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px;'>
                        <h2 style='color: #28a745;'>✅ ¡Cita Confirmada!</h2>
                        <p>Hola <strong>{clientName}</strong>,</p>
                        <p>Tu cita ha sido agendada exitosamente. Aquí están los detalles:</p>
                        
                        <div style='background-color: #f8f9fa; border-left: 4px solid #667eea; padding: 20px; margin: 20px 0; border-radius: 4px;'>
                            <p style='margin: 5px 0;'>📅 <strong>Fecha:</strong> {formattedDate}</p>
                            <p style='margin: 5px 0;'>🕐 <strong>Hora:</strong> {formattedTime} hrs</p>
                            <p style='margin: 5px 0;'>💇 <strong>Barbero:</strong> {barberName}</p>
                            <p style='margin: 5px 0;'>✂️ <strong>Servicio:</strong> {serviceName}</p>
                            <p style='margin: 5px 0;'>💰 <strong>Precio:</strong> Bs. {price:F2}</p>
                        </div>

                        <div style='background-color: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <p style='margin: 0; color: #856404;'>
                                <strong>📌 Recuerda:</strong> Te enviaremos un recordatorio 24 horas antes de tu cita.
                            </p>
                        </div>

                        <p>Si necesitas cancelar o reprogramar tu cita, puedes hacerlo desde la aplicación.</p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='color: #667eea;'>✂️ BarberApp - Sistema de Gestión de Citas</p>
                        <p style='color: #999; font-size: 12px;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, clientName, subject, body);
        }

        // ===== APPOINTMENT CANCELLATION EMAIL (OE4) =====
        public async Task SendAppointmentCancellationAsync(
            string toEmail,
            string clientName,
            DateTime dateTime)
        {
            var formattedDate = dateTime.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
            var formattedTime = dateTime.ToString("HH:mm");

            var subject = "❌ Cita Cancelada - BarberApp";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f5f5f5; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px;'>
                        <h2 style='color: #dc3545;'>❌ Cita Cancelada</h2>
                        <p>Hola <strong>{clientName}</strong>,</p>
                        <p>Tu cita programada para el siguiente horario ha sido cancelada:</p>
                        
                        <div style='background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 20px 0; border-radius: 4px;'>
                            <p style='margin: 5px 0;'>📅 <strong>Fecha:</strong> {formattedDate}</p>
                            <p style='margin: 5px 0;'>🕐 <strong>Hora:</strong> {formattedTime} hrs</p>
                        </div>

                        <p>Si deseas agendar una nueva cita, puedes hacerlo desde la aplicación.</p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='color: #667eea;'>✂️ BarberApp - Sistema de Gestión de Citas</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, clientName, subject, body);
        }

        // ===== APPOINTMENT REMINDER EMAIL (OE4) =====
        public async Task SendAppointmentReminderAsync(
            string toEmail,
            string clientName,
            string barberName,
            string serviceName,
            DateTime dateTime)
        {
            var formattedDate = dateTime.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
            var formattedTime = dateTime.ToString("HH:mm");

            var subject = "⏰ Recordatorio de Cita - BarberApp";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; background-color: #f5f5f5; padding: 20px;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px;'>
                        <h2 style='color: #667eea;'>⏰ Recordatorio de Cita</h2>
                        <p>Hola <strong>{clientName}</strong>,</p>
                        <p>Te recordamos que tienes una cita programada para <strong>mañana</strong>:</p>
                        
                        <div style='background-color: #e7f3ff; border-left: 4px solid #667eea; padding: 20px; margin: 20px 0; border-radius: 4px;'>
                            <p style='margin: 5px 0;'>📅 <strong>Fecha:</strong> {formattedDate}</p>
                            <p style='margin: 5px 0;'>🕐 <strong>Hora:</strong> {formattedTime} hrs</p>
                            <p style='margin: 5px 0;'>💇 <strong>Barbero:</strong> {barberName}</p>
                            <p style='margin: 5px 0;'>✂️ <strong>Servicio:</strong> {serviceName}</p>
                        </div>

                        <p>¡Te esperamos!</p>
                        
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='color: #667eea;'>✂️ BarberApp - Sistema de Gestión de Citas</p>
                    </div>
                </body>
                </html>";

            await SendEmailAsync(toEmail, clientName, subject, body);
        }

        // ===== MÉTODO PRIVADO PARA ENVIAR EMAILS =====
        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                emailSettings["SenderName"] ?? "BarberApp",
                emailSettings["SenderEmail"] ?? "noreply@barberapp.com"
            ));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            // Para desarrollo: mostrar en consola
            Console.WriteLine($"\n{'=',-50}");
            Console.WriteLine($"📧 EMAIL NOTIFICATION");
            Console.WriteLine($"{'=',-50}");
            Console.WriteLine($"To: {toName} <{toEmail}>");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"{'=',-50}\n");

            // Descomentar para producción con credenciales reales
            /*
            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(
                    emailSettings["SmtpServer"], 
                    int.Parse(emailSettings["SmtpPort"] ?? "587"), 
                    SecureSocketOptions.StartTls
                );
                
                await client.AuthenticateAsync(
                    emailSettings["Username"], 
                    emailSettings["Password"]
                );
                
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
            */

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"] ?? "587"),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    emailSettings["Username"],
                    emailSettings["Password"]
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine("✅ Email enviado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending email: {ex.Message}");
            }
        }
    }
}