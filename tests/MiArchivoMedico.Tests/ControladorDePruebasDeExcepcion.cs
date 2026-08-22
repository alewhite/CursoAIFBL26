using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Controlador de solo-tests que provoca una excepción para ejercitar el manejo de errores
/// genérico de <c>Program.cs</c> fuera de Development (E1.4 de FEAT-001b). Reemplaza al
/// <see cref="IStartupFilter"/> con <c>app.Run(throw)</c> que usaba FEAT-001a: una vez instalada la
/// FallbackPolicy, ese middleware terminal quedaba por detrás del challenge 302 de la autorización
/// y su excepción ya no llegaba al <c>UseExceptionHandler</c>.
///
/// La acción es <c>[AllowAnonymous]</c> a propósito: la excepción debe dispararse sin sesión, por el
/// único camino que deja abierto la FallbackPolicy. El controlador vive SOLO en el ensamblado de
/// tests, se registra con <c>AddApplicationPart</c> desde el build de pruebas y jamás desde el
/// ensamblado de la aplicación (E1.2 prohibe que el proyecto de producción lo referencie). Cuando
/// el entorno de ejecución es Production, sus acciones quedan por delante de ninguna otra ruta y su
/// excepción la atrapa el pipeline real; por eso el test se ejecuta bajo
/// <c>UseEnvironment("Production")</c>.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("Pruebas/De-Excepcion")]
public sealed class ControladorDePruebasDeExcepcion : ControllerBase
{
    /// <summary>Marcador que la excepción provocada lleva en su mensaje.</summary>
    internal const string MarcadorDeExcepcion = "MarcadorDeExcepcionDeDiagnostico";

    /// <summary>
    /// Acción que siempre lanza <see cref="InvalidOperationException"/>, con el mismo mensaje que
    /// el manejador genérico de la aplicación escribe en la respuesta 500. Se monta únicamente en
    /// el entorno Production de los tests: en Development la aplicación usa la página de error
    /// detallada y este caso no aplica.
    /// </summary>
    [HttpPost]
    [Route("Provocar")]
    public IActionResult Provocar() => throw new InvalidOperationException(MarcadorDeExcepcion);
}