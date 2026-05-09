using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;

namespace Test_ONUS.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Atleta> Atleti { get; set; } = new();
        public List<Parametro> ParametriDashboard { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");

            if (userId == null) return RedirectToPage("/Login");

            int? idSquadraFiltro = null;

            if (ruolo == "Staff")
            {
                Atleti = await _context.Atleti.Where(a => a.IsAttivo).ToListAsync();
            }
            else
            {
                Atleti = await _context.Atleti.Where(a => a.Id == userId && a.IsAttivo).ToListAsync();
                var atletaAttuale = Atleti.FirstOrDefault();
                if (atletaAttuale != null)
                {
                    idSquadraFiltro = atletaAttuale.SquadraId;
                }
            }

            ParametriDashboard = await _context.Parametri
                .Where(p => p.IsAttivo && (p.SquadraId == null || p.SquadraId == idSquadraFiltro))
                .OrderBy(p => p.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int AtletaId, Dictionary<int, string> ValoriParametri, string Note)
        {
            var sessione = new SessioneAllenamento
            {
                AtletaId = AtletaId,
                Data = DateTime.Now,
                DurataTotaleMinuti = 90,
                TempoEffettivoMinuti = 80,
                Note = Note,
                Valori = new List<ValoreSessione>()
            };

            var parametroRpe = await _context.Parametri.FirstOrDefaultAsync(p => p.IsCalcoloCarico);
            int rpeId = parametroRpe?.Id ?? 0;

            // SALVATAGGIO UNIVERSALE: Tutto finisce nella lista Valori
            foreach (var item in ValoriParametri)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    sessione.Valori.Add(new ValoreSessione
                    {
                        ParametroId = item.Key,
                        Valore = int.Parse(item.Value)
                    });
                }
            }

            // Controllo di sicurezza controllando la lista
            bool hasRpe = sessione.Valori.Any(v => v.ParametroId == rpeId);
            if (!hasRpe)
            {
                TempData["Errore"] = "L'inserimento del valore RPE (Carico) è obbligatorio.";
                return RedirectToPage();
            }

            _context.Sessioni.Add(sessione);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}