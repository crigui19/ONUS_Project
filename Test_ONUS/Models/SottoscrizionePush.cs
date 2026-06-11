using System.ComponentModel.DataAnnotations;

namespace Test_ONUS.Models
{
    public class SottoscrizionePush
    {
        [Key]
        public int Id { get; set; }

        // Colleghiamo il dispositivo all'atleta
        public int AtletaId { get; set; }
        public Atleta Atleta { get; set; }

        // I 3 dati fondamentali che ci darà il browser per rintracciare il telefono
        [Required]
        public string Endpoint { get; set; }
        [Required]
        public string P256dh { get; set; }
        [Required]
        public string Auth { get; set; }
    }
}