using System.Net;
using MiArchivoMedico.Tests.Infraestructura;

namespace MiArchivoMedico.Tests;

/// <summary>Búsqueda por metadatos y filtros combinables (RF-16, RF-17, RF-20 a RF-23, RNF-55).</summary>
public class BusquedaYFiltrosTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Theory(DisplayName = "AC-29, AC-30, AC-71 a AC-73: la búsqueda cubre los cinco campos de RF-16")]
    [InlineData("abdominal", "Ecografía abdominal")]       // título (AC-71)
    [InlineData("rutina", "Ecografía abdominal")]          // descripción (AC-72)
    [InlineData("Rivas", "Ecografía abdominal")]           // profesional (AC-73)
    [InlineData("Central", "Ecografía abdominal")]         // institución (AC-29)
    [InlineData("cardiología", "Ecografía abdominal")]     // etiqueta (AC-30)
    public async Task LaBusqueda_CubreLosCincoCampos(string termino, string esperado)
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        var html = await BuscarAsync(cliente, termino);

        Assert.Contains(esperado, html);
        Assert.DoesNotContain("Radiografía de tórax", html);
    }

    [Theory(DisplayName = "AC-45, AC-46: la búsqueda ignora mayúsculas, acentos y espacios sobrantes")]
    [InlineData("  hospital central  ")]   // AC-45
    [InlineData("cardiologia")]            // AC-46: sin acento
    [InlineData("ECOGRAFIA")]
    public async Task LaBusqueda_IgnoraMayusculasAcentosYEspacios(string termino)
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        var html = await BuscarAsync(cliente, termino);

        Assert.Contains("Ecografía abdominal", html);
    }

    [Fact(DisplayName = "AC-31: el rango de fechas deja solo los estudios incluidos")]
    public async Task ElRangoDeFechas_DejaSoloLosIncluidos()
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        var html = await LeerAsync(cliente, "/Estudios?Desde=2025-01-01&Hasta=2025-12-31");

        Assert.Contains("Radiografía de tórax", html);            // 2025-06-02
        Assert.DoesNotContain("Ecografía abdominal", html);        // 2026-03-15
        Assert.DoesNotContain("Análisis de sangre", html);         // 2024-02-20
        Assert.Contains("Se encontró 1 estudio.", html);
    }

    [Fact(DisplayName = "AC-34: el filtro por institución deja solo los estudios asociados")]
    public async Task ElFiltroPorInstitucion_DejaSoloLosAsociados()
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        var html = await LeerAsync(cliente, "/Estudios?Institucion=Hospital%20Central");

        Assert.Contains("Ecografía abdominal", html);
        Assert.DoesNotContain("Radiografía de tórax", html);
    }

    [Fact(DisplayName = "AC-35: la búsqueda de texto se combina con el filtro por institución")]
    public async Task LaBusqueda_SeCombinaConElFiltro()
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        // "ecografía" aparece en dos estudios, pero solo uno es del Hospital Central.
        await cliente.CrearYObtenerIdAsync(
            "Ecografía de rodilla", "2026-04-01", institucion: "Clinica del Norte");

        var html = await LeerAsync(
            cliente, "/Estudios?Texto=ecograf%C3%ADa&Institucion=Hospital%20Central");

        Assert.Contains("Ecografía abdominal", html);
        Assert.DoesNotContain("Ecografía de rodilla", html);
        Assert.Contains("Se encontró 1 estudio.", html);
    }

    [Fact(DisplayName = "AC-36: limpiar filtros restablece el listado completo")]
    public async Task LimpiarFiltros_RestableceElListadoCompleto()
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        var filtrado = await LeerAsync(
            cliente, "/Estudios?Texto=abdominal&Desde=2026-01-01&Institucion=Hospital%20Central");
        Assert.Contains("Se encontró 1 estudio.", filtrado);

        // El enlace de limpiar apunta al listado sin ningún criterio: una sola acción (RF-22).
        Assert.Contains("Limpiar filtros", filtrado);

        var completo = await LeerAsync(cliente, "/Estudios");

        Assert.Contains("Se encontraron 3 estudios.", completo);
        Assert.Contains("Ecografía abdominal", completo);
        Assert.Contains("Radiografía de tórax", completo);
        Assert.Contains("Análisis de sangre", completo);
        Assert.DoesNotContain("Limpiar filtros", completo);
    }

    [Fact(DisplayName = "AC-37: la interfaz informa la cantidad de estudios encontrados")]
    public async Task LaInterfaz_InformaLaCantidadEncontrada()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        for (var i = 0; i < 5; i++)
        {
            await cliente.CrearYObtenerIdAsync($"Control periodico {i}", "2026-02-01");
        }

        await cliente.CrearYObtenerIdAsync("Estudio distinto", "2026-02-01");

        var html = await BuscarAsync(cliente, "control periodico");

        Assert.Contains("Se encontraron 5 estudios.", html);
    }

    [Fact(DisplayName = "AC-49: el contador no incluye los estudios de la otra cuenta")]
    public async Task ElContador_NoIncluyeEstudiosAjenos()
    {
        await ConEstudiosDeEjemploAsync();   // tres estudios del primer usuario

        var segundo = _app.CrearCliente();
        await segundo.IniciarSesionAsync(AplicacionDePrueba.OtroUsuario, AplicacionDePrueba.OtraContrasena);
        await segundo.CrearYObtenerIdAsync("Estudio del segundo", "2026-01-05");

        var html = await LeerAsync(segundo, "/Estudios");

        Assert.Contains("Se encontró 1 estudio.", html);
        Assert.DoesNotContain("Ecografía abdominal", html);
    }

    [Fact(DisplayName = "AC-54: con 26 estudios se muestran 25 y hay control para la página siguiente")]
    public async Task ConVeintiseisEstudios_SePaginaDeAVeinticinco()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        for (var i = 1; i <= 26; i++)
        {
            // Fechas descendentes para que el orden sea predecible: el 01 es el más reciente.
            await cliente.CrearYObtenerIdAsync(
                $"Estudio numero {i:D2}", $"2026-01-{27 - i:D2}");
        }

        var primera = await LeerAsync(cliente, "/Estudios");

        Assert.Equal(25, ContarEstudiosListados(primera));
        Assert.Contains("Se encontraron 26 estudios.", primera);
        Assert.Contains("pagina-siguiente", primera);
        Assert.DoesNotContain("pagina-anterior", primera);

        var segunda = await LeerAsync(cliente, "/Estudios?Pagina=2");

        Assert.Equal(1, ContarEstudiosListados(segunda));
        Assert.Contains("Estudio numero 26", segunda);
        Assert.Contains("pagina-anterior", segunda);
    }

    [Fact(DisplayName = "RF-21: la paginación conserva los filtros aplicados")]
    public async Task LaPaginacion_ConservaLosFiltros()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        for (var i = 1; i <= 26; i++)
        {
            await cliente.CrearYObtenerIdAsync(
                $"Control anual {i:D2}", $"2026-01-{27 - i:D2}", institucion: "Hospital Central");
        }

        await cliente.CrearYObtenerIdAsync("Otro estudio", "2026-01-05", institucion: "Clinica del Norte");

        var primera = await LeerAsync(cliente, "/Estudios?Texto=control%20anual");
        Assert.Contains("Se encontraron 26 estudios.", primera);

        // El enlace a la página siguiente arrastra el término de búsqueda.
        Assert.Contains("Texto=control", primera);

        var segunda = await LeerAsync(cliente, "/Estudios?Texto=control%20anual&Pagina=2");

        Assert.Contains("Se encontraron 26 estudios.", segunda);
        Assert.DoesNotContain("Otro estudio", segunda);
    }

    [Fact(DisplayName = "RF-16: un término sin coincidencias devuelve cero resultados")]
    public async Task UnTerminoSinCoincidencias_DevuelveCero()
    {
        var cliente = await ConEstudiosDeEjemploAsync();

        var html = await BuscarAsync(cliente, "resonancia magnetica");

        Assert.Contains("Se encontraron 0 estudios.", html);
        Assert.Contains("Ningún estudio coincide con la búsqueda.", html);
    }

    private async Task<HttpClient> ConEstudiosDeEjemploAsync()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        await cliente.CrearYObtenerIdAsync(
            "Ecografía abdominal", "2026-03-15",
            profesional: "Dra. Rivas",
            institucion: "Hospital Central",
            descripcion: "control anual de rutina",
            etiquetas: "cardiología, control");

        await cliente.CrearYObtenerIdAsync(
            "Radiografía de tórax", "2025-06-02",
            profesional: "Dr. Molina",
            institucion: "Clinica del Norte",
            descripcion: "seguimiento");

        await cliente.CrearYObtenerIdAsync(
            "Análisis de sangre", "2024-02-20",
            profesional: "Dra. Paz",
            institucion: "Laboratorio Sur");

        return cliente;
    }

    private static async Task<string> BuscarAsync(HttpClient cliente, string termino) =>
        await LeerAsync(cliente, $"/Estudios?Texto={Uri.EscapeDataString(termino)}");

    private static async Task<string> LeerAsync(HttpClient cliente, string ruta)
    {
        var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());
    }

    /// <summary>Cuenta las filas del listado por su enlace al detalle.</summary>
    private static int ContarEstudiosListados(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, @"/Estudios/Detalle/[0-9a-fA-F-]{36}").Count;
}
