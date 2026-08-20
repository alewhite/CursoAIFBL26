var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

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
