using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;

namespace Test_ONUS.Pages
{
    public class ReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string NomeAtletaSelezionato { get; set; } = string.Empty;
        public int AltezzaAtleta { get; set; }
        public double PesoAtleta { get; set; }
        public double CaricoSettimanale { get; set; }
        public List<double> DatiACWR { get; set; } = new();
        public List<SessioneAllenamento> SessioniAtleta { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? atletaId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var squadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return RedirectToPage("/Login");

            // Un atleta può vedere solo il proprio report; lo Staff può scegliere un atleta della propria squadra
            int idDaCercare = (ruolo == "Staff" && atletaId.HasValue) ? atletaId.Value : userId.Value;

            var atleta = await _context.Atleti
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == idDaCercare);

            if (atleta == null)
                return NotFound();

            bool autorizzato = (ruolo == "Atleta" && atleta.Id == userId.Value)
                            || (ruolo == "Staff" && squadraId.HasValue && atleta.SquadraId == squadraId.Value);

            if (!autorizzato)
                return Forbid();

            NomeAtletaSelezionato = $"{atleta.Nome} {atleta.Cognome}";
            AltezzaAtleta = atleta.Altezza;
            PesoAtleta = atleta.Peso;

            SessioniAtleta = await _context.Sessioni
                .AsNoTracking()
                .Include(s => s.Valori)
                    .ThenInclude(v => v.Parametro)
                .Where(s => s.AtletaId == atleta.Id)
                .OrderByDescending(s => s.Data)
                .ToListAsync();

            var oggiUtc = DateTime.UtcNow.Date;
            var dataInizio7gg = oggiUtc.AddDays(-6);
            var dataInizio28gg = oggiUtc.AddDays(-27);

            CaricoSettimanale = SessioniAtleta
                .Where(s => s.Data.Date >= dataInizio7gg)
                .Sum(s => s.CaricoCalcolato);

            double acute = CaricoSettimanale;
            double chronic = SessioniAtleta
                .Where(s => s.Data.Date >= dataInizio28gg)
                .Sum(s => s.CaricoCalcolato) / 4.0;

            double currentAcwr = chronic > 0 ? Math.Round(acute / chronic, 2) : 0;
            DatiACWR.Add(currentAcwr);

            return Page();
        }
    }
}