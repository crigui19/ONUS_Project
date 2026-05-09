using System.ComponentModel.DataAnnotations.Schema;

namespace Test_ONUS.Models
{
    public class ValoreSessione
    {
        public int Id { get; set; }

        // Collegamento alla Sessione (Padre)
        public int SessioneId { get; set; }
        [ForeignKey("SessioneId")]
        public SessioneAllenamento? Sessione { get; set; }

        // Collegamento al Parametro (Definizione)
        public int ParametroId { get; set; }
        [ForeignKey("ParametroId")]
        public Parametro? Parametro { get; set; }

        // Il voto effettivo dato dall'atleta
        public int Valore { get; set; }
    }
}