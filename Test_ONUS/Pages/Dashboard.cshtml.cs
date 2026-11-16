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
            var squadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return RedirectToPage("/Login");

            int? idSquadraFiltro = squadraId;

            if (ruolo == "Staff")
            {
                if (squadraId == null)
                    return RedirectToPage("/Login");

                // Carica solo gli atleti attivi della squadra dello Staff loggato
                Atleti = await _context.Atleti
                    .AsNoTracking()
                    .Where(a => a.IsAttivo && a.SquadraId == squadraId.Value)
                    .OrderBy(a => a.Cognome)
                    .ThenBy(a => a.Nome)
                    .ToListAsync();
            }
            else
            {
                Atleti = await _context.Atleti
                    .AsNoTracking()
                    .Where(a => a.Id == userId.Value && a.IsAttivo)
                    .ToListAsync();

                var atletaAttuale = Atleti.FirstOrDefault();
                if (atletaAttuale != null)
                {
                    idSquadraFiltro = atletaAttuale.SquadraId;
                }
            }

            ParametriDashboard = await _context.Parametri
                .AsNoTracking()
                .Where(p => p.IsAttivo && (p.SquadraId == null || p.SquadraId == idSquadraFiltro))
                .OrderBy(p => p.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            int AtletaId,
            Dictionary<int, string>? ValoriParametri,
            string? Note,
            int? DurataTotaleMinuti,
            int? TempoEffettivoMinuti)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var squadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return RedirectToPage("/Login");

            var atleta = await _context.Atleti
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == AtletaId && a.IsAttivo);

            if (atleta == null)
            {
                TempData["Errore"] = "Atleta non trovato o non attivo.";
                return RedirectToPage();
            }

            // Controllo permessi: l'atleta può inserire solo per sé; lo Staff solo per la propria squadra
            bool autorizzato = (ruolo == "Atleta" && userId.Value == atleta.Id)
                            || (ruolo == "Staff" && squadraId.HasValue && atleta.SquadraId == squadraId.Value);

            if (!autorizzato)
                return Forbid();

            if (ValoriParametri == null || !ValoriParametri.Any())
            {
                TempData["Errore"] = "Compila almeno i parametri richiesti prima di salvare.";
                return RedirectToPage();
            }

            var parametriValidi = await _context.Parametri
                .AsNoTracking()
                .Where(p => p.IsAttivo && (p.SquadraId == null || p.SquadraId == atleta.SquadraId))
                .ToDictionaryAsync(p => p.Id);

            // Se la squadra ha definito un parametro custom per il carico, usa quello; altrimenti usa il globale (RPE)
            var parametroCarico = parametriValidi.Values
                .Where(p => p.IsCalcoloCarico)
                .OrderByDescending(p => p.SquadraId.HasValue)
                .FirstOrDefault();

            int durataTotale = (DurataTotaleMinuti.HasValue && DurataTotaleMinuti.Value > 0)
                ? DurataTotaleMinuti.Value
                : 90;

            int tempoEffettivo = (TempoEffettivoMinuti.HasValue && TempoEffettivoMinuti.Value > 0)
                ? Math.Min(TempoEffettivoMinuti.Value, durataTotale)
                : Math.Min(80, durataTotale);

            var sessione = new SessioneAllenamento
            {
                AtletaId = atleta.Id,
                Data = DateTime.UtcNow,
                DurataTotaleMinuti = durataTotale,
                TempoEffettivoMinuti = tempoEffettivo,
                Note = Note?.Trim(),
                Valori = new List<ValoreSessione>()
            };

            foreach (var item in ValoriParametri)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                    continue;

                if (!parametriValidi.TryGetValue(item.Key, out var paramDef))
                    continue;

                if (!int.TryParse(item.Value.Trim(), out int valoreNumerico)
                    || valoreNumerico < paramDef.ValoreMinimo
                    || valoreNumerico > paramDef.ValoreMassimo)
                {
                    TempData["Errore"] = $"Valore non valido per '{paramDef.Nome}' (ammesso tra {paramDef.ValoreMinimo} e {paramDef.ValoreMassimo}).";
                    return RedirectToPage();
                }

                sessione.Valori.Add(new ValoreSessione
                {
                    ParametroId = item.Key,
                    Valore = valoreNumerico
                });
            }

            if (parametroCarico != null && !sessione.Valori.Any(v => v.ParametroId == parametroCarico.Id))
            {
                TempData["Errore"] = $"L'inserimento del valore '{parametroCarico.Nome}' è obbligatorio.";
                return RedirectToPage();
            }

            _context.Sessioni.Add(sessione);
            await _context.SaveChangesAsync();

            TempData["MessaggioSuccesso"] = "Sessione registrata con successo!";
            return RedirectToPage();
        }
    }
}