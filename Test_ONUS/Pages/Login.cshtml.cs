using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.DataProtection;
using Test_ONUS.Data;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;

namespace Test_ONUS.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;

        public LoginModel(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("Onus.Auth.v1");
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMsg { get; set; } = string.Empty;

        public void OnGet()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("OnusAuth");
        }

        public IActionResult OnPost()
        {
            // 1. Cerca tra i Preparatori
            var preparatore = _context.PreparatoriAtletici
                .FirstOrDefault(p => p.Email == Username || (p.Nome.ToLower() + p.Cognome.ToLower()) == Username.ToLower());

            if (preparatore != null && BCrypt.Net.BCrypt.Verify(Password, preparatore.Password))
            {
                ImpostaSessioneECookie(preparatore.Id, "Staff", preparatore.Nome, preparatore.Cognome, preparatore.SquadraId);
                return RedirectToPage("/Dashboard");
            }

            // 2. Cerca tra gli Atleti
            var atleta = _context.Atleti
                .FirstOrDefault(a => (a.Nome.ToLower() + a.Cognome.ToLower()) == Username.ToLower() && a.Password == Password);

            if (atleta != null)
            {
                ImpostaSessioneECookie(atleta.Id, "Atleta", atleta.Nome, atleta.Cognome, (int)atleta.SquadraId);
                return RedirectToPage("/Analisi");
            }

            ErrorMsg = "Invalid Username or Password.";
            return Page();
        }

        private void ImpostaSessioneECookie(int id, string role, string nome, string cognome, int squadraId)
        {
            // Imposta Sessione ("Ruolo" resta in italiano perché usato nel Layout per i permessi)
            HttpContext.Session.SetInt32("UserId", id);
            HttpContext.Session.SetString("Ruolo", role);
            HttpContext.Session.SetString("Nome", nome);
            HttpContext.Session.SetString("Cognome", cognome);
            HttpContext.Session.SetString("NomeCompleto", $"{nome} {cognome}");
            HttpContext.Session.SetInt32("SquadraId", squadraId);

            // Crea Cookie Persistente e Sicuro per la funzionalità "Remember Me"
            var encryptedId = _protector.Protect(id.ToString());

            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(60), // 60 giorni di validità
                HttpOnly = true,
                IsEssential = true,
                Secure = true
            };

            Response.Cookies.Append("OnusAuth", encryptedId, cookieOptions);
        }
    }
}