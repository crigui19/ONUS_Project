using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;

namespace Test_ONUS.Pages
{
    public class GestioneParametriModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GestioneParametriModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Parametro> Parametri { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            Parametri = await _context.Parametri
                .AsNoTracking()
                .Where(p => p.SquadraId == null || p.SquadraId == squadraId.Value)
                .OrderBy(p => p.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(
     int Id,
     string Nome,
     int ValoreMinimo,
     int ValoreMassimo,
     bool IsAttivo)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(Nome))
            {
                TempData["Errore"] = "Il nome del parametro è obbligatorio.";
                return RedirectToPage();
            }

            if (ValoreMinimo >= ValoreMassimo)
            {
                TempData["Errore"] = "Il valore minimo deve essere inferiore al valore massimo.";
                return RedirectToPage();
            }

            var nomePulito = Nome.Trim();

            if (IsAttivo)
            {
                int attiviCount = await _context.Parametri.CountAsync(p =>
                    p.IsAttivo && p.Id != Id && (p.SquadraId == null || p.SquadraId == squadraId.Value));

                if (attiviCount >= 6)
                {
                    TempData["Errore"] = "Limite raggiunto: puoi avere al massimo 6 parametri attivi. Disattivane uno vecchio prima di attivare questo.";
                    return RedirectToPage();
                }
            }

            if (Id == 0)
            {
                bool nomeDuplicato = await _context.Parametri.AnyAsync(p =>
                    (p.SquadraId == null || p.SquadraId == squadraId.Value)
                    && p.Nome.ToLower() == nomePulito.ToLower());

                if (nomeDuplicato)
                {
                    TempData["Errore"] = $"Esiste già un parametro chiamato '{nomePulito}'.";
                    return RedirectToPage();
                }

                var nuovo = new Parametro
                {
                    Nome = nomePulito,
                    ValoreMinimo = ValoreMinimo,
                    ValoreMassimo = ValoreMassimo,
                    IsCalcoloCarico = false,
                    IsAttivo = IsAttivo,
                    SquadraId = squadraId.Value
                };
                _context.Parametri.Add(nuovo);
            }
            else
            {
                var esistente = await _context.Parametri.FindAsync(Id);
                if (esistente == null)
                    return NotFound();

                if (esistente.SquadraId == null)
                {
                    TempData["Errore"] = "Non puoi modificare i parametri di sistema. Creane uno personalizzato.";
                    return RedirectToPage();
                }

                if (esistente.SquadraId != squadraId.Value)
                    return Forbid();

                esistente.Nome = nomePulito;
                esistente.ValoreMinimo = ValoreMinimo;
                esistente.ValoreMassimo = ValoreMassimo;
                esistente.IsCalcoloCarico = false;
                esistente.IsAttivo = IsAttivo;
            }

            await _context.SaveChangesAsync();
            TempData["MessaggioSuccesso"] = "Parametro salvato correttamente.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            var param = await _context.Parametri.FindAsync(id);
            if (param == null)
                return NotFound();

            if (param.SquadraId == null)
            {
                TempData["Errore"] = "Impossibile disattivare un parametro di sistema.";
                return RedirectToPage();
            }

            if (param.SquadraId != squadraId.Value)
                return Forbid();

            param.IsAttivo = false;
            await _context.SaveChangesAsync();

            TempData["MessaggioSuccesso"] = $"Parametro '{param.Nome}' disattivato.";
            return RedirectToPage();
        }
    }
}