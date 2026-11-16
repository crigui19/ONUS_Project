using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using Test_ONUS.Data;
using Test_ONUS.Models;

namespace Test_ONUS.Controllers
{
    [Route("[controller]")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataProtector _protector;

        // Iniettiamo anche IDataProtectionProvider come hai fatto nel LoginModel
        public AccountController(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _protector = dataProtectionProvider.CreateProtector("Onus.Auth.v1");
        }

        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
                return Redirect("/Login");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var nome = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "Utente";
            var cognome = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "";

            if (string.IsNullOrEmpty(email))
                return Redirect("/Login");

            var preparatore = await _context.PreparatoriAtletici
                .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower() || p.GoogleId == googleId);

            if (preparatore == null)
            {
                // Registrazione automatica con squadra predefinita
                preparatore = new PreparatoreAtletico
                {
                    Nome = nome,
                    Cognome = cognome,
                    Email = email,
                    GoogleId = googleId,
                    Password = null,
                    Squadra = new Squadra
                    {
                        Nome = $"{nome}'s Team",
                        Atleti = new List<Atleta>(),
                        Preparatori = new List<PreparatoreAtletico>()
                    }
                };

                _context.PreparatoriAtletici.Add(preparatore);
                await _context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(preparatore.GoogleId))
            {
                preparatore.GoogleId = googleId;
                await _context.SaveChangesAsync();
            }

            // Invece di usare SignInAsync, richiamiamo esattamente la TUA logica di sessione
            ImpostaSessioneECookie(preparatore.Id, "Staff", preparatore.Nome, preparatore.Cognome, preparatore.SquadraId);

            return Redirect("/Dashboard");
        }

        // Questo metodo è la copia esatta di quello che hai in LoginModel.cs
        private void ImpostaSessioneECookie(int id, string role, string nome, string cognome, int squadraId)
        {
            HttpContext.Session.SetInt32("UserId", id);
            HttpContext.Session.SetString("Ruolo", role);
            HttpContext.Session.SetString("Nome", nome);
            HttpContext.Session.SetString("Cognome", cognome);
            HttpContext.Session.SetString("NomeCompleto", $"{nome} {cognome}");
            HttpContext.Session.SetInt32("SquadraId", squadraId);

            var encryptedId = _protector.Protect(id.ToString());
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(60),
                HttpOnly = true,
                IsEssential = true,
                Secure = true
            };

            Response.Cookies.Append("OnusAuth", encryptedId, cookieOptions);
        }
    }
}