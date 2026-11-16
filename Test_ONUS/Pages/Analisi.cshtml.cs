using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
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

        public string[] EtichetteDate { get; set; } = Array.Empty<string>();
        public double[] DatiAcute { get; set; } = Array.Empty<double>();
        public double[] DatiChronic { get; set; } = Array.Empty<double>();
        public double[] DatiACWR { get; set; } = Array.Empty<double>();
        public double[] DatiZScore { get; set; } = Array.Empty<double>();

        public string NomeAtletaSelezionato { get; set; } = string.Empty;
        public bool IsStaff { get; set; } = false;
        public double CaricoSettimanale { get; set; }
        public double MonotoniaSettimanale { get; set; }
        public double StrainSettimanale { get; set; }
        public int AltezzaAtleta { get; set; }
        public double PesoAtleta { get; set; }

        public async Task<IActionResult> OnGetAsync(int? atletaId, string? filtroTempo)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var squadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return RedirectToPage("/Login");

            IsStaff = (ruolo == "Staff");

            // 1. Caricamento atleti isolato per squadra / ruolo
            if (IsStaff)
            {
                if (squadraId == null)
                    return RedirectToPage("/Login");

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
                    .Where(a => a.Id == userId.Value)
                    .ToListAsync();

                atletaId = userId.Value;
            }

            // 2. Se un atleta è selezionato, verifica che appartenga alla lista autorizzata
            if (atletaId.HasValue)
            {
                var atleta = Atleti.FirstOrDefault(a => a.Id == atletaId.Value);
                if (atleta == null)
                    return Forbid();

                NomeAtletaSelezionato = $"{atleta.Nome} {atleta.Cognome}";
                AltezzaAtleta = atleta.Altezza;
                PesoAtleta = atleta.Peso;

                var tutteLeSessioni = await _context.Sessioni
                    .AsNoTracking()
                    .Include(s => s.Valori)
                        .ThenInclude(v => v.Parametro)
                    .Where(s => s.AtletaId == atleta.Id)
                    .OrderByDescending(s => s.Data)
                    .ToListAsync();

                // Carico settimanale fisso (ultimi 7 giorni UTC)
                var dataLimiteSettimana = DateTime.UtcNow.Date.AddDays(-6);
                CaricoSettimanale = tutteLeSessioni
                    .Where(s => s.Data.Date >= dataLimiteSettimana)
                    .Sum(s => s.CaricoCalcolato);

                // Filtro temporale applicato SOLO alla lista visualizzata in tabella
                switch (filtroTempo)
                {
                    case "ultimi7":
                        SessioniAtleta = tutteLeSessioni.Take(7).ToList();
                        break;
                    case "ultimaSettimana":
                        var inizioSettimana = DateTime.UtcNow.AddDays(-7);
                        SessioniAtleta = tutteLeSessioni.Where(s => s.Data >= inizioSettimana).ToList();
                        break;
                    case "meseCorrente":
                        var oggiUtc = DateTime.UtcNow;
                        var inizioMese = new DateTime(oggiUtc.Year, oggiUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        SessioniAtleta = tutteLeSessioni.Where(s => s.Data >= inizioMese).ToList();
                        break;
                    case "tutto":
                    default:
                        SessioniAtleta = tutteLeSessioni;
                        break;
                }

                // I grafici (ACWR 7:28 e Z-Score 28gg) usano SEMPRE lo storico completo
                CalcolaGrafici(tutteLeSessioni);
            }

            return Page();
        }

        private void CalcolaGrafici(List<SessioneAllenamento> tutteLeSessioni)
        {
            var date = new List<string>(29);
            var acute = new List<double>(29);
            var chronic = new List<double>(29);
            var acwr = new List<double>(29);
            var zScores = new List<double>(29);

            // Pre-aggrega i carichi giornalieri in un dizionario O(1)
            var caricoPerGiorno = tutteLeSessioni
                .GroupBy(s => s.Data.Date)
                .ToDictionary(g => g.Key, g => (double)g.Sum(s => s.CaricoCalcolato));

            DateTime oggi = DateTime.UtcNow.Date;

            for (int i = 28; i >= 0; i--)
            {
                DateTime giornoCorrente = oggi.AddDays(-i);
                date.Add(giornoCorrente.ToString("dd/MM"));

                double caricoGiornaliero = caricoPerGiorno.GetValueOrDefault(giornoCorrente, 0);

                // Carico acuto (ultimi 7 giorni inclusi oggi)
                double caricoAcuto = 0;
                for (int d = 0; d < 7; d++)
                {
                    caricoAcuto += caricoPerGiorno.GetValueOrDefault(giornoCorrente.AddDays(-d), 0);
                }

                // Carico cronico e Z-Score sugli ultimi 28 giorni
                var carichi28gg = new double[28];
                double caricoCronicoTotale = 0;
                for (int j = 0; j < 28; j++)
                {
                    double c = caricoPerGiorno.GetValueOrDefault(giornoCorrente.AddDays(-j), 0);
                    carichi28gg[j] = c;
                    caricoCronicoTotale += c;
                }

                double mediaCronica = caricoCronicoTotale / 4.0;
                double media28 = caricoCronicoTotale / 28.0;
                double varianza28 = carichi28gg.Sum(l => Math.Pow(l - media28, 2)) / 28.0;
                double devStd28 = Math.Sqrt(varianza28);
                double zScore = devStd28 > 0 ? Math.Round((caricoGiornaliero - media28) / devStd28, 2) : 0;

                acute.Add(caricoAcuto);
                chronic.Add(mediaCronica);
                acwr.Add(mediaCronica > 0 ? Math.Round(caricoAcuto / mediaCronica, 2) : 0);
                zScores.Add(zScore);

                // Calcolo Monotonia e Strain di Foster sull'ultimo giorno (oggi)
                if (i == 0)
                {
                    var carichi7gg = carichi28gg.Take(7).ToArray();
                    double media7 = carichi7gg.Average();
                    double devStd7 = Math.Sqrt(carichi7gg.Sum(l => Math.Pow(l - media7, 2)) / 7.0);
                    MonotoniaSettimanale = devStd7 > 0 ? Math.Round(media7 / devStd7, 2) : 0;
                    StrainSettimanale = Math.Round(caricoAcuto * MonotoniaSettimanale, 0);
                }
            }

            EtichetteDate = date.ToArray();
            DatiAcute = acute.ToArray();
            DatiChronic = chronic.ToArray();
            DatiACWR = acwr.ToArray();
            DatiZScore = zScores.ToArray();
        }

        public async Task<IActionResult> OnPostUpdateSessionAsync(
            int sessionId,
            DateTime data,
            int durata,
            int rpe,
            int sonno,
            int dolore,
            string? note)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var squadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return RedirectToPage("/Login");

            var sessione = await _context.Sessioni
                .Include(s => s.Atleta)
                .Include(s => s.Valori)
                    .ThenInclude(v => v.Parametro)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (sessione == null)
                return NotFound();

            // Autorizzazione: Staff della stessa squadra oppure l'atleta proprietario della sessione
            bool autorizzato = (ruolo == "Atleta" && sessione.AtletaId == userId.Value)
                            || (ruolo == "Staff" && squadraId.HasValue && sessione.Atleta.SquadraId == squadraId.Value);

            if (!autorizzato)
                return Forbid();

            // Npgsql richiede DateTime con Kind=Utc per colonne timestamptz
            sessione.Data = DateTime.SpecifyKind(data, DateTimeKind.Utc);
            sessione.DurataTotaleMinuti = durata > 0 ? durata : sessione.DurataTotaleMinuti;
            sessione.Note = note?.Trim();

            var valRpe = sessione.Valori.FirstOrDefault(v => v.Parametro != null && v.Parametro.IsCalcoloCarico);
            if (valRpe != null) valRpe.Valore = rpe;

            var valSonno = sessione.Valori.FirstOrDefault(v => v.Parametro != null && v.Parametro.Nome.Contains("Sonno"));
            if (valSonno != null) valSonno.Valore = sonno;

            var valDolore = sessione.Valori.FirstOrDefault(v => v.Parametro != null &&
                (v.Parametro.Nome.Contains("Dolore") || v.Parametro.Nome.Contains("Indolenzimento")));
            if (valDolore != null) valDolore.Valore = dolore;

            await _context.SaveChangesAsync();
            TempData["MessaggioSuccesso"] = "Sessione aggiornata correttamente.";
            return RedirectToPage(new { atletaId = sessione.AtletaId });
        }
    }
}