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
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            Parametri = await _context.Parametri.OrderBy(p => p.Nome).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(int Id, string Nome, int ValoreMinimo, int ValoreMassimo, bool IsCalcoloCarico, bool IsAttivo)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            // 1. PROTEZIONE PARAMETRI DI SISTEMA
            bool isFisso = (Nome.ToLower() == "rpe" || Nome.ToLower().Contains("sonno") || Nome.ToLower() == "stress");
            if (isFisso && !IsAttivo)
            {
                TempData["Errore"] = $"Il parametro '{Nome}' è di base per l'applicazione e non può essere disattivato.";
                return RedirectToPage();
            }

            // 2. CONTROLLO LIMITE MASSIMO (6 ATTIVI)
            if (IsAttivo)
            {
                // Conta quanti ALTRI parametri sono già attivi
                int attiviCount = await _context.Parametri.CountAsync(p => p.IsAttivo && p.Id != Id);

                if (attiviCount >= 6)
                {
                    TempData["Errore"] = "Limite raggiunto: puoi avere al massimo 6 parametri attivi in totale (3 fissi + 3 personalizzati). Disattivane uno vecchio prima di attivare questo.";
                    return RedirectToPage();
                }
            }

            // 3. GESTIONE UNICITÀ RPE
            if (IsCalcoloCarico)
            {
                var altriRpe = await _context.Parametri.Where(p => p.IsCalcoloCarico && p.Id != Id).ToListAsync();
                foreach (var p in altriRpe) p.IsCalcoloCarico = false;
            }

            // 4. SALVATAGGIO
            if (Id == 0)
            {
                var nuovo = new Parametro
                {
                    Nome = Nome,
                    ValoreMinimo = ValoreMinimo,
                    ValoreMassimo = ValoreMassimo,
                    IsCalcoloCarico = IsCalcoloCarico,
                    IsAttivo = IsAttivo
                };
                _context.Parametri.Add(nuovo);
            }
            else
            {
                var esistente = await _context.Parametri.FindAsync(Id);
                if (esistente != null)
                {
                    esistente.Nome = Nome;
                    esistente.ValoreMinimo = ValoreMinimo;
                    esistente.ValoreMassimo = ValoreMassimo;
                    esistente.IsCalcoloCarico = IsCalcoloCarico;
                    esistente.IsAttivo = IsAttivo;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            var param = await _context.Parametri.FindAsync(id);
            if (param != null)
            {
                bool isFisso = (param.Nome.ToLower() == "rpe" || param.Nome.ToLower().Contains("sonno") || param.Nome.ToLower() == "stress");
                if (isFisso)
                {
                    TempData["Errore"] = "Impossibile disattivare un parametro di sistema.";
                    return RedirectToPage();
                }

                param.IsAttivo = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}