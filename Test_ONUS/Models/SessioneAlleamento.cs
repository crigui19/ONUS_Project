using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Test_ONUS.Models
{
    public class SessioneAllenamento
    {
        public int Id { get; set; }
        public int AtletaId { get; set; }

        public DateTime Data { get; set; } = DateTime.Now;
        public int DurataTotaleMinuti { get; set; } // Durata complessiva (es. 90 min)
        public int TempoEffettivoMinuti { get; set; } // Tempo reale di lavoro (es. 70 min)
        public string? Note { get; set; }

        // RELAZIONE: Contiene TUTTI i parametri (incluso l'RPE)
        public List<ValoreSessione> Valori { get; set; } = new();

        // Calcola il carico cercando nella lista Valori il parametro designato
        [NotMapped]
        public int CaricoCalcolato
        {
            get
            {
                // Cerca il valore dell'RPE (quello che ha IsCalcoloCarico = true)
                // Nota: richiede che la query usi .Include(s => s.Valori).ThenInclude(v => v.Parametro)
                var valoreRpe = Valori.FirstOrDefault(v => v.Parametro != null && v.Parametro.IsCalcoloCarico)?.Valore ?? 0;
                return valoreRpe * DurataTotaleMinuti;
            }
        }
    }
}