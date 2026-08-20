using MiArchivoMedico.Web.Data;
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

var app = builder.Build();

// Modo WAL y migraciones, en ese orden y antes de cualquier otra cosa que toque la base (FR-04,
// ADR-001). Un fallo de cualquiera de las dos propaga la excepción y aborta el arranque.
ubicacionDeLaBase.PrepararEnModoWal();

using (var alcanceDeArranque = app.Services.CreateScope())
{
    alcanceDeArranque.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
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
