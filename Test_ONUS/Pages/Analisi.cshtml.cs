using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Test_ONUS.Data;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Models;

namespace Test_ONUS.Pages
{
    public class AnalisiModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AnalisiModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Atleta> Atleti { get; set; } = new();
        public List<SessioneAllenamento> SessioniAtleta { get; set; } = new();

        public string[] EtichetteDate { get; set; }
        public double[] DatiAcute { get; set; }
        public double[] DatiChronic { get; set; }
        public double[] DatiACWR { get; set; }
        // NUOVA PROPRIETÀ PER LO Z-SCORE
        public double[] DatiZScore { get; set; }
        public string NomeAtletaSelezionato { get; set; } = "";
        public bool IsStaff { get; set; } = false;
        public double CaricoSettimanale { get; set; }
        public int AltezzaAtleta { get; set; }
        public double PesoAtleta { get; set; }

        public async Task<IActionResult> OnGetAsync(int? atletaId, string filtroTempo) // Aggiunto filtroTempo
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");

            if (userId == null) return RedirectToPage("/Login");

            IsStaff = (ruolo == "Staff");

            // --- CARICAMENTO ATLETI ---
            if (IsStaff)
            {
                Atleti = await _context.Atleti.Where(a => a.IsAttivo).ToListAsync();
            }
            else
            {
                Atleti = await _context.Atleti.Where(a => a.Id == userId).ToListAsync();
                atletaId = userId;
            }

            // --- LOGICA SE UN ATLETA E' SELEZIONATO ---
            if (atletaId.HasValue)
            {
                var atleta = Atleti.FirstOrDefault(a => a.Id == atletaId);
                if (atleta == null && !IsStaff) return RedirectToPage("/Login");

                if (atleta != null)
                {
                    NomeAtletaSelezionato = $"{atleta.Nome} {atleta.Cognome}";
                    AltezzaAtleta = atleta.Altezza;
                    PesoAtleta = atleta.Peso;
                }
                // 1. Recupero TUTTE le sessioni dell'atleta dal database
                var tutteLeSessioni = await _context.Sessioni
                    .Include(s => s.Valori)
                    .ThenInclude(v => v.Parametro)
                    .Where(s => s.AtletaId == atletaId)
                    .OrderByDescending(s => s.Data)
                    .ToListAsync();

                // 2. Calcolo Carico Settimanale FISSO (sempre ultimi 7 giorni reali)
                var dataLimiteSettimana = DateTime.Now.AddDays(-7);
                CaricoSettimanale = tutteLeSessioni
                    .Where(s => s.Data >= dataLimiteSettimana)
                    .Sum(s => s.CaricoCalcolato);

                // 3. APPLICAZIONE DEL FILTRO TEMPORALE
                DateTime dataInizioFiltro = DateTime.MinValue;

                switch (filtroTempo)
                {
                    case "ultimi7":
                        SessioniAtleta = tutteLeSessioni.Take(7).ToList();
                        break;
                    case "ultimaSettimana":
                        dataInizioFiltro = DateTime.Now.AddDays(-7);
                        SessioniAtleta = tutteLeSessioni.Where(s => s.Data >= dataInizioFiltro).ToList();
                        break;
                    case "meseCorrente":
                        dataInizioFiltro = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        SessioniAtleta = tutteLeSessioni.Where(s => s.Data >= dataInizioFiltro).ToList();
                        break;
                    case "tutto":
                    default: // Ora il comportamento base è "Intera Stagione"
                        SessioniAtleta = tutteLeSessioni;
                        break;
                }

                // 4. Calcola i grafici basandosi SOLO sui dati filtrati!
                CalcolaGrafici(SessioniAtleta);
            }
            else
            {
                // Se non c'è atleta, inizializza la lista vuota per evitare errori nella View
                SessioniAtleta = new List<SessioneAllenamento>();
            }

            return Page();
        }

        private void CalcolaGrafici(List<SessioneAllenamento> sessioni)
        {
            var date = new List<string>();
            var acute = new List<double>();
            var chronic = new List<double>();
            var acwr = new List<double>();
            var zScores = new List<double>(); // Corretto: ora popoliamo questa lista

            DateTime oggi = DateTime.Today;

            for (int i = 28; i >= 0; i--)
            {
                DateTime giornoCorrente = oggi.AddDays(-i);
                date.Add(giornoCorrente.ToString("dd/MM"));

                double caricoAcuto = sessioni
                    .Where(s => s.Data.Date > giornoCorrente.AddDays(-7) && s.Data.Date <= giornoCorrente)
                    .Sum(s => s.CaricoCalcolato);

                double caricoCronicoTotale = sessioni
                    .Where(s => s.Data.Date > giornoCorrente.AddDays(-28) && s.Data.Date <= giornoCorrente)
                    .Sum(s => s.CaricoCalcolato);

                double mediaCronica = caricoCronicoTotale / 4.0;

                // --- CALCOLO Z-SCORE GIORNALIERO ---
                double caricoGiornaliero = sessioni.Where(s => s.Data.Date == giornoCorrente.Date).Sum(s => s.CaricoCalcolato);

                var carichi28gg = new List<double>();
                for (int j = 0; j < 28; j++)
                {
                    double c = sessioni.Where(s => s.Data.Date == giornoCorrente.AddDays(-j).Date).Sum(s => s.CaricoCalcolato);
                    carichi28gg.Add(c);
                }

                double media28 = carichi28gg.Average();
                double devStd = Math.Sqrt(carichi28gg.Sum(l => Math.Pow(l - media28, 2)) / 28);
                double zScore = devStd > 0 ? Math.Round((caricoGiornaliero - media28) / devStd, 2) : 0;

                acute.Add(caricoAcuto);
                chronic.Add(mediaCronica);
                acwr.Add(mediaCronica > 0 ? Math.Round(caricoAcuto / mediaCronica, 2) : 0);
                zScores.Add(zScore); // Aggiunto alla lista
            }

            EtichetteDate = date.ToArray();
            DatiAcute = acute.ToArray();
            DatiChronic = chronic.ToArray();
            DatiACWR = acwr.ToArray();
            DatiZScore = zScores.ToArray(); // Inviato al grafico
        }

        public async Task<IActionResult> OnPostUpdateSessionAsync(int sessionId, DateTime data, int durata, int rpe, int sonno, int dolore, string note)
        {
            var sessione = await _context.Sessioni
                .Include(s => s.Valori)
                .ThenInclude(v => v.Parametro)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (sessione != null)
            {
                sessione.Data = data;
                sessione.DurataTotaleMinuti = durata;
                sessione.Note = note;

                // Aggiorniamo i valori cercando nella lista
                var valRpe = sessione.Valori.FirstOrDefault(v => v.Parametro.IsCalcoloCarico);
                if (valRpe != null) valRpe.Valore = rpe;

                var valSonno = sessione.Valori.FirstOrDefault(v => v.Parametro.Nome.Contains("Sonno"));
                if (valSonno != null) valSonno.Valore = sonno;

                var valDolore = sessione.Valori.FirstOrDefault(v => v.Parametro.Nome.Contains("Dolore") || v.Parametro.Nome.Contains("Indolenzimento"));
                if (valDolore != null) valDolore.Valore = dolore;

                await _context.SaveChangesAsync();
                return RedirectToPage(new { atletaId = sessione.AtletaId });
            }

            return RedirectToPage();
        }
    }
}