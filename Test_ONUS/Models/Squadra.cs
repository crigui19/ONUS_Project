using System.ComponentModel.DataAnnotations;

namespace Test_ONUS.Models
{
    public class Squadra
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        public ICollection<Atleta> Atleti { get; set; } = new List<Atleta>();
        public ICollection<PreparatoreAtletico> Preparatori { get; set; } = new List<PreparatoreAtletico>();
    }
}