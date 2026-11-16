using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Test_ONUS.Data;
using Microsoft.AspNetCore.DataProtection;


var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAZIONE API & CORS (NEXT.JS)
// ==========================================
builder.Services.AddControllers();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ==========================================
// 2. CONFIGURAZIONE DATABASE (POSTGRESQL)
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
        var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;

        connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Prefer;Trust Server Certificate=True;";
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==========================================
// 3. RAZOR PAGES, SESSIONE & LOCALIZZAZIONE
// ==========================================
builder.Services.AddRazorPages().AddViewLocalization();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "it", "en" };
    options.SetDefaultCulture("it")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

// ==========================================
// 4. AUTENTICAZIONE (COOKIE & GOOGLE OAUTH)
// ==========================================
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Login";
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });
}

// Configurazione header proxy per Render (HTTPS)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
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
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Errore durante la migrazione del database: {ex.Message}");
    }
}

// ==========================================
// 5. PIPELINE HTTP (ORDINE CORRETTO)
// ==========================================
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseStaticFiles();



var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedOptions);

app.UseRouting();

app.UseCors("AllowNextJs");

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Ripristino sicuro della sessione tramite cookie crittografato "Ruolo:Id"
app.Use(async (context, next) =>
{
    if (context.Session.GetInt32("UserId") == null && context.Request.Cookies.TryGetValue("OnusAuth", out var authCookie))
    {
        try
        {
            var provider = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
            var protector = provider.CreateProtector("Onus.Auth.v1");
            var payload = protector.Unprotect(authCookie); // Formato atteso: "Atleta:12" oppure "Staff:3"

            var parts = payload.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int userId))
            {
                var ruolo = parts[0];
                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                if (ruolo == "Atleta")
                {
                    var atleta = db.Atleti.Find(userId);
                    if (atleta != null)
                    {
                        context.Session.SetInt32("UserId", atleta.Id);
                        context.Session.SetString("Ruolo", "Atleta");
                        context.Session.SetString("Nome", atleta.Nome);
                        context.Session.SetString("Cognome", atleta.Cognome);
                        context.Session.SetString("NomeCompleto", $"{atleta.Nome} {atleta.Cognome}");
                        context.Session.SetInt32("SquadraId", atleta.SquadraId);
                        context.Session.SetInt32("Altezza", atleta.Altezza);
                        context.Session.SetString("Peso", atleta.Peso.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
                else if (ruolo == "Staff")
                {
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
            // Cookie scaduto, invalido o vecchio formato: lo rimuoviamo per forzare un login pulito
            context.Response.Cookies.Delete("OnusAuth");
        }
    }

    await next();
});

app.MapControllers();
app.MapRazorPages();

app.Run();