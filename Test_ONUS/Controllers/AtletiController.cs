using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;

namespace Test_ONUS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtletiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AtletiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/atleti/squadra/5
        [HttpGet("squadra/{squadraId}")]
        public async Task<IActionResult> GetAtletiDiSquadra(int squadraId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var userSquadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return Unauthorized();

            // Solo lo Staff della stessa squadra può scaricare l'intera rosa
            if (ruolo != "Staff" || userSquadraId != squadraId)
                return Forbid();

            var atleti = await _context.Atleti
                .AsNoTracking()
                .Where(a => a.SquadraId == squadraId)
                .OrderBy(a => a.Cognome)
                .ThenBy(a => a.Nome)
                .Select(a => new AtletaDto
                {
                    Id = a.Id,
                    Nome = a.Nome,
                    Cognome = a.Cognome,
                    SquadraId = a.SquadraId,
                    Altezza = a.Altezza,
                    Peso = a.Peso,
                    IsAttivo = a.IsAttivo,
                    IsInfortunato = a.IsInfortunato,
                    IsInRiabilitazione = a.IsInRiabilitazione,
                    DescrizioneInfortunio = a.DescrizioneInfortunio,
                    FotoUrl = a.FotoUrl
                })
                .ToListAsync();

            return Ok(atleti);
        }

        // GET: api/atleti/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAtleta(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var userSquadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return Unauthorized();

            var atleta = await _context.Atleti
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (atleta == null)
                return NotFound();

            // Autorizzazione: l'atleta stesso oppure uno Staff della sua stessa squadra
            bool autorizzato = (ruolo == "Atleta" && userId == atleta.Id)
                            || (ruolo == "Staff" && userSquadraId.HasValue && atleta.SquadraId == userSquadraId.Value);

            if (!autorizzato)
                return Forbid();

            return Ok(new AtletaDto
            {
                Id = atleta.Id,
                Nome = atleta.Nome,
                Cognome = atleta.Cognome,
                SquadraId = atleta.SquadraId,
                Altezza = atleta.Altezza,
                Peso = atleta.Peso,
                IsAttivo = atleta.IsAttivo,
                IsInfortunato = atleta.IsInfortunato,
                IsInRiabilitazione = atleta.IsInRiabilitazione,
                DescrizioneInfortunio = atleta.DescrizioneInfortunio,
                FotoUrl = atleta.FotoUrl
            });
        }

        // GET: api/atleti/5/sessioni
        [HttpGet("{id}/sessioni")]
        public async Task<IActionResult> GetSessioniAtleta(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var ruolo = HttpContext.Session.GetString("Ruolo");
            var userSquadraId = HttpContext.Session.GetInt32("SquadraId");

            if (userId == null)
                return Unauthorized();

            var atleta = await _context.Atleti
                .AsNoTracking()
                .Select(a => new { a.Id, a.SquadraId })
                .FirstOrDefaultAsync(a => a.Id == id);

            if (atleta == null)
                return NotFound();

            bool autorizzato = (ruolo == "Atleta" && userId == atleta.Id)
                            || (ruolo == "Staff" && userSquadraId.HasValue && atleta.SquadraId == userSquadraId.Value);

            if (!autorizzato)
                return Forbid();

            var sessioni = await _context.Sessioni
                .AsNoTracking()
                .Where(s => s.AtletaId == id)
                .Include(s => s.Valori)
                    .ThenInclude(v => v.Parametro)
                .OrderByDescending(s => s.Data)
                .Select(s => new SessioneDto
                {
                    Id = s.Id,
                    AtletaId = s.AtletaId,
                    Data = s.Data,
                    DurataTotaleMinuti = s.DurataTotaleMinuti,
                    TempoEffettivoMinuti = s.TempoEffettivoMinuti,
                    Note = s.Note,
                    CaricoCalcolato = (s.Valori
                        .Where(v => v.Parametro != null && v.Parametro.IsCalcoloCarico)
                        .Select(v => (int?)v.Valore)
                        .FirstOrDefault() ?? 0) * s.DurataTotaleMinuti,
                    Valori = s.Valori.Select(v => new ValoreParametroDto
                    {
                        ParametroId = v.ParametroId,
                        NomeParametro = v.Parametro != null ? v.Parametro.Nome : string.Empty,
                        Valore = v.Valore
                    }).ToList()
                })
                .ToListAsync();

            return Ok(sessioni);
        }

        // DTO sicuri (escludono Password e riferimenti circolari)
        public class AtletaDto
        {
            public int Id { get; set; }
            public string Nome { get; set; } = string.Empty;
            public string Cognome { get; set; } = string.Empty;
            public int? SquadraId { get; set; }
            public int Altezza { get; set; }
            public double Peso { get; set; }
            public bool IsAttivo { get; set; }
            public bool IsInfortunato { get; set; }
            public bool IsInRiabilitazione { get; set; }
            public string? DescrizioneInfortunio { get; set; }
            public string FotoUrl { get; set; } = string.Empty;
        }

        public class SessioneDto
        {
            public int Id { get; set; }
            public int AtletaId { get; set; }
            public DateTime Data { get; set; }
            public int DurataTotaleMinuti { get; set; }
            public int TempoEffettivoMinuti { get; set; }
            public string? Note { get; set; }
            public int CaricoCalcolato { get; set; }
            public List<ValoreParametroDto> Valori { get; set; } = new();
        }

        public class ValoreParametroDto
        {
            public int ParametroId { get; set; }
            public string NomeParametro { get; set; } = string.Empty;
            public int Valore { get; set; }
        }
    }
}