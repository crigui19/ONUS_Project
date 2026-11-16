using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;

namespace Test_ONUS.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResetPasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } = string.Empty;

        [BindProperty]
        public string NuovaPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfermaPassword { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                Message = "Token di sicurezza mancante o non valido.";
                return Page();
            }

            var tokenHash = CalcolaHashToken(Token);
            var tokenValido = await _context.PreparatoriAtletici
                .AnyAsync(p => p.ResetToken == tokenHash && p.ResetTokenScadenza > DateTime.UtcNow);

            if (!tokenValido)
            {
                Message = "Questo link non è valido o è scaduto. Richiedine uno nuovo.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                Message = "Token di sicurezza mancante o non valido.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(NuovaPassword) || NuovaPassword.Length < 8)
            {
                Message = "La nuova password deve contenere almeno 8 caratteri.";
                return Page();
            }

            if (NuovaPassword != ConfermaPassword)
            {
                Message = "Le password non coincidono.";
                return Page();
            }

            var tokenHash = CalcolaHashToken(Token);

            var coach = await _context.PreparatoriAtletici.FirstOrDefaultAsync(p =>
                p.ResetToken == tokenHash &&
                p.ResetTokenScadenza > DateTime.UtcNow);

            if (coach == null)
            {
                Message = "Questo link non è valido o è scaduto. Richiedine uno nuovo.";
                return Page();
            }

            coach.Password = BCrypt.Net.BCrypt.HashPassword(NuovaPassword);
            coach.ResetToken = null;
            coach.ResetTokenScadenza = null;

            await _context.SaveChangesAsync();

            TempData["MessaggioSuccesso"] = "Password aggiornata con successo. Ora puoi accedere.";
            return RedirectToPage("/Login");
        }

        private static string CalcolaHashToken(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken.Trim());
            return Convert.ToBase64String(SHA256.HashData(bytes));
        }
    }
}