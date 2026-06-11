using Microsoft.EntityFrameworkCore;
using Test_ONUS.Models;

namespace Test_ONUS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Atleta> Atleti { get; set; }

        // --- QUESTA RIGA È QUELLA CHE MANCA O È DIVERSA ---
        public DbSet<SessioneAllenamento> Sessioni { get; set; }

        // Tabelle di supporto
        public DbSet<Parametro> Parametri { get; set; }
        public DbSet<ValoreSessione> ValoriSessione { get; set; }
        public DbSet<Squadra> Squadre { get; set; }
        public DbSet<PreparatoreAtletico> PreparatoriAtletici { get; set; }
        public DbSet<SottoscrizionePush> SottoscrizioniPush { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SEEDING: Creiamo i parametri base
            modelBuilder.Entity<Parametro>().HasData(
                new Parametro { Id = 1, Nome = "RPE", IsCalcoloCarico = true, IsAttivo = true },
                new Parametro { Id = 2, Nome = "Sonno", IsCalcoloCarico = false, IsAttivo = true },
                new Parametro { Id = 3, Nome = "Dolore", IsCalcoloCarico = false, IsAttivo = true }
            );
        }
    }
}