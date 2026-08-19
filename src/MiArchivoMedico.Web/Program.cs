using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using MiArchivoMedico.Web.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Servicios;

var constructor = WebApplication.CreateBuilder(args);

// --- Configuración externa obligatoria -------------------------------------------------
// Sin cualquiera de estas tres la aplicación NO arranca, en lugar de degradar una garantía
// de seguridad (RNF-62, AC-83).
var cadenaDeConexion = constructor.Configuration.GetConnectionString("ArchivoMedico");
if (string.IsNullOrWhiteSpace(cadenaDeConexion))
    throw new InvalidOperationException(
        "Falta ConnectionStrings:ArchivoMedico. Se carga por user-secrets o variables de entorno.");

var almacenamiento = constructor.Configuration.GetSection("Almacenamiento").Get<OpcionesDeAlmacenamiento>()
    ?? new OpcionesDeAlmacenamiento();

if (string.IsNullOrWhiteSpace(almacenamiento.Ruta))
    throw new InvalidOperationException("Falta Almacenamiento:Ruta, la carpeta de archivos cifrados.");

if (string.IsNullOrWhiteSpace(almacenamiento.ClaveBase64))
    throw new InvalidOperationException(
        "Falta Almacenamiento:ClaveBase64. Sin clave de cifrado la aplicación no puede arrancar: " +
        "guardar archivos en claro violaría RNF-02.");

var clave = Convert.FromBase64String(almacenamiento.ClaveBase64);
if (clave.Length != 32)
    throw new InvalidOperationException(
        $"Almacenamiento:ClaveBase64 decodifica a {clave.Length} bytes y AES-256 exige exactamente 32.");

if (almacenamiento.CupoTotalEnBytes <= 0)
    almacenamiento.CupoTotalEnBytes = OpcionesDeAlmacenamiento.CupoPorOmisionEnBytes;

constructor.Services.AddSingleton(almacenamiento);

// --- Servicios --------------------------------------------------------------------------
constructor.Services.AddSingleton(TimeProvider.System);
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<IUsuarioActual, UsuarioActual>();
constructor.Services.AddScoped<ControlDeIntentosDeInicioDeSesion>();
constructor.Services.AddSingleton<ValidadorDeArchivos>();
constructor.Services.AddSingleton<IAlmacenamientoDeArchivos, AlmacenamientoCifradoEnDisco>();
constructor.Services.AddScoped<ServicioDeCargaDeArchivos>();
constructor.Services.AddScoped<GeneradorDeTokenDeArchivo>();

constructor.Services.AddDbContext<ArchivoMedicoDbContext>(o => o.UseSqlite(cadenaDeConexion));

constructor.Services.Configure<PasswordHasherOptions>(o =>
    o.IterationCount = HasherPbkdf2Sha256.IteracionesMinimas);

constructor.Services
    .AddIdentity<Usuario, IdentityRole>(o =>
    {
        o.Password.RequiredLength = CuentaInicial.LargoMinimoDeContrasena;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireDigit = false;
        o.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ArchivoMedicoDbContext>()
    .AddDefaultTokenProviders();

// PBKDF2-HMAC-SHA256 en lugar del SHA512 que trae Identity, que no es ninguna de las tres
// combinaciones que admite RNF-03.
constructor.Services.AddScoped<IPasswordHasher<Usuario>, HasherPbkdf2Sha256>();

constructor.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "archivo-medico";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
    // Cookie de sesión del navegador: no sobrevive a su cierre (RNF-68, AC-104).
    o.Cookie.MaxAge = null;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = true;
    o.LoginPath = "/Cuenta/InicioDeSesion";
    o.LogoutPath = "/Cuenta/CerrarSesion";
    o.AccessDeniedPath = "/Cuenta/InicioDeSesion";

    // Dos reglas que el manejador de la cookie no trae por sí solo.
    o.Events.OnValidatePrincipal = async contexto =>
    {
        var reloj = contexto.HttpContext.RequestServices.GetRequiredService<TimeProvider>();

        // 1. Tope absoluto de 24 horas: prevalece sobre la ventana deslizante, así que una sesión con
        //    actividad sostenida termina igual (RNF-05, AC-07).
        var marca = contexto.Principal?.FindFirst(CuentaController.ClaimDeInicioDeSesion)?.Value;
        if (long.TryParse(marca, out var ticks)
            && reloj.GetUtcNow() - new DateTimeOffset(ticks, TimeSpan.Zero) >= TimeSpan.FromHours(24))
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        // 2. Una sola sesión activa por cuenta: si la marca de seguridad del usuario cambió, esta
        //    cookie es de un ingreso anterior y deja de valer (RNF-68, AC-103). Se compara sin volver
        //    a firmar, para no reiniciar el tope absoluto ni perder el claim del punto 1.
        var identificador = contexto.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var marcaDeLaCookie = contexto.Principal?.FindFirst("AspNet.Identity.SecurityStamp")?.Value;
        if (identificador is null || marcaDeLaCookie is null) return;

        var administrador = contexto.HttpContext.RequestServices.GetRequiredService<UserManager<Usuario>>();
        var usuario = await administrador.FindByIdAsync(identificador);
        if (usuario is null || usuario.SecurityStamp != marcaDeLaCookie)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

// Autorización por omisión: una pantalla nueva nace protegida y abrirla exige [AllowAnonymous]
// explícito (Principio I).
constructor.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

constructor.Services.AddDistributedMemoryCache();
constructor.Services.AddSession(o =>
{
    o.Cookie.Name = "archivo-medico-estado";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromMinutes(30);
});

// El codificador de HTML, por omisión, convierte todo lo que sale de Basic Latin en entidades
// numéricas: en una aplicación en español eso vuelve ilegible cualquier texto acentuado del marcado.
// Ampliarlo al suplemento Latin-1 no relaja el escapado de <, >, & ni de las comillas, que es lo que
// evita la inyección.
constructor.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Latin1Supplement));

constructor.Services.AddControllersWithViews();

var aplicacion = constructor.Build();

if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Home/Error");
    // Obliga al navegador a usar HTTPS en las visitas posteriores, por un año (RNF-01).
    aplicacion.UseHsts();
}

aplicacion.UseHttpsRedirection();
aplicacion.UseStaticFiles();
aplicacion.UseRouting();
aplicacion.UseAuthentication();
aplicacion.UseAuthorization();
aplicacion.UseSession();

aplicacion.MapControllerRoute("por-omision", "{controller=Home}/{action=Index}/{id?}");

await InicializadorDeBaseDeDatos.InicializarAsync(aplicacion.Services);

// Restos que un corte pudiera haber dejado en el área de tránsito.
using (var alcanceDeArranque = aplicacion.Services.CreateScope())
    alcanceDeArranque.ServiceProvider.GetRequiredService<ServicioDeCargaDeArchivos>().PurgarTransito();

aplicacion.Run();

/// <summary>Expuesta para que la fábrica de pruebas pueda levantar la aplicación real.</summary>
public partial class Program;
