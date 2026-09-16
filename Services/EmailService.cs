using olhuz.API.Services.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Configuration;

namespace olhuz.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string token)
        {
            // ================================================
            // CRIAÇÃO E CONFIGURAÇÃO BÁSICA DO E-MAIL
            // ================================================

            // REMETENTE - DESTINATÁRIO - ASSUNTO

            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(_configuration["EmailSettings:SenderName"], _configuration["EmailSettings:SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Código de Recuperação de Senha";

            // ================================================
            // MONTAGEM DO CORPO DO E-MAIL (HTML)
            // ================================================

            string htmlBody = $@"
            <div style=""font-family: Arial, sans-serif; padding: 20px; color: #333;"">
                <h2>Recuperação de Senha</h2>
                <p>Você solicitou a redefinição da sua senha. Utilize o código de verificação abaixo:</p>
                <div style=""font-size: 28px; font-weight: bold; letter-spacing: 5px; color: #007bff; margin: 20px 0;"">
                    {token}
                </div>
                <p>Este código é válido por <strong>15 minutos</strong>.</p>
                <p>Se você não solicitou esta alteração, desconsidere este e-mail.</p>
            </div>";

            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            // ================================================
            // CONEXÃO, AUTENTICAÇÃO E ENVIO VIA SMTP
            // ================================================

            using var smtp = new SmtpClient();

            // Estabelece conexão com o servidor SMTP usando TLS seguro
            await smtp.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:Port"]!),
                SecureSocketOptions.StartTls
            );

            // Autentica no servidor de e-mail com as credenciais configuradas
            await smtp.AuthenticateAsync(
                _configuration["EmailSettings:SenderEmail"],
                _configuration["EmailSettings:Password"]
            );

            // Dispara o e-mail e encerra a conexão
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
