using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Microsoft.AspNetCore.Http; // Importante per la Sessione

namespace Test_ONUS.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // CORREZIONE: Rinominato da MessaggioErrore a ErrorMsg per coerenza
        public string ErrorMsg { get; set; } = "";

        public void OnGet()
        {
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostAsync(string username, string password)
        {
            // 1. Cerca prima nello Staff (Preparatori)
            var coach = await _context.PreparatoriAtletici
                .Include(p => p.Squadra)
                .FirstOrDefaultAsync(p => p.Nome == username && p.Password == password);

            if (coach != null)
            {
                HttpContext.Session.SetInt32("UserId", coach.Id);
                HttpContext.Session.SetString("Ruolo", "Staff");
                HttpContext.Session.SetString("NomeCompleto", $"{coach.Nome} {coach.Cognome}");

                // Salviamo ID squadra
                HttpContext.Session.SetInt32("SquadraId", coach.SquadraId);
                HttpContext.Session.SetString("NomeSquadra", coach.Squadra?.Nome ?? "Nessuna");

                return RedirectToPage("/Dashboard");
            }

            // 2. Se non è staff, cerca negli Atleti
            var atleta = await _context.Atleti
                .Include(a => a.Squadra)
                .FirstOrDefaultAsync(a => a.Nome == username && a.Password == password);

            if (atleta != null)
            {
                if (!atleta.IsAttivo)
                {
                    ErrorMsg = "Utente disabilitato.";
                    return Page();
                }

                HttpContext.Session.SetInt32("UserId", atleta.Id);
                HttpContext.Session.SetString("Ruolo", "Atleta"); // O "Giocatore"
                HttpContext.Session.SetString("NomeCompleto", $"{atleta.Nome} {atleta.Cognome}");

                if (atleta.SquadraId != null)
                    HttpContext.Session.SetInt32("SquadraId", (int)atleta.SquadraId);

                return RedirectToPage("/Dashboard");
            }

            ErrorMsg = "Nome o Password non validi.";
            return Page();
        }
    }
}