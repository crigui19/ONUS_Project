using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Test_ONUS.Data;
using Test_ONUS.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace Test_ONUS.Pages
{
    public class ProfiloModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProfiloModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            // Pagina vuota, serve solo per il routing del POST
        }

        public async Task<IActionResult>
    OnPostSalvaDatiAsync(string Nome, string Cognome, string Password)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Login");

            var atleta = await _context.Atleti.FindAsync(userId);
            if (atleta != null)
            {
                // Aggiorna i dati nel DB
                atleta.Nome = Nome;
                atleta.Cognome = Cognome;
                // Se volessi logica più complessa per la password, andrebbe qui
                if (!string.IsNullOrWhiteSpace(Password))
                {
                    atleta.Password = Password;
                }

                await _context.SaveChangesAsync();

                // Aggiorna la Sessione così vedi subito le modifiche senza rifare il login
                HttpContext.Session.SetString("Nome", atleta.Nome);
                HttpContext.Session.SetString("Cognome", atleta.Cognome);
                HttpContext.Session.SetString("NomeCompleto", $"{atleta.Nome} {atleta.Cognome}");
            }

            // Torna alla pagina da cui sei arrivato
            return Redirect(Request.Headers["Referer"].ToString());
        }
        public IActionResult OnPostCambiaLingua(string culture)
        {
            if (culture != null)
            {
                // Imposta il cookie che ASP.NET Core usa per determinare la lingua
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }

            // Ricarica la pagina da cui sei arrivato
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
