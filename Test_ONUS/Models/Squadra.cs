using System.ComponentModel.DataAnnotations;

namespace Test_ONUS.Models
{
    public class Squadra
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Nome { get; set; }

        // Relazione: Una squadra ha un preparatore (o più)
        public required ICollection<PreparatoreAtletico> Preparatori { get; set; }

        // Relazione: Una squadra ha molti atleti
        public required ICollection<Atleta> Atleti { get; set; }
    }
}