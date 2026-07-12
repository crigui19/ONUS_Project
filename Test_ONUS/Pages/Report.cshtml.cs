using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test_ONUS.Data;
using Test_ONUS.Models;
using Microsoft.AspNetCore.Http;

namespace Test_ONUS.Pages
{
    public class ReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Variables needed for the Report graphics
        public string NomeAtletaSelezionato { get; set; } = "";
        public int AltezzaAtleta { get; set; }
        public double PesoAtleta { get; set; }
        public double CaricoSettimanale { get; set; }
        public List<double> DatiACWR { get; set; } = new List<double>();
        public List<SessioneAllenamento> SessioniAtleta { get; set; } = new List<SessioneAllenamento>();

        public async Task<IActionResult> OnGetAsync(int? atletaId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Login");

            // Find the ID passed from the "Analisi" page or use the logged-in user's ID
            int idDaCercare = atletaId ?? userId.Value;

            var atleta = await _context.Atleti.FindAsync(idDaCercare);

            if (atleta != null)
            {
                NomeAtletaSelezionato = $"{atleta.Nome} {atleta.Cognome}";
                AltezzaAtleta = atleta.Altezza;
                PesoAtleta = atleta.Peso;

                // Fetch sessions directly from the context instead of using Include on Atleta
                // Order descending (newest at the top)
                SessioniAtleta = await _context.Sessioni
                    .Include(s => s.Valori)
                        .ThenInclude(v => v.Parametro)
                    .Where(s => s.AtletaId == idDaCercare)
                    .OrderByDescending(s => s.Data)
                    .ToListAsync();

                // Load last 7 days calculation
                var dataInizio = DateTime.Now.Date.AddDays(-7);
                CaricoSettimanale = SessioniAtleta
                    .Where(s => s.Data >= dataInizio)
                    .Sum(s => s.CaricoCalcolato);

                // Simple ACWR Calculation (Acute Load / Chronic Load divided by 4)
                double acute = SessioniAtleta.Where(s => s.Data >= DateTime.Now.AddDays(-7)).Sum(s => s.CaricoCalcolato);
                double chronic = SessioniAtleta.Where(s => s.Data >= DateTime.Now.AddDays(-28)).Sum(s => s.CaricoCalcolato) / 4.0;

                double currentAcwr = chronic > 0 ? Math.Round(acute / chronic, 2) : 0;
                DatiACWR.Add(currentAcwr);
            }

            return Page();
        }
    }
}