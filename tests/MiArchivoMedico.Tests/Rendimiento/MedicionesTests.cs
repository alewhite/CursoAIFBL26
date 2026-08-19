using System.Diagnostics;
using MiArchivoMedico.Tests.Apoyo;
using Xunit.Abstractions;

namespace MiArchivoMedico.Tests.Rendimiento;

/// <summary>
/// Mediciones de AC-51, AC-52 y AC-53. **No forman parte de la suite habitual**: sembrar 2.000
/// estudios la volvería lenta en cada corrida. Se ejecutan a pedido:
///
/// <code>dotnet test --filter "Category=Rendimiento"</code>
///
/// El percentil 95 se calcula sobre 20 repeticiones, tomando el valor en la posición 95 % de la
/// muestra ordenada. Se descartan las dos primeras corridas, que pagan la compilación de la consulta.
/// </summary>
[Trait("Category", "Rendimiento")]
public class MedicionesTests(ITestOutputHelper salida)
{
    private const int Repeticiones = 20;
    private const int Descartadas = 2;

    [Fact(DisplayName = "AC-51: con 2.000 estudios, el p95 de una búsqueda por metadatos es menor a 1 segundo")]
    public async Task Busqueda_P95MenorAUnSegundo()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        await SembradorDeVolumen.SembrarAsync(aplicacion, AplicacionDePrueba.UsuarioUno);

        var p95 = await MedirP95Async(async () => await cliente.BuscarAsync("ecografía"));
        salida.WriteLine($"AC-51 · búsqueda sobre 2.000 estudios · p95 = {p95:F0} ms (límite 1.000 ms)");

        Assert.True(p95 < 1_000, $"El p95 de la búsqueda fue {p95:F0} ms y AC-51 exige menos de 1.000 ms.");
    }

    [Fact(DisplayName = "AC-52: con 2.000 estudios, el p95 del listado inicial es menor a 2 segundos")]
    public async Task ListadoInicial_P95MenorADosSegundos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        await SembradorDeVolumen.SembrarAsync(aplicacion, AplicacionDePrueba.UsuarioUno);

        var p95 = await MedirP95Async(async () => await cliente.GetAsync("/Estudios"));
        salida.WriteLine($"AC-52 · listado inicial sobre 2.000 estudios · p95 = {p95:F0} ms (límite 2.000 ms)");

        Assert.True(p95 < 2_000, $"El p95 del listado fue {p95:F0} ms y AC-52 exige menos de 2.000 ms.");
    }

    [Fact(DisplayName = "RNF-24: la colección de referencia tiene exactamente 2.000 estudios")]
    public async Task ColeccionDeReferencia_TieneElVolumenEsperado()
    {
        using var aplicacion = new AplicacionDePrueba();
        await SembradorDeVolumen.SembrarAsync(aplicacion, AplicacionDePrueba.UsuarioUno);

        Assert.Equal(SembradorDeVolumen.EstudiosDelTecho,
            await SembradorDeVolumen.ContarAsync(aplicacion, AplicacionDePrueba.UsuarioUno));
    }

    /// <summary>
    /// AC-53 (carga de 10 MB en menos de 15 segundos a 10 Mbps) **no se mide acá**: exige limitar el
    /// ancho de banda del enlace, y el servidor de pruebas corre en memoria sin red de por medio. Su
    /// procedimiento está en verificacion-manual.md.
    /// </summary>
    private static async Task<double> MedirP95Async(Func<Task<HttpResponseMessage>> operacion)
    {
        var muestras = new List<double>();

        for (var i = 0; i < Repeticiones + Descartadas; i++)
        {
            var cronometro = Stopwatch.StartNew();
            var respuesta = await operacion();
            cronometro.Stop();

            Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
            if (i >= Descartadas) muestras.Add(cronometro.Elapsed.TotalMilliseconds);
        }

        muestras.Sort();
        var posicion = (int)Math.Ceiling(muestras.Count * 0.95) - 1;
        return muestras[Math.Clamp(posicion, 0, muestras.Count - 1)];
    }
}
