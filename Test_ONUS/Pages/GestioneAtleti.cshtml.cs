using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Test_ONUS.Pages
{
    public class GestioneAtletiModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GestioneAtletiModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public List<Atleta> Atleti { get; set; } = new();

        [BindProperty]
        public string NomeSquadra { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null) return RedirectToPage("/Login");

            // Carica nome squadra
            var squadra = await _context.Squadre.FindAsync(squadraId);
            NomeSquadra = squadra?.Nome ?? "Tua Squadra";

            // Carica solo atleti della TUA squadra
            Atleti = await _context.Atleti
                                   .Where(a => a.SquadraId == squadraId)
                                   .OrderBy(a => a.Cognome)
                                   .ToListAsync();
            return Page();
        }

        // Cambio nome squadra
        public async Task<IActionResult> OnPostAggiornaSquadraAsync()
        {
            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId != null && !string.IsNullOrWhiteSpace(NomeSquadra))
            {
                var squadra = await _context.Squadre.FindAsync(squadraId);
                if (squadra != null)
                {
                    squadra.Nome = NomeSquadra;
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("NomeSquadra", squadra.Nome);
                }
            }
            return RedirectToPage();
        }

        // Salvataggio Atleta (con Foto e SquadraId automatico)
        public async Task<IActionResult> OnPostSaveAsync(int Id, string Nome, string Cognome, string Password, string FotoUrlCorrente, bool IsAttivo, IFormFile? UploadImmagine, bool IsInfortunato, bool IsInRiabilitazione, string DescrizioneInfortunio)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null) return RedirectToPage("/Login");

            // 1. GESTIONE FOTO
            string percorsoFinaleFoto = FotoUrlCorrente;

            if (UploadImmagine != null && UploadImmagine.Length > 0)
            {
                string cartellaUpload = Path.Combine(_environment.WebRootPath, "Images");
                if (!Directory.Exists(cartellaUpload)) Directory.CreateDirectory(cartellaUpload);

                string nomeFileUnivoco = Guid.NewGuid().ToString() + Path.GetExtension(UploadImmagine.FileName);
                string percorsoCompleto = Path.Combine(cartellaUpload, nomeFileUnivoco);

                using (var fileStream = new FileStream(percorsoCompleto, FileMode.Create))
                {
                    await UploadImmagine.CopyToAsync(fileStream);
                }
                percorsoFinaleFoto = "/Images/" + nomeFileUnivoco;
            }

            if (string.IsNullOrEmpty(percorsoFinaleFoto)) percorsoFinaleFoto = "/Img/default.png";

            // 2. SALVATAGGIO DB
            if (Id == 0)
            {
                var nuovo = new Atleta
                {
                    Nome = Nome,
                    Cognome = Cognome,
                    Password = Password,
                    FotoUrl = percorsoFinaleFoto,
                    IsAttivo = IsAttivo,
                    SquadraId = squadraId // Assegna automaticamente alla squadra del coach
                };
                _context.Atleti.Add(nuovo);
            }
            else
            {
                // Modifica sicura: controlla anche SquadraId
                var esistente = await _context.Atleti.FirstOrDefaultAsync(a => a.Id == Id && a.SquadraId == squadraId);
                if (esistente != null)
                {
                    esistente.Nome = Nome;
                    esistente.Cognome = Cognome;
                    esistente.Password = Password;
                    esistente.FotoUrl = percorsoFinaleFoto;
                    esistente.IsAttivo = IsAttivo;
                    esistente.IsInfortunato = IsInfortunato;
                    esistente.IsInRiabilitazione = IsInRiabilitazione;
                    esistente.DescrizioneInfortunio = DescrizioneInfortunio;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");

            var atleta = await _context.Atleti.FirstOrDefaultAsync(a => a.Id == id && a.SquadraId == squadraId);
            if (atleta != null)
            {
                _context.Atleti.Remove(atleta);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}