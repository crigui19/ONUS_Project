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

        // Variabili necessarie per far funzionare la grafica del Report
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

            // Cerca l'ID passato dalla pagina "Analisi" o usa quello dell'utente loggato
            int idDaCercare = atletaId ?? userId.Value;

            var atleta = await _context.Atleti
                .Include(a => a.SessioniAllenamento)
                    .ThenInclude(s => s.Valori)
                        .ThenInclude(v => v.Parametro)
                .FirstOrDefaultAsync(a => a.Id == idDaCercare);

            if (atleta != null)
            {
                NomeAtletaSelezionato = $"{atleta.Nome} {atleta.Cognome}";
                AltezzaAtleta = atleta.Altezza;
                PesoAtleta = atleta.Peso;

                // Prendi le sessioni in ordine decrescente (le più recenti in alto nella tabella)
                SessioniAtleta = atleta.SessioniAllenamento.OrderByDescending(s => s.Data).ToList();

                // Calcolo Carico ultimi 7 giorni
                var dataInizio = DateTime.Now.Date.AddDays(-7);
                CaricoSettimanale = atleta.SessioniAllenamento
                    .Where(s => s.Data >= dataInizio)
                    .Sum(s => s.CaricoCalcolato);

                // Calcolo ACWR Semplice (Carico Acuto / Carico Cronico diviso 4)
                double acute = atleta.SessioniAllenamento.Where(s => s.Data >= DateTime.Now.AddDays(-7)).Sum(s => s.CaricoCalcolato);
                double chronic = atleta.SessioniAllenamento.Where(s => s.Data >= DateTime.Now.AddDays(-28)).Sum(s => s.CaricoCalcolato) / 4.0;

                double currentAcwr = chronic > 0 ? Math.Round(acute / chronic, 2) : 0;
                DatiACWR.Add(currentAcwr);
            }

            return Page();
        }
    }
}