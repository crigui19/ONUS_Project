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

// Se siamo su Render, prende la stringa dalle variabili di ambiente
var envDb = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(envDb))
{
    connectionString = envDb;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)); // <-- Cambiato da UseSqlite a UseNpgsq

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

app.MapControllers(); // MAPPA LE TUE API C#
app.MapRazorPages();  // MAPPA LE TUE PAGINE CLASSICHE

app.Run();