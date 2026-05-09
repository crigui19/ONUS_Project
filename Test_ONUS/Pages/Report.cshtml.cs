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

        public List<Atleta> Atleti { get; set; } = new();
        public List<Parametro> ParametriExtra { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public List<int> ParametriSelezionatiIds { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public bool IncludiLoad { get; set; } = true;

        public bool MostraAnteprima { get; set; } = false;
        public string NomeAtleta { get; set; } = "TEAM";
        public string FiltroTesto { get; set; } = "";
        public string DataInizio { get; set; } = "";
        public string DataFine { get; set; } = "";

        public double RpeCorrente { get; set; }
        public double RpeVariazione { get; set; }
        public double LoadCorrente { get; set; }
        public double LoadVariazionePercentuale { get; set; }

        public Dictionary<int, double> MedieDinamiche { get; set; } = new();
        public Dictionary<int, double> VariazioniDinamiche { get; set; } = new();

        public string[] EtichetteDate { get; set; } = Array.Empty<string>();
        public double[] DatiLoad { get; set; } = Array.Empty<double>();

        public async Task<IActionResult> OnGetAsync(string atletaId, string filtroTempo)
        {
            var ruolo = HttpContext.Session.GetString("Ruolo");
            if (ruolo != "Staff") return RedirectToPage("/Login");

            Atleti = await _context.Atleti.Where(a => a.IsAttivo).ToListAsync();

            ParametriExtra = await _context.Parametri
                .Where(p => p.IsAttivo && !p.IsCalcoloCarico)
                .ToListAsync();

            if (string.IsNullOrEmpty(filtroTempo))
            {
                ParametriSelezionatiIds = ParametriExtra.Select(p => p.Id).ToList();
                return Page();
            }

            MostraAnteprima = true;

            DateTime inizio = DateTime.Today, fine = DateTime.Today;
            DateTime inPrec = DateTime.Today, finePrec = DateTime.Today;

            switch (filtroTempo)
            {
                case "ultimi7":
                case "ultimaSettimana":
                    inizio = DateTime.Today.AddDays(-7); FiltroTesto = "LAST 7 DAYS";
                    inPrec = inizio.AddDays(-7); finePrec = inizio; break;
                case "meseCorrente":
                    inizio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); FiltroTesto = "CURRENT MONTH";
                    inPrec = inizio.AddMonths(-1); finePrec = inizio; break;
                default:
                    inizio = DateTime.Today.AddDays(-30); FiltroTesto = "LAST MONTH";
                    inPrec = inizio.AddDays(-30); finePrec = inizio; break;
            }

            DataInizio = inizio.ToString("dd/MM/yyyy");
            DataFine = fine.ToString("dd/MM/yyyy");

            var query = _context.Sessioni.Include(s => s.Valori).ThenInclude(v => v.Parametro).AsQueryable();

            if (atletaId != "tutti" && int.TryParse(atletaId, out int idAttr))
            {
                query = query.Where(s => s.AtletaId == idAttr);
                var a = Atleti.FirstOrDefault(x => x.Id == idAttr);
                if (a != null) NomeAtleta = $"{a.Nome} {a.Cognome}";
            }
            else { NomeAtleta = "TEAM (SQUADRA)"; }

            var sessioniTutte = await query.ToListAsync();
            var sCorr = sessioniTutte.Where(s => s.Data >= inizio && s.Data <= fine).ToList();
            var sPrec = sessioniTutte.Where(s => s.Data >= inPrec && s.Data < finePrec).ToList();

            if (sCorr.Any())
            {
                // CORREZIONE: Cerca l'ID in modo sicuro dal database
                int idRpe = await _context.Parametri.Where(p => p.IsCalcoloCarico).Select(p => p.Id).FirstOrDefaultAsync();

                var valoriRpeCorr = sCorr.SelectMany(s => s.Valori).Where(v => v.ParametroId == idRpe).Select(v => v.Valore).ToList();
                RpeCorrente = valoriRpeCorr.Any() ? Math.Round(valoriRpeCorr.Average(), 1) : 0;

                var valoriRpePrec = sPrec.SelectMany(s => s.Valori).Where(v => v.ParametroId == idRpe).Select(v => v.Valore).ToList();
                double rpeP = valoriRpePrec.Any() ? Math.Round(valoriRpePrec.Average(), 1) : 0;

                RpeVariazione = Math.Round(RpeCorrente - rpeP, 1);

                LoadCorrente = sCorr.Sum(s => s.CaricoCalcolato);
                double loadP = sPrec.Sum(s => s.CaricoCalcolato);
                LoadVariazionePercentuale = loadP > 0 ? ((LoadCorrente - loadP) / loadP) * 100 : 0;

                foreach (var pId in ParametriSelezionatiIds)
                {
                    var vCorr = sCorr.SelectMany(s => s.Valori).Where(v => v.ParametroId == pId).Select(v => v.Valore).ToList();
                    double mCorr = vCorr.Any() ? Math.Round(vCorr.Average(), 1) : 0;
                    MedieDinamiche[pId] = mCorr;

                    var vPrec = sPrec.SelectMany(s => s.Valori).Where(v => v.ParametroId == pId).Select(v => v.Valore).ToList();
                    double mPrec = vPrec.Any() ? vPrec.Average() : 0;
                    VariazioniDinamiche[pId] = Math.Round(mCorr - mPrec, 1);
                }

                EtichetteDate = sCorr.OrderBy(s => s.Data).Select(s => s.Data.ToString("dd/MM")).Distinct().ToArray();
                DatiLoad = sCorr.OrderBy(s => s.Data).GroupBy(s => s.Data.Date).Select(g => (double)g.Sum(s => s.CaricoCalcolato)).ToArray();
            }

            return Page();
        }
    }
}