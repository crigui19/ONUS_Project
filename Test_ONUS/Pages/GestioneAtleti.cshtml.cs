using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration; // Necessario per leggere appsettings.json
using WebPush; // La libreria che abbiamo appena installato!
using System;

namespace Test_ONUS.Pages
{
    public class GestioneAtletiModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // Aggiungiamo IConfiguration per leggere le chiavi

        public GestioneAtletiModel(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IList<Atleta> Atleti { get; set; } = default!;
        public string NomeSquadra { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            // Controllo permessi
            if (HttpContext.Session.GetString("Ruolo") != "Staff")
            {
                return RedirectToPage("/Index");
            }

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null)
            {
                return RedirectToPage("/Login");
            }

            // Recupera gli atleti
            Atleti = await _context.Atleti
                .Where(a => a.SquadraId == squadraId)
                .ToListAsync();

            // Recupera il nome della squadra
            var squadra = await _context.Squadre.FindAsync(squadraId);
            NomeSquadra = squadra?.Nome ?? "N/A";

            return Page();
        }

        public async Task<IActionResult> OnPostInviaNotificheAsync(List<int> AtletiSelezionati, string MessaggioTesto)
        {
            // Sicurezza: solo lo staff può mandare notifiche
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");

            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null) return RedirectToPage("/Login");

            // Controllo base: hai selezionato qualcuno e hai scritto un messaggio?
            if (AtletiSelezionati == null || !AtletiSelezionati.Any() || string.IsNullOrWhiteSpace(MessaggioTesto))
            {
                TempData["Errore"] = "Devi selezionare almeno un atleta e scrivere un messaggio.";
                return RedirectToPage();
            }

            // 1. Leggiamo le VAPID Keys dal tuo file appsettings.json
            var subject = _configuration["VapidKeys:Subject"];
            var publicKey = _configuration["VapidKeys:PublicKey"];
            var privateKey = _configuration["VapidKeys:PrivateKey"];

            // Configuriamo il "postino" (WebPush) con la tua identità
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            var webPushClient = new WebPushClient();

            int notificheInviateConSuccesso = 0;

            // 2. Passiamo in rassegna tutti gli atleti che hai spuntato nella pagina
            foreach (var atletaId in AtletiSelezionati)
            {
                // Cerchiamo tutti i dispositivi (es. telefono e PC) registrati per questo atleta
                var dispositivi = await _context.SottoscrizioniPush
                    .Where(s => s.AtletaId == atletaId)
                    .ToListAsync();

                // 3. Inviamo la notifica a ogni dispositivo trovato
                foreach (var dispositivo in dispositivi)
                {
                    try
                    {
                        // Ricostruiamo l'indirizzo esatto del dispositivo
                        var pushSubscription = new PushSubscription(dispositivo.Endpoint, dispositivo.P256dh, dispositivo.Auth);

                        // INVIA LA NOTIFICA!
                        await webPushClient.SendNotificationAsync(pushSubscription, MessaggioTesto, vapidDetails);

                        notificheInviateConSuccesso++;
                    }
                    catch (WebPushException exception)
                    {
                        // Se c'è un errore (es. l'utente ha revocato i permessi dal telefono)
                        Console.WriteLine($"Errore Push (StatusCode: {exception.StatusCode}): {exception.Message}");

                        // Opzionale (ma consigliato): se l'errore è "Gone" (410) o "Not Found" (404), 
                        // significa che l'iscrizione non è più valida. Possiamo cancellarla dal database.
                        if (exception.StatusCode == System.Net.HttpStatusCode.Gone || exception.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _context.SottoscrizioniPush.Remove(dispositivo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Errore generico invio Push: {ex.Message}");
                    }
                }
            }

            // Salviamo le eventuali cancellazioni di dispositivi non più validi
            await _context.SaveChangesAsync();

            // Mostriamo un messaggio di conferma sulla pagina web
            TempData["MessaggioSuccesso"] = $"Inviate {notificheInviateConSuccesso} notifiche con successo!";

            return RedirectToPage();
        }

        // ==============================================================
        // METODO PER SALVARE O AGGIORNARE UN ATLETA
        // ==============================================================
        public async Task<IActionResult> OnPostSaveAsync(
            int Id, string Nome, string Cognome, int Altezza, double Peso, string Password,
            bool IsAttivo, bool IsInfortunato, bool IsInRiabilitazione, string DescrizioneInfortunio,
            IFormFile UploadImmagine, string FotoUrlCorrente)
        {
            // Controllo permessi
            if (HttpContext.Session.GetString("Ruolo") != "Staff") return RedirectToPage("/Index");
            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null) return RedirectToPage("/Login");

            Atleta atleta;

            // 1. Cerca l'atleta esistente o creane uno nuovo
            if (Id > 0)
            {
                atleta = await _context.Atleti.FindAsync(Id);
                if (atleta == null) return NotFound();
            }
            else
            {
                atleta = new Atleta();
                atleta.SquadraId = squadraId.Value;
                _context.Atleti.Add(atleta);
            }

            // 2. Aggiorna tutti i campi base
            atleta.Nome = Nome;
            atleta.Cognome = Cognome;
            atleta.Altezza = Altezza;
            atleta.Peso = Peso;
            atleta.IsAttivo = IsAttivo;
            atleta.IsInfortunato = IsInfortunato;
            atleta.IsInRiabilitazione = IsInRiabilitazione;
            atleta.DescrizioneInfortunio = DescrizioneInfortunio;

            // 3. Cripta la password (solo se è nuova e non è già criptata con BCrypt)
            if (!string.IsNullOrEmpty(Password) && !Password.StartsWith("$2"))
            {
                atleta.Password = BCrypt.Net.BCrypt.HashPassword(Password);
            }

            // 4. Gestione Immagine Profilo (Caricamento)
            if (UploadImmagine != null && UploadImmagine.Length > 0)
            {
                var uploadsFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "Img");
                System.IO.Directory.CreateDirectory(uploadsFolder); // Crea la cartella se non esiste

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + UploadImmagine.FileName;
                var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await UploadImmagine.CopyToAsync(fileStream);
                }
                atleta.FotoUrl = "/Img/" + uniqueFileName;
            }
            else if (Id == 0)
            {
                // Se è un atleta nuovo e non carica foto, usa quella di default
                atleta.FotoUrl = "/Img/default.png";
            }
            else
            {
                // Se è una modifica e non cambia foto, mantieni la precedente
                atleta.FotoUrl = FotoUrlCorrente ?? "/Img/default.png";
            }

            // 5. Salva tutto nel database
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // ==============================================================
        // METODO PER AGGIORNARE IL NOME DELLA SQUADRA
        // ==============================================================
        public async Task<IActionResult> OnPostAggiornaSquadraAsync(string NomeSquadra)
        {
            var squadraId = HttpContext.Session.GetInt32("SquadraId");
            if (squadraId == null) return RedirectToPage("/Login");

            var squadra = await _context.Squadre.FindAsync(squadraId);

            if (squadra != null && !string.IsNullOrWhiteSpace(NomeSquadra))
            {
                squadra.Nome = NomeSquadra;
                await _context.SaveChangesAsync();

                // Aggiorna anche il nome in sessione per navbar/dashboard
                HttpContext.Session.SetString("NomeSquadra", NomeSquadra);
            }
            return RedirectToPage();
        }
    }
}