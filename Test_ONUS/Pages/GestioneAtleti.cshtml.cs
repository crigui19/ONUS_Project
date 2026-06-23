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
    }
}