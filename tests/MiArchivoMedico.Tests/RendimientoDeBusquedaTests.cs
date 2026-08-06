using System.Diagnostics;
using System.Net;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Rendimiento sobre el volumen que define RNF-24: 2.000 estudios.
/// </summary>
/// <remarks>
/// La medición es del servidor en proceso: cubre el modelo, la consulta a SQLite y el renderizado, que es
/// donde puede degradarse la búsqueda, pero no incluye red ni TLS. Sirve para detectar una regresión de
/// consulta —una búsqueda que deje de usar las columnas normalizadas, por ejemplo—, no para certificar el
/// tiempo de respuesta de un despliegue real.
/// </remarks>
public class RendimientoDeBusquedaTests : IAsyncLifetime
{
    private const int CantidadDeEstudios = 2_000;
    private const int Muestras = 20;

    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-51: sobre 2.000 estudios, el p95 de la búsqueda es menor a 1 segundo")]
    public async Task ElP95DeLaBusqueda_EsMenorAUnSegundo()
    {
        var cliente = await ConDosMilEstudiosAsync();

        var terminos = new[] { "cardiologia", "rivas", "hospital", "rutina", "control" };

        var p95 = await MedirP95Async(indice =>
            $"/Estudios?Texto={terminos[indice % terminos.Length]}", cliente);

        Assert.True(p95 < TimeSpan.FromSeconds(1), $"El p95 de la búsqueda fue {p95.TotalMilliseconds:F0} ms.");
    }

    [Fact(DisplayName = "AC-52: sobre 2.000 estudios, el p95 del listado inicial es menor a 2 segundos")]
    public async Task ElP95DelListado_EsMenorADosSegundos()
    {
        var cliente = await ConDosMilEstudiosAsync();

        var p95 = await MedirP95Async(_ => "/Estudios", cliente);

        Assert.True(p95 < TimeSpan.FromSeconds(2), $"El p95 del listado fue {p95.TotalMilliseconds:F0} ms.");
    }

    private async Task<TimeSpan> MedirP95Async(Func<int, string> ruta, HttpClient cliente)
    {
        var tiempos = new List<TimeSpan>(Muestras);

        // Una pasada previa descartada: la primera consulta paga la compilación del modelo de EF Core y
        // del árbol de expresiones, que no se repite en régimen.
        (await cliente.GetAsync(ruta(0))).EnsureSuccessStatusCode();

        for (var i = 0; i < Muestras; i++)
        {
            var cronometro = Stopwatch.StartNew();
            var respuesta = await cliente.GetAsync(ruta(i));
            cronometro.Stop();

            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            tiempos.Add(cronometro.Elapsed);
        }

        tiempos.Sort();

        // Percentil 95 por rango: con 20 muestras es la 19.ª.
        var posicion = (int)Math.Ceiling(0.95 * tiempos.Count) - 1;
        return tiempos[posicion];
    }

    private async Task<HttpClient> ConDosMilEstudiosAsync()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        await _app.EnAlcanceAsync(async servicios =>
        {
            var usuarios = servicios.GetRequiredService<UserManager<UsuarioApp>>();
            var propietario = await usuarios.FindByNameAsync(AplicacionDePrueba.Usuario);

            var contexto = servicios.GetRequiredService<ArchivoMedicoDbContext>();

            var instituciones = new[]
            {
                "Hospital Central", "Clinica del Norte", "Laboratorio Sur", "Centro Ficticio Este",
            };
            var profesionales = new[] { "Dra. Rivas", "Dr. Molina", "Dra. Paz", "Dr. Sosa" };

            var estudios = Enumerable.Range(0, CantidadDeEstudios).Select(i => new Estudio
            {
                // El propietario se asigna a mano: fuera de una solicitud HTTP no hay usuario en sesión
                // del que estamparlo.
                OwnerId = propietario!.Id,
                Titulo = $"Estudio ficticio numero {i:D4}",
                Fecha = new DateOnly(2020, 1, 1).AddDays(i % 2_000),
                Profesional = profesionales[i % profesionales.Length],
                Institucion = instituciones[i % instituciones.Length],
                Descripcion = i % 3 == 0 ? "control anual de rutina" : "seguimiento periodico",
                CreadoUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(i),
                Etiquetas =
                [
                    new EtiquetaDeEstudio { OwnerId = propietario.Id, Texto = i % 2 == 0 ? "cardiología" : "clínica" },
                ],
            });

            contexto.Estudios.AddRange(estudios);
            await contexto.SaveChangesAsync();
        });

        // Sin esta comprobación, una siembra que fallara en silencio haría que la medición se hiciera
        // sobre una base vacía y el test pasara por el motivo equivocado.
        var listado = await cliente.GetAsync("/Estudios");
        listado.EnsureSuccessStatusCode();
        Assert.Contains(
            $"Se encontraron {CantidadDeEstudios} estudios.",
            WebUtility.HtmlDecode(await listado.Content.ReadAsStringAsync()));

        return cliente;
    }
}
