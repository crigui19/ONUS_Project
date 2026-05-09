using System.ComponentModel.DataAnnotations;

namespace Test_ONUS.Models
{
    public class Parametro
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty; // Es. "RPE", "Sonno", "Idratazione"

        public int ValoreMinimo { get; set; } = 0;
        public int ValoreMassimo { get; set; } = 10;

        public bool IsAttivo { get; set; } = true; // Se false, non compare più nel form ma non perdiamo i dati vecchi

        // Definisce se questo parametro serve per il calcolo del carico (RPE)
        public bool IsCalcoloCarico { get; set; } = false;
        // NUOVO CAMPO: Se Null = Globale (RPE, Sonno, ecc.), Se valorizzato = Custom della squadra
        public int? SquadraId { get; set; }
    }
}