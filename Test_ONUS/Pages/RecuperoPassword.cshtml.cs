using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;

namespace Test_ONUS.Pages
{
    public class RecuperoPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecuperoPasswordModel> _logger;

        public RecuperoPasswordModel(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<RecuperoPasswordModel> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = false;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Message = "Inserisci un indirizzo email valido.";
                return Page();
            }

            var emailNormalizzata = Email.Trim().ToLower();
            var coach = await _context.PreparatoriAtletici
                .FirstOrDefaultAsync(p => p.Email.ToLower() == emailNormalizzata);

            if (coach != null)
            {
                // Genera un token crittograficamente sicuro (256 bit)
                var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
                var rawToken = WebEncoders.Base64UrlEncode(rawTokenBytes);

                // Salva nel DB solo l'hash SHA-256 del token
                var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

                coach.ResetToken = tokenHash;
                coach.ResetTokenScadenza = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                var resetLink = Url.Page("/ResetPassword", null, new { token = rawToken }, Request.Scheme);

                try
                {
                    var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                    var smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
                    var senderEmail = _configuration["EmailSettings:SenderEmail"];
                    var senderName = _configuration["EmailSettings:SenderName"] ?? "ONUS Support";
                    var smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                    if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(smtpPassword))
                    {
                        _logger.LogError("Configurazione SMTP mancante in EmailSettings.");
                        Message = "Servizio email temporaneamente non disponibile. Riprova più tardi.";
                        return Page();
                    }

                    using var client = new SmtpClient(smtpHost, smtpPort)
                    {
                        EnableSsl = true,
                        Credentials = new NetworkCredential(senderEmail, smtpPassword)
                    };

                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = "ONUS - Reimpostazione Password",
                        Body = $"<div style='font-family: Arial, sans-serif; padding: 20px; background-color: #1a1a1a; color: white; text-align: center;'>" +
                               $"<h2 style='color: #00a0ff;'>Richiesta Reset Password</h2>" +
                               $"<p>Clicca sul pulsante qui sotto per scegliere una nuova password. Il link è valido per 1 ora.</p>" +
                               $"<a href='{resetLink}' style='background-color: #00a0ff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 50px; display: inline-block; margin-top: 15px; font-weight: bold;'>REIMPOSTA PASSWORD</a>" +
                               $"</div>",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(coach.Email);
                    await client.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante l'invio dell'email di reset password a {Email}", coach.Email);
                    Message = "Si è verificato un errore durante l'invio dell'email. Riprova più tardi.";
                    return Page();
                }
            }

            IsSuccess = true;
            Message = "Se l'indirizzo email è registrato, riceverai a breve un link per reimpostare la password.";
            return Page();
        }
    }
}