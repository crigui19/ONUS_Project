using System.ComponentModel.DataAnnotations;

namespace Test_ONUS.Models
{
    public class SottoscrizionePush
    {
        [Key]
        public int Id { get; set; }

        public int AtletaId { get; set; }
        public Atleta Atleta { get; set; } = null!;

        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public string P256dh { get; set; } = string.Empty;

        [Required]
        public string Auth { get; set; } = string.Empty;
    }
}