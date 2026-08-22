using MiArchivoMedico.Web.Accounts;
using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// La ubicación de la base se resuelve ANTES de construir el host: si falta, si cae dentro del
// árbol de la aplicación o si su directorio no puede crearse, el arranque aborta acá y la
// aplicación no llega a atender una sola solicitud (NFR-04, AC-06, M-3).
var ubicacionDeLaBase = SqliteLocation.Resolver(builder.Configuration, builder.Environment);

// `AddDbContext` con alcance scoped, sin pooling y sin `IDbContextFactory`: el pooling y las
// factorías singleton dificultan inyectar más adelante un accesor del usuario autenticado, que es
// lo que va a necesitar el filtro global por propietario del aislamiento entre cuentas. Acá no se
// construye nada de ese mecanismo —todavía no hay datos que aislar— pero tampoco se le cierra la
// puerta.
builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseSqlite(ubicacionDeLaBase.CadenaDeConexion));

// `AddIdentityCore` y no `AddIdentity<,>`: la segunda instala además el esquema de autenticación
// por cookies con sus valores por defecto —14 días, deslizante—, que es justamente la política que
// FEAT-001c tiene que endurecer, y la dejaría decidida por omisión antes de que nadie la decida.
// El `AddSignInManager()` es obligatorio: el `CuentaController` inyecta `SignInManager<AppUser>` y
// sin este registro el contenedor no lo resuelve; el `SignInManager` firma la cookie con
// `IdentityConstants.ApplicationScheme` de forma hardcodeada.
builder.Services
    .AddIdentityCore<AppUser>(opciones =>
    {
        // NFR-03 exige el mínimo de longitud y nada más: las reglas de composición serían alcance
        // no pedido. El mínimo lo aplica este mecanismo central, nunca una comprobación propia.
        opciones.Password.RequiredLength = AccountProvisioner.LongitudMinimaDeLaContrasena;
        opciones.Password.RequireDigit = false;
        opciones.Password.RequireLowercase = false;
        opciones.Password.RequireUppercase = false;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Password.RequiredUniqueChars = 1;
    })
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>();

// Esquema de cookies delimitado a este sub-ticket: LoginPath y AccessDeniedPath apuntan ambos al
// inicio de sesión FEAT-001b. La cookie es SECURE en toda configuración sin excepciones por entorno
// (NFR-01 no permite degradarla en desarrollo ni en tests), HttpOnly (inaccesible a JavaScript) y
// SameSite=Strict. No se configura ExpireTimeSpan ni Cookie.Expiration: la cookie es de sesión del
// navegador, caduca al cerrarlo (NFR-02). El bloqueo por intentos, la sesión única y la expiración
// por inactividad pertenecen a FEAT-001c y quedan explícitamente fuera de este sub-ticket.
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, opciones =>
    {
        opciones.LoginPath = "/Cuenta/IniciarSesion";
        opciones.AccessDeniedPath = "/Cuenta/IniciarSesion";
        opciones.Cookie.Name = "MiArchivoMedico.Auth";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Strict;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// NFR-07: único mecanismo central de autorización. La FallbackPolicy exige usuario autenticado en
// TODA ruta con endpoint; solo el par GET/POST de `IniciarSesion` declara `[AllowAnonymous]`
// (FEAT-001b, Bloque 2). Las rutas no mapeadas no producen endpoint y quedan fuera de la política.
builder.Services.AddAuthorization(opciones =>
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Se declara `AddControllersWithViews` (Razor Views) aunque este sub-ticket aún no agrega
// controladores: el pipeline necesita mapear la ruta por defecto y el host de pruebas agrega su
// ApplicationPart del ensamblado de tests sobre esta base. El framework compartido lo provee el SDK
// `Microsoft.NET.Sdk.Web`; no se agrega ningún PackageReference suelto.
builder.Services.AddControllersWithViews();

// Parámetros del hash fijados en código y no en configuración externa (ADR-002): un despliegue con
// `IterationCount: 1000` arrancaría sin que nada avisara.
builder.Services.Configure<PasswordHasherOptions>(opciones =>
{
    opciones.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
    opciones.IterationCount = AccountProvisioner.IteracionesDePbkdf2;
});

// Las altas llegan por variables de entorno o user-secrets, jamás por un archivo versionado
// (ADR-003, M-2). Se enlazan de forma diferida: el host de pruebas agrega su fuente de
// configuración durante `Build()`, después de esta línea.
builder.Services.Configure<AccountSeedOptions>(
    builder.Configuration.GetSection(AccountSeedOptions.SeccionDeLaAplicacion));

builder.Services.AddScoped<AccountProvisioner>();

var app = builder.Build();

// Modo WAL y migraciones, en ese orden y antes de cualquier otra cosa que toque la base (FR-04,
// ADR-001). Un fallo de cualquiera de las dos propaga la excepción y aborta el arranque.
ubicacionDeLaBase.PrepararEnModoWal();

using (var alcanceDeArranque = app.Services.CreateScope())
{
    alcanceDeArranque.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

    // La siembra corre DESPUÉS de las migraciones —escribe sobre el esquema de Identity— y sus
    // rechazos no abortan el arranque: la aplicación levanta con las cuentas válidas (AC-03, AC-04).
    await alcanceDeArranque.ServiceProvider.GetRequiredService<AccountProvisioner>().SembrarAsync();
}

// Pipeline de middleware, en orden estricto (spec FEAT-001b, Bloque 1):
//   1. UseExceptionHandler PRIMERO — heredado de FEAT-001a: 500 + text/plain genérico desde hoy.
//   2. HSTS y redirección HTTPS SOLO fuera de Development (AC-08, NFR-04; NFR-01 no degrada la
//      cookie a HTTP en desarrollo, E1.3).
//   3. UseRouting explícito — exige endpoint routing para que la autorización sea por endpoint.
//   4. UseAuthentication SIEMPRE antes de UseAuthorization: invertir el orden deja la
//      FallbackPolicy sin sesión que evaluar y cada ruta redirigiría al login (E1.1).
//   5. UseAuthorization — aplica la FallbackPolicy (NFR-07).
//   6. MapControllerRoute — jamás MapFallback (NFR-07).
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(rama => rama.Run(async contexto =>
    {
        contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
        contexto.Response.ContentType = "text/plain; charset=utf-8";

        // Sin traza, sin tipo de excepción y sin datos médicos: AGENTS.md prohíbe exponerlos en
        // mensajes de error, y una página detallada fuera de desarrollo los filtraría.
        await contexto.Response.WriteAsync("Se produjo un error inesperado.");
    }));
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// La superficie HTTP privada se mapea al arrancar: `Cuenta` es el controlador por defecto y
// `Privada` la acción por defecto, de modo que `/` resuelve a la página privada. Toda ruta mapeada
// queda bajo la FallbackPolicy; las no mapeadas responden 404 real (Sup_FEAT-001b §Bloque 1).
app.UseRouting();
app.MapControllerRoute("default", "{controller=Cuenta}/{action=Privada}/{id?}");
app.UseAuthentication();
app.UseAuthorization();

app.Run();

/// <summary>
/// Punto de entrada expuesto para que el proyecto de tests pueda instanciar el host con
/// <c>WebApplicationFactory&lt;Program&gt;</c>. No agrega comportamiento.
/// </summary>
public partial class Program;
