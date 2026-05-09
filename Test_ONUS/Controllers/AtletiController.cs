using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Test_ONUS.Models;
 // Cambia "TuoProgetto" col nome reale (forse Test_ONUS.Models)

namespace TuoProgetto.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtletiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Chiediamo a .NET di passarci il database (Dependency Injection)
        public AtletiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Questo metodo risponderà quando Next.js farà una richiesta GET a /api/atleti
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Atleta>>> GetAtleti()
        {
            // Peschiamo tutti gli atleti dal tuo database SQL Server e li restituiamo
            return await _context.Atleti.ToListAsync();
        }

        // Questo metodo risponderà a richieste come: /api/atleti/1 (per LeBron)
        [HttpGet("{id}")]
        public async Task<ActionResult<Atleta>> GetAtleta(int id)
        {
            // Cerca l'atleta nel database tramite l'ID
            var atleta = await _context.Atleti.FindAsync(id);

            if (atleta == null)
            {
                return NotFound(); // Restituisce un errore 404 se non esiste
            }

            return atleta;
        }

        // GET: api/atleti/5/sessioni
        [HttpGet("{id}/sessioni")]
        public async Task<ActionResult<IEnumerable<SessioneAllenamento>>> GetSessioniAtleta(int id)
        {
            // Cerca tutte le sessioni che appartengono a questo AtletaId
            // Le ordiniamo per Data decrescente (dalla più recente alla più vecchia)
            var sessioni = await _context.Sessioni
                .Where(s => s.AtletaId == id)
                .OrderByDescending(s => s.Data)
                .ToListAsync();

            if (sessioni == null)
            {
                return NotFound();
            }

            return sessioni;
        }
    }
}