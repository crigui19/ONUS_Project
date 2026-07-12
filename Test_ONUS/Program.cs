using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAZIONI PER NEXT.JS (API & CORS)
// ==========================================
builder.Services.AddControllers(); // Abilita le API

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",          // Per lo sviluppo locale
                "https://tuo-sito-nextjs.com"     // INSERISCI QUI IL DOMINIO FUTURO DEL TUO FRONTEND!
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// ==========================================
// 2. CONFIGURAZIONE DATABASE
// ==========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                  ?? Environment.GetEnvironmentVariable("INTERNAL_DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    databaseUrl = databaseUrl.Replace("postgresql://", "postgres://");

    if (databaseUrl.StartsWith("postgres://"))
    {
        var databaseUri = new Uri(databaseUrl);
        var userInfo = databaseUri.UserInfo.Split(':');

        // TRUCCHETTO PORTA: Se Render non scrive la porta nel link, usiamo la 5432 di default
        var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;

        // Ho modificato SSL Mode in "Prefer" che è lo standard per i database interni di Render
        connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Prefer;Trust Server Certificate=True;";
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
   options.UseNpgsql(connectionString));

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//   options.UseNpgsql(connectionString)); // <-- Cambiato da UseSqlite a UseNpgsq

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//// Aggiungi questo: se il server è Render, dirotta il DB nella cartella persistente
//if (Environment.GetEnvironmentVariable("RENDER") == "true")
//{
//    connectionString = "Data Source=/data/OnusDb.db";
//}

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlite(connectionString));

// ==========================================
// 3. SERVIZI PER SESSIONE E PAGINE (RAZOR)
// ==========================================
builder.Services.AddRazorPages().AddViewLocalization();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==========================================
// 4. CONFIGURAZIONE LOCALIZZAZIONE
// ==========================================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "it", "en" };
    options.SetDefaultCulture("it")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});


var app = builder.Build();
// ==========================================
// AUTO-MIGRAZIONE DATABASE ALL'AVVIO
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Applica automaticamente le migrazioni (crea le tabelle su Neon/Postgres)
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore durante la migrazione del database: {ex.Message}");
    }
}
// ==========================================
// 5. CONFIGURAZIONE PIPELINE HTTP
// ==========================================
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

// ⚠️ RICORDA: Lo teniamo commentato finché sviluppi in locale con Next.js!
// Altrimenti avrai l'errore "fetch failed"
// app.UseHttpsRedirection();

app.UseRequestLocalization(); // Corretto: inserito prima di UseStaticFiles e UseRouting
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowNextJs"); // ATTIVA IL CORS PER NEXT.JS

app.UseAuthorization();
app.UseSession();
app.Use(async (context, next) =>
{
    // Se la sessione è vuota (il server si è riavviato) ma il cookie "Remember Me" esiste
    if (context.Session.GetInt32("UserId") == null && context.Request.Cookies.ContainsKey("OnusAuth"))
    {
        try
        {
            var provider = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
            var protector = provider.CreateProtector("Onus.Auth.v1");

            // Decripta l'ID utente in modo sicuro
            var decryptedUserId = protector.Unprotect(context.Request.Cookies["OnusAuth"]!);

            if (int.TryParse(decryptedUserId, out int userId))
            {
                var db = context.RequestServices.GetRequiredService<Test_ONUS.Data.ApplicationDbContext>();

                // Cerca prima tra gli atleti
                var atleta = db.Atleti.Find(userId);
                if (atleta != null)
                {
                    context.Session.SetInt32("UserId", atleta.Id);
                    context.Session.SetString("Ruolo", "Atleta");
                    context.Session.SetString("Nome", atleta.Nome);
                    context.Session.SetString("Cognome", atleta.Cognome);
                    context.Session.SetString("NomeCompleto", $"{atleta.Nome} {atleta.Cognome}");
                    context.Session.SetInt32("SquadraId", (int)atleta.SquadraId);
                }
                else
                {
                    // Se non è un atleta, cerca tra lo staff
                    var staff = db.PreparatoriAtletici.Find(userId);
                    if (staff != null)
                    {
                        context.Session.SetInt32("UserId", staff.Id);
                        context.Session.SetString("Ruolo", "Staff");
                        context.Session.SetString("Nome", staff.Nome);
                        context.Session.SetString("Cognome", staff.Cognome);
                        context.Session.SetString("NomeCompleto", $"{staff.Nome} {staff.Cognome}");
                        context.Session.SetInt32("SquadraId", staff.SquadraId);
                    }
                }
            }
        }
        catch
        {
            // Se il cookie è scaduto o manomesso, ignoriamo (verrà reindirizzato al login normale)
        }
    }
    await next();
});

app.UseRouting();

app.MapControllers(); // MAPPA LE TUE API C#
app.MapRazorPages();  // MAPPA LE TUE PAGINE CLASSICHE

app.Run();