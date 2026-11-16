using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
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
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");

            if (userId == null || ruolo != "Atleta")
            {
                return Unauthorized("Solo gli atleti possono registrarsi alle notifiche.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Rimuove eventuali associazioni precedenti dello stesso endpoint su altri account
            // (es. dispositivo condiviso o cambio utente sullo stesso telefono)
            var altreSottoscrizioni = await _context.SottoscrizioniPush
                .Where(s => s.Endpoint == sub.Endpoint && s.AtletaId != userId.Value)
                .ToListAsync();

            if (altreSottoscrizioni.Any())
            {
                _context.SottoscrizioniPush.RemoveRange(altreSottoscrizioni);
            }

            var existing = await _context.SottoscrizioniPush
                .FirstOrDefaultAsync(s => s.AtletaId == userId.Value && s.Endpoint == sub.Endpoint);

            if (existing == null)
            {
                var nuovaSottoscrizione = new SottoscrizionePush
                {
                    AtletaId = userId.Value,
                    Endpoint = sub.Endpoint,
                    P256dh = sub.Keys.P256dh,
                    Auth = sub.Keys.Auth
                };

                _context.SottoscrizioniPush.Add(nuovaSottoscrizione);
            }
            else
            {
                // Aggiorna eventuali chiavi rinnovate dal browser
                existing.P256dh = sub.Keys.P256dh;
                existing.Auth = sub.Keys.Auth;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(model.Endpoint))
                return BadRequest();

            var sub = await _context.SottoscrizioniPush
                .FirstOrDefaultAsync(s => s.AtletaId == userId.Value && s.Endpoint == model.Endpoint);

            if (sub != null)
            {
                _context.SottoscrizioniPush.Remove(sub);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }

    public class PushSubscriptionViewModel
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public KeysViewModel Keys { get; set; } = new();
    }

    public class KeysViewModel
    {
        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;
    }

    public class UnsubscribeViewModel
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;
    }
}