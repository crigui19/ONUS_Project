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
        public DbSet<SessioneAllenamento> Sessioni { get; set; }
        public DbSet<Parametro> Parametri { get; set; }
        public DbSet<ValoreSessione> ValoriSessione { get; set; }
        public DbSet<Squadra> Squadre { get; set; }
        public DbSet<PreparatoreAtletico> PreparatoriAtletici { get; set; }
        public DbSet<SottoscrizionePush> SottoscrizioniPush { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indice composito per velocizzare Dashboard, Analisi e Report per atleta e intervallo date
            modelBuilder.Entity<SessioneAllenamento>()
                .HasIndex(s => new { s.AtletaId, s.Data });

            // Evita parametri duplicati all'interno della stessa sessione
            modelBuilder.Entity<ValoreSessione>()
                .HasIndex(v => new { v.SessioneId, v.ParametroId })
                .IsUnique();

            // Email univoca per i preparatori atletici
            modelBuilder.Entity<PreparatoreAtletico>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Evita sottoscrizioni push duplicate sullo stesso endpoint per lo stesso atleta
            modelBuilder.Entity<SottoscrizionePush>()
                .HasIndex(sp => new { sp.AtletaId, sp.Endpoint })
                .IsUnique();

            // Seeding parametri base
            modelBuilder.Entity<Parametro>().HasData(
                new Parametro { Id = 1, Nome = "RPE", ValoreMinimo = 0, ValoreMassimo = 10, IsCalcoloCarico = true, IsAttivo = true },
                new Parametro { Id = 2, Nome = "Sonno", ValoreMinimo = 0, ValoreMassimo = 10, IsCalcoloCarico = false, IsAttivo = true },
                new Parametro { Id = 3, Nome = "Dolore", ValoreMinimo = 0, ValoreMassimo = 10, IsCalcoloCarico = false, IsAttivo = true }
            );
        }
    }
}