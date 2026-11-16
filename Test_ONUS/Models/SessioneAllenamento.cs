using System.ComponentModel.DataAnnotations.Schema;

namespace Test_ONUS.Models
{
    public class SessioneAllenamento
    {
        public int Id { get; set; }

        public int AtletaId { get; set; }
        public Atleta Atleta { get; set; } = null!;

        public DateTime Data { get; set; } = DateTime.UtcNow;

        public int DurataTotaleMinuti { get; set; }
        public int TempoEffettivoMinuti { get; set; }
        public string? Note { get; set; }

        public List<ValoreSessione> Valori { get; set; } = new();

        [NotMapped]
        public int CaricoCalcolato
        {
            get
            {
                var valoreRpe = Valori?
                    .FirstOrDefault(v => v.Parametro != null && v.Parametro.IsCalcoloCarico)?.Valore ?? 0;
                return valoreRpe * DurataTotaleMinuti;
            }
        }
    }
}