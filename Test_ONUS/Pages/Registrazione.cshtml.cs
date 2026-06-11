using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Test_ONUS.Data;
using Test_ONUS.Models;
using System.ComponentModel.DataAnnotations;

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

        public string ErrorMsg { get; set; } = "";

        // Classe di supporto per il form
        public class RegistrazioneInput
        {
            [Required] public string Nome { get; set; } = "";
            [Required] public string Cognome { get; set; } = "";
            [Required] public string Password { get; set; } = "";
            [Required] public string NomeSquadra { get; set; } = "";
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            try
            {
                // 1. Creiamo la Squadra
                var nuovaSquadra = new Squadra
                {
                    Nome = Input.NomeSquadra,
                    // Inizializziamo le liste per evitare errori di validazione se richiesto dalle classi 'required'
                    Preparatori = new List<PreparatoreAtletico>(),
                    Atleti = new List<Atleta>()
                };

                _context.Squadre.Add(nuovaSquadra);
                // Salviamo per ottenere l'ID della squadra
                await _context.SaveChangesAsync();

                // 2. Creiamo il Preparatore collegandolo alla squadra appena creata
                var nuovoCoach = new PreparatoreAtletico
                {
                    Nome = Input.Nome,
                    Cognome = Input.Cognome,


                    Password = BCrypt.Net.BCrypt.HashPassword(Input.Password),
                    SquadraId = nuovaSquadra.Id,
                    Squadra = nuovaSquadra
                };

                _context.PreparatoriAtletici.Add(nuovoCoach);
                await _context.SaveChangesAsync();

                // 3. Reindirizziamo al Login
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