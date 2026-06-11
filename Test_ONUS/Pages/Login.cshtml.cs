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

        public string ErrorMsg { get; set; } = "";

        public void OnGet()
        {
            HttpContext.Session.Clear();
        }

        public async Task<IActionResult> OnPostAsync(string username, string password)
        {
            // ==========================================
            // 1. Cerca prima nello Staff (Preparatori)
            // ==========================================
            // Estrai l'utente DAL SOLO NOME
            var coach = await _context.PreparatoriAtletici
                .Include(p => p.Squadra)
                .FirstOrDefaultAsync(p => p.Nome == username);

            // Se l'utente esiste, verifica se la password digitata coincide con l'hash
            if (coach != null && BCrypt.Net.BCrypt.Verify(password, coach.Password))
            {
                HttpContext.Session.SetInt32("UserId", coach.Id);
                HttpContext.Session.SetString("Ruolo", "Staff");
                HttpContext.Session.SetString("NomeCompleto", $"{coach.Nome} {coach.Cognome}");

                // Salviamo ID squadra
                HttpContext.Session.SetInt32("SquadraId", coach.SquadraId);
                HttpContext.Session.SetString("NomeSquadra", coach.Squadra?.Nome ?? "Nessuna");

                return RedirectToPage("/Dashboard");
            }

            // ==========================================
            // 2. Se non è staff, cerca negli Atleti
            // ==========================================
            // Estrai l'atleta DAL SOLO NOME
            var atleta = await _context.Atleti
                .Include(a => a.Squadra)
                .FirstOrDefaultAsync(a => a.Nome == username);

            // Se l'atleta esiste, verifica la password con BCrypt
            if (atleta != null && BCrypt.Net.BCrypt.Verify(password, atleta.Password))
            {
                if (!atleta.IsAttivo)
                {
                    ErrorMsg = "Utente disabilitato.";
                    return Page();
                }

                HttpContext.Session.SetInt32("UserId", atleta.Id);
                HttpContext.Session.SetString("Ruolo", "Atleta");
                HttpContext.Session.SetString("NomeCompleto", $"{atleta.Nome} {atleta.Cognome}");

                if (atleta.SquadraId != null)
                    HttpContext.Session.SetInt32("SquadraId", (int)atleta.SquadraId);

                return RedirectToPage("/Dashboard");
            }

            // Se arriva qui, nome utente inesistente o password errata
            ErrorMsg = "Nome o Password non validi.";
            return Page();
        }
    }
}