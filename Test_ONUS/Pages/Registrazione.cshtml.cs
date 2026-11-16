using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;

namespace Test_ONUS.Pages
{
    public class RegistrazioneModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RegistrazioneModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RegistrazioneInput Input { get; set; } = new();

        public string ErrorMsg { get; set; } = string.Empty;

        public class RegistrazioneInput
        {
            [Required] public string Nome { get; set; } = string.Empty;
            [Required] public string Cognome { get; set; } = string.Empty;
            [Required] public string NomeSquadra { get; set; } = string.Empty;
            [Required, EmailAddress] public string Email { get; set; } = string.Empty;

            [Required]
            [MinLength(8, ErrorMessage = "La password deve contenere almeno 8 caratteri.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Conferma la password")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Le password non coincidono.")]
            public string ConfermaPassword { get; set; } = string.Empty;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var emailNormalizzata = Input.Email.Trim().ToLower();

            var emailEsistente = await _context.PreparatoriAtletici
                .AnyAsync(p => p.Email.ToLower() == emailNormalizzata);

            if (emailEsistente)
            {
                ErrorMsg = "Esiste già un account registrato con questa email.";
                return Page();
            }

            try
            {
                var nuovaSquadra = new Squadra
                {
                    Nome = Input.NomeSquadra.Trim(),
                    Preparatori = new List<PreparatoreAtletico>(),
                    Atleti = new List<Atleta>()
                };

                var nuovoCoach = new PreparatoreAtletico
                {
                    Nome = Input.Nome.Trim(),
                    Cognome = Input.Cognome.Trim(),
                    Email = Input.Email.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                    Squadra = nuovaSquadra
                };

                _context.Squadre.Add(nuovaSquadra);
                _context.PreparatoriAtletici.Add(nuovoCoach);

                // Un unico SaveChangesAsync garantisce una transazione atomica
                await _context.SaveChangesAsync();

                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                ErrorMsg = "Errore durante la registrazione: " + ex.Message;
                return Page();
            }
        }
    }
}