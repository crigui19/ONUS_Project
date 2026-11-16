using System.ComponentModel.DataAnnotations;

namespace Test_ONUS.Models
{
    public class Atleta
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Cognome { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public int SquadraId { get; set; }
        public Squadra? Squadra { get; set; }

        public int Altezza { get; set; }
        public double Peso { get; set; }

        public bool IsAttivo { get; set; } = true;
        public bool IsInfortunato { get; set; }
        public bool IsInRiabilitazione { get; set; }
        public string? DescrizioneInfortunio { get; set; }

        public string FotoUrl { get; set; } = "/Img/default.png";

        public ICollection<SessioneAllenamento> Sessioni { get; set; } = new List<SessioneAllenamento>();
        public ICollection<SottoscrizionePush> SottoscrizioniPush { get; set; } = new List<SottoscrizionePush>();
    }
}