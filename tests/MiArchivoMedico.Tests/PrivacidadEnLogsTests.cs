using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

/// <summary>
/// Un ciclo de vida completo de un estudio cuyos metadatos son cadenas únicas e irrepetibles, y
/// después se busca cada una de esas cadenas en todo lo que la aplicación emitió (AC-43, AC-85).
/// </summary>
public class PrivacidadEnLogsTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    private static readonly string[] CadenasUnicas =
    [
        "zqxjtitulo7391", "zqxjprofesional7392", "zqxjinstitucion7393",
        "zqxjdescripcion7394", "zqxjetiqueta7395", "zqxjarchivo7396",
    ];

    [Fact(DisplayName = "AC-43: ninguna cadena del estudio aparece en los registros técnicos")]
    public async Task CicloDeVidaCompleto_NoDejaRastroEnLosRegistros()
    {
        using var aplicacion = new AplicacionDePruebaConRegistro();
        await EjercitarCicloDeVidaAsync(aplicacion);

        var registrado = aplicacion.Registro.TextoCompleto();

        foreach (var cadena in CadenasUnicas)
        {
            Assert.DoesNotContain(cadena, registrado, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "AC-85: ninguna cadena del estudio aparece en las métricas técnicas")]
    public async Task CicloDeVidaCompleto_NoDejaRastroEnLasMetricas()
    {
        using var aplicacion = new AplicacionDePrueba();
        var emitido = new List<string>();

        using var escucha = new MeterListener();
        escucha.InstrumentPublished = (instrumento, oyente) => oyente.EnableMeasurementEvents(instrumento);
        escucha.SetMeasurementEventCallback<long>((instrumento, medida, etiquetas, estado) =>
            emitido.Add(Describir(instrumento, etiquetas)));
        escucha.SetMeasurementEventCallback<double>((instrumento, medida, etiquetas, estado) =>
            emitido.Add(Describir(instrumento, etiquetas)));
        escucha.Start();

        await EjercitarCicloDeVidaAsync(aplicacion);
        escucha.RecordObservableInstruments();

        var texto = string.Join("\n", emitido);
        foreach (var cadena in CadenasUnicas)
        {
            Assert.DoesNotContain(cadena, texto, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string Describir(Instrument instrumento, ReadOnlySpan<KeyValuePair<string, object?>> etiquetas)
    {
        var partes = new List<string> { instrumento.Meter.Name, instrumento.Name };
        foreach (var etiqueta in etiquetas)
            partes.Add($"{etiqueta.Key}={etiqueta.Value}");
        return string.Join(" ", partes);
    }

    private static async Task EjercitarCicloDeVidaAsync(AplicacionDePrueba aplicacion)
    {
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync(
            CadenasUnicas[0], Hoy,
            profesional: CadenasUnicas[1],
            institucion: CadenasUnicas[2],
            descripcion: CadenasUnicas[3],
            etiquetas: CadenasUnicas[4],
            archivos: [($"{CadenasUnicas[5]}.pdf", ArchivosFicticios.PdfValido(), "application/pdf")]);

        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];

        await cliente.GetAsync("/Estudios");
        await cliente.GetAsync($"/Estudios/Detalle/{estudio.Id}");
        await cliente.GetAsync($"/Archivos/Ver/{estudio.Archivos[0].Id}");
        await cliente.BuscarAsync(CadenasUnicas[0]);
        await cliente.EliminarEstudioAsync(estudio.Id);
    }
}

/// <summary>Fábrica que captura todo lo que la aplicación registra.</summary>
public sealed class AplicacionDePruebaConRegistro : AplicacionDePrueba
{
    public RegistroCapturado Registro { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder constructor)
    {
        base.ConfigureWebHost(constructor);
        constructor.ConfigureServices(servicios => servicios.AddLogging(registro =>
        {
            registro.SetMinimumLevel(LogLevel.Trace);
            registro.AddProvider(Registro);
        }));
    }
}

public sealed class RegistroCapturado : ILoggerProvider
{
    private readonly List<string> _lineas = [];
    private readonly object _candado = new();

    public ILogger CreateLogger(string categoria) => new Anotador(this, categoria);

    public string TextoCompleto()
    {
        lock (_candado) return string.Join("\n", _lineas);
    }

    private void Anotar(string linea)
    {
        lock (_candado) _lineas.Add(linea);
    }

    public void Dispose() { }

    private sealed class Anotador(RegistroCapturado destino, string categoria) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState estado) where TState : notnull
        {
            destino.Anotar($"{categoria} scope {estado}");
            return null;
        }

        public bool IsEnabled(LogLevel nivel) => true;

        public void Log<TState>(
            LogLevel nivel, EventId evento, TState estado, Exception? error, Func<TState, Exception?, string> formatear)
        {
            destino.Anotar($"{categoria} {nivel} {formatear(estado, error)} {estado} {error}");
        }
    }
}
