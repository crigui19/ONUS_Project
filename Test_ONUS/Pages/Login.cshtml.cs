using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Test_ONUS.Data;

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
            var cleanUsername = (Username ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(cleanUsername) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMsg = "Inserisci username e password.";
                return Page();
            }

            var normalizedUser = cleanUsername.ToLower();

            // 1. Cerca tra i Preparatori
            var preparatore = _context.PreparatoriAtletici
                .FirstOrDefault(p => p.Email.ToLower() == normalizedUser
                                  || (p.Nome.ToLower() + p.Cognome.ToLower()) == normalizedUser);

            if (preparatore != null && !string.IsNullOrEmpty(preparatore.Password)
                && BCrypt.Net.BCrypt.Verify(Password, preparatore.Password))
            {
                ImpostaSessioneECookie(preparatore.Id, "Staff", preparatore.Nome, preparatore.Cognome, preparatore.SquadraId);
                return RedirectToPage("/Dashboard");
            }

            // 2. Cerca tra gli Atleti
            var atleta = _context.Atleti
                .FirstOrDefault(a => (a.Nome.ToLower() + a.Cognome.ToLower()) == normalizedUser);

            if (atleta != null && !string.IsNullOrEmpty(atleta.Password))
            {
                bool passwordValida;

                // Se è già un hash BCrypt (inizia con $2a$, $2b$ o $2y$), verifica con BCrypt
                if (atleta.Password.StartsWith("$2"))
                {
                    passwordValida = BCrypt.Net.BCrypt.Verify(Password, atleta.Password);
                }
                else
                {
                    // Compatibilità retroattiva: verifica vecchia password in chiaro e aggiornala subito a BCrypt
                    passwordValida = (atleta.Password == Password);
                    if (passwordValida)
                    {
                        atleta.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                        _context.SaveChanges();
                    }
                }

                if (passwordValida)
                {
                    ImpostaSessioneECookie(atleta.Id, "Atleta", atleta.Nome, atleta.Cognome, atleta.SquadraId);
                    return RedirectToPage("/Analisi");
                }
            }

            ErrorMsg = "Username o password non validi.";
            return Page();
        }

        private void ImpostaSessioneECookie(int id, string role, string nome, string cognome, int? squadraId)
        {
            HttpContext.Session.SetInt32("UserId", id);
            HttpContext.Session.SetString("Ruolo", role);
            HttpContext.Session.SetString("Nome", nome);
            HttpContext.Session.SetString("Cognome", cognome);
            HttpContext.Session.SetString("NomeCompleto", $"{nome} {cognome}");

            if (squadraId.HasValue)
            {
                HttpContext.Session.SetInt32("SquadraId", squadraId.Value);
            }

            // Formato allineato con Program.cs: "Ruolo:Id"
            var encryptedPayload = _protector.Protect($"{role}:{id}");

            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(60),
                HttpOnly = true,
                IsEssential = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Append("OnusAuth", encryptedPayload, cookieOptions);
        }
        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("OnusAuth");
            return RedirectToPage("/Login");
        }
    }
}