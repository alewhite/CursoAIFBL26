using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class CreacionDeEstudiosTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-09: con título y fecha válidos, el estudio queda almacenado y aparece en el listado")]
    public async Task TituloYFechaValidos_CreaElEstudio()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Analisis de sangre", Hoy);

        var estudios = await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno);
        Assert.Single(estudios);
        Assert.Equal("Analisis de sangre", estudios[0].Titulo);

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();
        Assert.Contains("Analisis de sangre", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-10: con el título vacío, el sistema no crea el estudio y muestra un error")]
    public async Task TituloVacio_NoCreaElEstudio()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioAsync(string.Empty, Hoy);

        Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
        Assert.Contains("error", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-11: con una fecha inválida, el sistema no crea el estudio y muestra un error")]
    public async Task FechaInvalida_NoCreaElEstudio()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioConFechaCrudaAsync("Estudio con fecha rara", "31/02/2026");

        Assert.Equal(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Empty(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
    }

    [Fact(DisplayName = "AC-91: una fecha posterior al día en curso se rechaza y la del día se acepta")]
    public async Task FechaFutura_SeRechaza()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio de mañana", Hoy.AddDays(1));
        Assert.Empty(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));

        await cliente.CrearEstudioAsync("Estudio de hoy", Hoy);
        Assert.Single(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
    }

    [Fact(DisplayName = "AC-106: un título de 201 caracteres se rechaza y uno de 200 se acepta")]
    public async Task TituloDemasiadoLargo_SeRechaza()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync(new string('a', 201), Hoy);
        Assert.Empty(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));

        await cliente.CrearEstudioAsync(new string('b', 200), Hoy);
        Assert.Single(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
    }

    [Fact(DisplayName = "RNF-70: una descripción de más de 2.000 caracteres se rechaza")]
    public async Task DescripcionDemasiadoLarga_SeRechaza()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Con descripción larga", Hoy, descripcion: new string('c', 2001));

        Assert.Empty(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
    }
}
