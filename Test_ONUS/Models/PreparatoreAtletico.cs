using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test_ONUS.Models
{
    public class PreparatoreAtletico
    {
        [Key]
        public int Id { get; set; }

        public required string Nome { get; set; }
        public required string Cognome { get; set; }
        public required string Password { get; set; }

        // Foreign Key per la Squadra
        public int SquadraId { get; set; }

        [ForeignKey("SquadraId")]
        public required Squadra Squadra { get; set; }
    }
}