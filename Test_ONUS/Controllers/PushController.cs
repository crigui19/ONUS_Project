using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;

namespace Test_ONUS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PushController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionViewModel sub)
        {
            // Verifichiamo che sia un Atleta loggato a mandare la richiesta
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");

            if (userId == null || ruolo != "Atleta")
            {
                return Unauthorized("Solo gli atleti possono registrarsi alle notifiche.");
            }

            // Controlliamo se questo telefono è già registrato nel database
            var existing = await _context.SottoscrizioniPush.FirstOrDefaultAsync(s => s.Endpoint == sub.Endpoint);

            if (existing == null)
            {
                // È un telefono nuovo! Lo salviamo.
                var nuovaSottoscrizione = new SottoscrizionePush
                {
                    AtletaId = userId.Value,
                    Endpoint = sub.Endpoint,
                    P256dh = sub.Keys.P256dh,
                    Auth = sub.Keys.Auth
                };

                _context.SottoscrizioniPush.Add(nuovaSottoscrizione);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }

    // Classi di supporto per leggere i dati inviati dal Javascript del telefono
    public class PushSubscriptionViewModel
    {
        public string Endpoint { get; set; }
        public KeysViewModel Keys { get; set; }
    }

    public class KeysViewModel
    {
        public string P256dh { get; set; }
        public string Auth { get; set; }
    }
}