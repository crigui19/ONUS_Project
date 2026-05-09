using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Test_ONUS.Models
{
    public class Atleta
    {
        [Key]
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cognome { get; set; } = string.Empty;

        // Queste erano quelle che davano errore:
        public string FotoUrl { get; set; } = "/Img/default.png";
        public bool IsAttivo { get; set; } = true;

        public string Password { get; set; } = "1234";
        public double Peso { get; set; }  // In kg
        public int Altezza { get; set; }   // In cm

        // Nuovi campi per la gestione infortuni
        public bool IsInfortunato { get; set; } = false; // Tasto Rosso
        public bool IsInRiabilitazione { get; set; } = false; // Tasto Giallo
        public string? DescrizioneInfortunio { get; set; } // Testo per il pop-up

        // Relazione con la Squadra (necessaria per la gestione rosa)
        public int? SquadraId { get; set; }
        [ForeignKey("SquadraId")]
        public Squadra? Squadra { get; set; }


        [NotMapped]
        public double BMI
        {
            get
            {
                if (Altezza <= 0) return 0;
                double altezzaMetri = Altezza / 100.0;
                return Math.Round(Peso / (altezzaMetri * altezzaMetri), 1);
            }
        }
    }
}