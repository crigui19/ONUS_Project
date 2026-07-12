using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
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

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Token))
            {
                Message = "Invalid or missing security token.";
                return Page();
            }

            if (NuovaPassword != ConfermaPassword)
            {
                Message = "Passwords do not match.";
                return Page();
            }

            // Cerca il preparatore che ha questo token ESATTO e verifica che non sia scaduto
            var coach = await _context.PreparatoriAtletici.FirstOrDefaultAsync(p =>
                p.ResetToken == Token &&
                p.ResetTokenScadenza > DateTime.UtcNow);

            if (coach == null)
            {
                Message = "This reset link is invalid or has expired. Please request a new one.";
                return Page();
            }

            // Aggiorna la password con BCrypt
            coach.Password = BCrypt.Net.BCrypt.HashPassword(NuovaPassword);

            // Invalida il token per evitare che venga riutilizzato
            coach.ResetToken = null;
            coach.ResetTokenScadenza = null;

            await _context.SaveChangesAsync();

            // Reindirizza al Login con magari un messaggio di successo salvato in TempData (opzionale)
            return RedirectToPage("/Login");
        }
    }
}