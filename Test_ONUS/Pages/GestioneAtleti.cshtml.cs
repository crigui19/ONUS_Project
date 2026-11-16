using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;
using WebPush;

namespace Test_ONUS.Pages
{
    public class GestioneAtletiModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public GestioneAtletiModel(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IList<Atleta> Atleti { get; set; } = new List<Atleta>();
        public string NomeSquadra { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            Atleti = await _context.Atleti
                .Where(a => a.SquadraId == squadraId.Value)
                .OrderBy(a => a.Cognome)
                .ThenBy(a => a.Nome)
                .ToListAsync();

            var squadra = await _context.Squadre.FindAsync(squadraId.Value);
            NomeSquadra = squadra?.Nome ?? "N/A";

            return Page();
        }

        public async Task<IActionResult> OnPostInviaNotificheAsync(List<int> AtletiSelezionati, string MessaggioTesto)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            if (AtletiSelezionati == null || !AtletiSelezionati.Any() || string.IsNullOrWhiteSpace(MessaggioTesto))
            {
                TempData["Errore"] = "Devi selezionare almeno un atleta e scrivere un messaggio.";
                return RedirectToPage();
            }

            var subject = _configuration["VapidKeys:Subject"];
            var publicKey = _configuration["VapidKeys:PublicKey"];
            var privateKey = _configuration["VapidKeys:PrivateKey"];

            if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            {
                TempData["Errore"] = "Chiavi VAPID non configurate sul server.";
                return RedirectToPage();
            }

            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            var webPushClient = new WebPushClient();

            // Filtra solo gli atleti che appartengono effettivamente alla squadra dello staff loggato
            var atletiValidiIds = await _context.Atleti
                .Where(a => a.SquadraId == squadraId.Value && AtletiSelezionati.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync();

            var dispositivi = await _context.SottoscrizioniPush
                .Where(s => atletiValidiIds.Contains(s.AtletaId))
                .ToListAsync();

            int notificheInviateConSuccesso = 0;

            foreach (var dispositivo in dispositivi)
            {
                try
                {
                    var pushSubscription = new PushSubscription(dispositivo.Endpoint, dispositivo.P256dh, dispositivo.Auth);
                    await webPushClient.SendNotificationAsync(pushSubscription, MessaggioTesto.Trim(), vapidDetails);
                    notificheInviateConSuccesso++;
                }
                catch (WebPushException exception)
                {
                    Console.WriteLine($"Errore Push (StatusCode: {exception.StatusCode}): {exception.Message}");

                    if (exception.StatusCode == System.Net.HttpStatusCode.Gone ||
                        exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _context.SottoscrizioniPush.Remove(dispositivo);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore generico invio Push: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            TempData["MessaggioSuccesso"] = $"Inviate {notificheInviateConSuccesso} notifiche con successo!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveAsync(
            int Id, string Nome, string Cognome, int Altezza, double Peso, string? Password,
            bool IsAttivo, bool IsInfortunato, bool IsInRiabilitazione, string? DescrizioneInfortunio,
            IFormFile? UploadImmagine, string? FotoUrlCorrente)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Cognome))
            {
                TempData["Errore"] = "Nome e cognome sono obbligatori.";
                return RedirectToPage();
            }

            Atleta atleta;

            if (Id > 0)
            {
                // Verifica che l'atleta esista E appartenga alla squadra corrente
                var esistente = await _context.Atleti
                    .FirstOrDefaultAsync(a => a.Id == Id && a.SquadraId == squadraId.Value);

                if (esistente == null)
                    return Forbid();

                atleta = esistente;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    TempData["Errore"] = "La password è obbligatoria per un nuovo atleta.";
                    return RedirectToPage();
                }

                atleta = new Atleta
                {
                    SquadraId = squadraId.Value
                };
                _context.Atleti.Add(atleta);
            }

            atleta.Nome = Nome.Trim();
            atleta.Cognome = Cognome.Trim();
            atleta.Altezza = Altezza;
            atleta.Peso = Peso;
            atleta.IsAttivo = IsAttivo;
            atleta.IsInfortunato = IsInfortunato;
            atleta.IsInRiabilitazione = IsInRiabilitazione;
            atleta.DescrizioneInfortunio = DescrizioneInfortunio?.Trim();

            if (!string.IsNullOrWhiteSpace(Password) && !Password.StartsWith("$2"))
            {
                atleta.Password = BCrypt.Net.BCrypt.HashPassword(Password);
            }

            // Gestione sicura caricamento immagine
            if (UploadImmagine != null && UploadImmagine.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(UploadImmagine.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension) || UploadImmagine.Length > 5 * 1024 * 1024)
                {
                    TempData["Errore"] = "Formato immagine non valido (usa JPG, PNG o WebP, max 5MB).";
                    return RedirectToPage();
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Img");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadImmagine.CopyToAsync(fileStream);
                }

                atleta.FotoUrl = "/Img/" + uniqueFileName;
            }
            else if (Id == 0)
            {
                atleta.FotoUrl = "/Img/default.png";
            }
            else
            {
                atleta.FotoUrl = string.IsNullOrWhiteSpace(FotoUrlCorrente) ? "/Img/default.png" : FotoUrlCorrente;
            }

            await _context.SaveChangesAsync();
            TempData["MessaggioSuccesso"] = "Dati atleta salvati correttamente.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAggiornaSquadraAsync(string NomeSquadra)
        {
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
                return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
                return RedirectToPage("/Login");

            var squadra = await _context.Squadre.FindAsync(squadraId.Value);

            if (squadra != null && !string.IsNullOrWhiteSpace(NomeSquadra))
            {
                var nomePulito = NomeSquadra.Trim();
                squadra.Nome = nomePulito;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("NomeSquadra", nomePulito);
                TempData["MessaggioSuccesso"] = "Nome squadra aggiornato.";
            }

            return RedirectToPage();
        }
    }
}