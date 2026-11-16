using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test_ONUS.Models
{
    public class PreparatoreAtletico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Cognome { get; set; } = string.Empty;

        public string? Password { get; set; }
        public string? GoogleId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenScadenza { get; set; }

        public int SquadraId { get; set; }

        [ForeignKey("SquadraId")]
        public Squadra Squadra { get; set; } = null!;
    }
}