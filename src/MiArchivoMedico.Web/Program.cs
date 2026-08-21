using MiArchivoMedico.Web.Accounts;
using MiArchivoMedico.Web.Data;
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
    .AddEntityFrameworkStores<AppDbContext>();

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

// Manejo de errores: es lo ÚNICO que este sub-ticket configura en el pipeline. La autenticación,
// la autorización y la política de cookies pertenecen a FEAT-001b y FEAT-001c; dejarlas
// configuradas por adelantado decidiría por ellas con los valores por defecto del framework.
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

// No se mapea ninguna ruta: la superficie HTTP de este sub-ticket es deliberadamente vacía y
// AC-02 lo verifica. Provocar una excepción para comprobar el manejador es cosa del proyecto de
// tests, que la inyecta con un IStartupFilter; la aplicación no expone un atajo de diagnóstico
// que además quedaría fuera de la autorización por defecto que instalará FEAT-001b.
app.Run();

/// <summary>
/// Punto de entrada expuesto para que el proyecto de tests pueda instanciar el host con
/// <c>WebApplicationFactory&lt;Program&gt;</c>. No agrega comportamiento.
/// </summary>
public partial class Program;
