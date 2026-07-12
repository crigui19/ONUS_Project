using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Test_ONUS.Data;

namespace Test_ONUS.Pages
{
    public class RecuperoPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RecuperoPasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = false;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email))
            {
                Message = "Please enter an email address.";
                return Page();
            }

            // Cerca il coach nel database tramite la mail
            var coach = await _context.PreparatoriAtletici.FirstOrDefaultAsync(p => p.Email.ToLower() == Email.ToLower());

            if (coach != null)
            {
                // Genera un token sicuro e imposta la scadenza a 1 ora
                coach.ResetToken = Guid.NewGuid().ToString();
                coach.ResetTokenScadenza = DateTime.UtcNow.AddHours(1);
                await _context.SaveChangesAsync();

                // Genera il link assoluto da inviare via mail
                var resetLink = Url.Page("/ResetPassword", null, new { token = coach.ResetToken }, Request.Scheme);

                try
                {
                    /* CONFIGURAZIONE GMAIL SMTP */
                    using (var client = new SmtpClient("smtp.gmail.com", 587))
                    {
                        client.EnableSsl = true;

                        // INSERISCI QUI LA TUA MAIL E LA TUA PASSWORD PER LE APP DA 16 CARATTERI (Senza spazi)
                        client.Credentials = new NetworkCredential("criguidolin@gmail.com", "nwxc nraf kkbi mzjb");

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("criguidolin@gmail.com", "ONUS Support"),
                            Subject = "ONUS - Password Reset",
                            Body = $"<div style='font-family: Arial, sans-serif; padding: 20px; background-color: #1a1a1a; color: white; text-align: center;'>" +
                                   $"<h2 style='color: #00a0ff;'>Password Reset Request</h2>" +
                                   $"<p>Click the button below to choose a new password. This link is valid for 1 hour.</p>" +
                                   $"<a href='{resetLink}' style='background-color: #00a0ff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 50px; display: inline-block; margin-top: 15px; font-weight: bold;'>RESET PASSWORD</a>" +
                                   $"</div>",
                            IsBodyHtml = true
                        };

                        mailMessage.To.Add(coach.Email);
                        await client.SendMailAsync(mailMessage);
                    }
                }
                catch (Exception ex)
                {
                    // Se l'SMTP non è configurato, per ora logghiamo l'errore o lo mostriamo (in produzione rimuovere ex.Message)
                    Message = "Error sending email. Check SMTP settings. System error: " + ex.Message;
                    return Page();
                }
            }

            // Mostriamo sempre lo stesso messaggio per motivi di sicurezza (per non rivelare quali email sono registrate)
            IsSuccess = true;
            Message = "If the email is registered, you will receive a reset link shortly.";

            return Page();
        }
    }
}