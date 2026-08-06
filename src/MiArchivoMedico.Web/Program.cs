using MiArchivoMedico.Web.Archivos;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión llega por user-secrets o variables de entorno, nunca versionada.
var cadenaDeConexion = builder.Configuration.GetConnectionString("ArchivoMedico")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'ArchivoMedico'. Definila con user-secrets o una variable de entorno.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddScoped<ServicioDeCuentas>();
builder.Services.AddScoped<EventosDeSesion>();

builder.Services.AddDbContext<ArchivoMedicoDbContext>(opciones =>
    opciones.UseSqlite(cadenaDeConexion));

builder.Services.Configure<OpcionesDeAlmacenamiento>(
    builder.Configuration.GetSection(OpcionesDeAlmacenamiento.Seccion));

// Si la clave de cifrado no se resuelve desde configuración externa, la aplicación no arranca: guardar
// archivos médicos en claro no es una degradación aceptable (RNF-02, RNF-62, AC-83).
// Sección ausente y clave vacía deben fallar igual, así que la instancia por omisión no se saltea.
(builder.Configuration.GetSection(OpcionesDeAlmacenamiento.Seccion).Get<OpcionesDeAlmacenamiento>()
    ?? new OpcionesDeAlmacenamiento()).ResolverClave();

builder.Services.AddSingleton<IAlmacenamientoDeArchivos, AlmacenamientoCifradoEnDisco>();
builder.Services.AddSingleton<ValidadorDeArchivos>();
builder.Services.AddScoped<ServicioDeCargaDeArchivos>();
builder.Services.AddScoped<BuscadorDeEstudios>();

// El límite de cuerpo cubre un envío completo: hasta 20 archivos de 50 MB no entran, pero sí el uso real
// del formulario. Cada archivo se acota individualmente en ValidadorDeArchivos (RNF-14).
builder.Services.Configure<FormOptions>(opciones =>
{
    opciones.MultipartBodyLengthLimit = 250L * 1024 * 1024;
    opciones.MultipartHeadersLengthLimit = 32 * 1024;
});
builder.WebHost.ConfigureKestrel(opciones => opciones.Limits.MaxRequestBodySize = 250L * 1024 * 1024);

builder.Services
    .AddIdentity<UsuarioApp, IdentityRole>(opciones =>
    {
        // Bloqueo tras 5 intentos fallidos, sostenido 15 minutos (RNF-60).
        opciones.Lockout.AllowedForNewUsers = true;
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        opciones.User.RequireUniqueEmail = true;
        opciones.Password.RequiredLength = 12;
        opciones.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ArchivoMedicoDbContext>()
    .AddDefaultTokenProviders();

// RNF-03 admite PBKDF2 solo con HMAC-SHA256; el hasher que trae Identity deriva con HMAC-SHA512 y no
// permite cambiarlo, así que se reemplaza (AC-76). Las iteraciones se fijan explícitamente.
builder.Services.Configure<PasswordHasherOptions>(opciones => opciones.IterationCount = 210_000);
builder.Services.AddScoped<IPasswordHasher<UsuarioApp>, HasherPbkdf2Sha256>();

builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.Cookie.Name = "__Host-archivo-medico";
    opciones.Cookie.HttpOnly = true;                              // RNF-11, AC-58
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;     // RNF-11, AC-58
    opciones.Cookie.SameSite = SameSiteMode.Strict;               // RNF-11, AC-58

    // Expiración por inactividad: 30 minutos deslizantes (RNF-04, AC-06).
    opciones.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    opciones.SlidingExpiration = true;

    // Duración absoluta de 24 horas y validación del sello de seguridad (RNF-05, RNF-12).
    opciones.EventsType = typeof(EventosDeSesion);

    opciones.LoginPath = "/Cuenta/Login";
    opciones.LogoutPath = "/Cuenta/Logout";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});

builder.Services.Configure<AntiforgeryOptions>(opciones =>
{
    opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAuthorization(opciones =>
{
    // Todo requiere sesión salvo lo marcado con [AllowAnonymous]: agregar una pantalla nueva no puede
    // dejarla abierta por olvido (RF-01, RNF-51, AC-01).
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();   // RNF-01, AC-56
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await InicializadorDeBaseDeDatos.InicializarAsync(app.Services);

app.Run();

/// <summary>Punto de entrada expuesto para las pruebas de integración.</summary>
public partial class Program;
