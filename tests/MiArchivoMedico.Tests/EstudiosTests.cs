using System.Net;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>Creación, edición y eliminación de estudios (RF-33 a RF-35, RF-10, RF-13, RF-14).</summary>
public class EstudiosTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-09: un estudio con título y fecha válidos queda almacenado y aparece listado")]
    public async Task EstudioValido_QuedaAlmacenadoYAparecEnElListado()
    {
        var cliente = await ClienteAutenticadoAsync();

        var id = await cliente.CrearYObtenerIdAsync("Ecografia abdominal", "2026-01-10");

        var listado = await cliente.GetAsync("/Estudios");
        listado.EnsureSuccessStatusCode();

        Assert.Contains("Ecografia abdominal", await listado.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/Estudios/Detalle/{id}")).StatusCode);
    }

    [Fact(DisplayName = "AC-10: un título vacío no crea el estudio y muestra un error")]
    public async Task TituloVacio_NoCreaElEstudio()
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioAsync(string.Empty);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);   // vuelve al formulario, no redirige
        Assert.Contains(
            "El título es obligatorio.",
            WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
        await AssertSinEstudiosAsync();
    }

    [Fact(DisplayName = "AC-11: una fecha inválida no crea el estudio y muestra un error")]
    public async Task FechaInvalida_NoCreaElEstudio()
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioAsync("Estudio sin fecha", fecha: "no-es-una-fecha");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await AssertSinEstudiosAsync();
    }

    [Fact(DisplayName = "AC-12: un PDF y dos imágenes quedan agrupados en el mismo estudio")]
    public async Task VariosArchivos_QuedanAgrupadosEnElMismoEstudio()
    {
        var cliente = await ClienteAutenticadoAsync();

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio con tres archivos",
            archivos:
            [
                new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf()),
                new ArchivoAEnviar("placa-uno.jpg", "image/jpeg", ArchivosFicticios.Jpg()),
                new ArchivoAEnviar("placa-dos.png", "image/png", ArchivosFicticios.Png()),
            ]);

        var ids = await cliente.ObtenerIdsDeArchivosAsync(id);

        Assert.Equal(3, ids.Length);
    }

    [Theory(DisplayName = "AC-13, AC-82, AC-88, AC-89: los metadatos se almacenan y se ven en el detalle")]
    [InlineData("profesional", "Dra. Rivas Ficticia")]
    [InlineData("institucion", "Hospital Ficticio Central")]
    [InlineData("descripcion", "control anual de rutina")]
    [InlineData("etiquetas", "cardiologia, control anual")]
    public async Task LosMetadatos_SeAlmacenanYSeMuestranEnElDetalle(string campo, string valor)
    {
        var cliente = await ClienteAutenticadoAsync();

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio con metadatos",
            profesional: campo == "profesional" ? valor : null,
            institucion: campo == "institucion" ? valor : null,
            descripcion: campo == "descripcion" ? valor : null,
            etiquetas: campo == "etiquetas" ? valor : null);

        var detalle = await cliente.GetAsync($"/Estudios/Detalle/{id}");
        var html = WebUtility.HtmlDecode(await detalle.Content.ReadAsStringAsync());

        // Las etiquetas se muestran una por una, no como el texto separado por comas que se ingresó.
        foreach (var esperado in valor.Split(',', StringSplitOptions.TrimEntries))
        {
            Assert.Contains(esperado, html);
        }
    }

    [Fact(DisplayName = "AC-14: editar un metadato no altera el hash del archivo")]
    public async Task EditarUnMetadato_NoAlteraElHashDelArchivo()
    {
        var cliente = await ClienteAutenticadoAsync();
        var contenido = ArchivosFicticios.Pdf();

        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio a editar",
            institucion: "Hospital Ficticio Uno",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", contenido)]);

        var hashAntes = await HashAlmacenadoAsync(id);

        var respuesta = await cliente.EditarEstudioAsync(
            id, "Estudio a editar", institucion: "Hospital Ficticio Dos");
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);

        var detalle = await cliente.GetAsync($"/Estudios/Detalle/{id}");
        Assert.Contains(
            "Hospital Ficticio Dos", WebUtility.HtmlDecode(await detalle.Content.ReadAsStringAsync()));
        Assert.Equal(hashAntes, await HashAlmacenadoAsync(id));
        Assert.Equal(ArchivosFicticios.HashDe(contenido), hashAntes);
    }

    [Fact(DisplayName = "AC-17: eliminar pide confirmación antes de ejecutar")]
    public async Task Eliminar_PideConfirmacion()
    {
        var cliente = await ClienteAutenticadoAsync();
        var id = await cliente.CrearYObtenerIdAsync("Estudio a confirmar");

        var confirmacion = await cliente.GetAsync($"/Estudios/Eliminar/{id}");
        confirmacion.EnsureSuccessStatusCode();

        var html = WebUtility.HtmlDecode(await confirmacion.Content.ReadAsStringAsync());
        Assert.Contains("no se puede deshacer", html, StringComparison.OrdinalIgnoreCase);

        // El GET solo pregunta: el estudio sigue estando (AC-18).
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/Estudios/Detalle/{id}")).StatusCode);
    }

    [Fact(DisplayName = "AC-18: cancelar la confirmación deja el estudio y sus archivos disponibles")]
    public async Task CancelarLaConfirmacion_DejaTodoDisponible()
    {
        var cliente = await ClienteAutenticadoAsync();
        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio que sobrevive",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        var idDeArchivo = (await cliente.ObtenerIdsDeArchivosAsync(id)).Single();

        // Se abre la confirmación y no se envía el formulario: equivale a cancelar.
        await cliente.GetAsync($"/Estudios/Eliminar/{id}");

        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/Estudios/Detalle/{id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await cliente.GetAsync($"/Archivos/Descargar/{idDeArchivo}")).StatusCode);
    }

    [Fact(DisplayName = "AC-19: la eliminación confirmada quita el estudio y sus archivos")]
    public async Task EliminacionConfirmada_QuitaElEstudioYSusArchivos()
    {
        var cliente = await ClienteAutenticadoAsync();
        var id = await cliente.CrearYObtenerIdAsync(
            "Estudio a eliminar",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        var idDeArchivo = (await cliente.ObtenerIdsDeArchivosAsync(id)).Single();
        var token = await cliente.ObtenerTokenAsync($"/Estudios/Eliminar/{id}");

        var respuesta = await cliente.PostAsync($"/Estudios/Eliminar/{id}", new FormUrlEncodedContent(
            [new KeyValuePair<string, string>("__RequestVerificationToken", token)]));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound, (await cliente.GetAsync($"/Estudios/Detalle/{id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await cliente.GetAsync($"/Archivos/Descargar/{idDeArchivo}")).StatusCode);

        // El contenido tampoco queda en disco.
        Assert.False(File.Exists(Path.Combine(_app.RutaDeAlmacenamiento, $"{idDeArchivo:N}.bin")));
    }

    [Fact(DisplayName = "AC-28: el listado ordena del más reciente al más antiguo")]
    public async Task ElListado_OrdenaDelMasRecienteAlMasAntiguo()
    {
        var cliente = await ClienteAutenticadoAsync();

        await cliente.CrearYObtenerIdAsync("Estudio antiguo", "2024-03-01");
        await cliente.CrearYObtenerIdAsync("Estudio reciente", "2026-05-20");
        await cliente.CrearYObtenerIdAsync("Estudio intermedio", "2025-07-11");

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        var reciente = html.IndexOf("Estudio reciente", StringComparison.Ordinal);
        var intermedio = html.IndexOf("Estudio intermedio", StringComparison.Ordinal);
        var antiguo = html.IndexOf("Estudio antiguo", StringComparison.Ordinal);

        Assert.True(reciente < intermedio && intermedio < antiguo, "El listado no quedó ordenado.");
    }

    [Fact(DisplayName = "AC-79: el alta se completa en una sola pantalla")]
    public async Task ElAlta_SeCompletaEnUnaSolaPantalla()
    {
        var cliente = await ClienteAutenticadoAsync();

        // Una pantalla de formulario y un envío: título, fecha y archivo entran juntos (RNF-31).
        var formulario = await cliente.GetAsync("/Estudios/Crear");
        formulario.EnsureSuccessStatusCode();

        var respuesta = await cliente.CrearEstudioAsync(
            "Estudio en un paso",
            archivos: [new ArchivoAEnviar("informe.pdf", "application/pdf", ArchivosFicticios.Pdf())]);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
    }

    [Fact(DisplayName = "AC-80: los errores se muestran junto al campo y al archivo que los produjo")]
    public async Task LosErrores_SeMuestranJuntoAlCampoQueLosProdujo()
    {
        var cliente = await ClienteAutenticadoAsync();

        var respuesta = await cliente.CrearEstudioAsync(
            string.Empty,
            archivos: [new ArchivoAEnviar("vacio.pdf", "application/pdf", [])]);

        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        // El error del título viaja en el span de validación del propio campo.
        Assert.Contains("""data-valmsg-for="Titulo""", html);
        Assert.Contains("El título es obligatorio.", html);

        // El del archivo nombra el archivo que lo causó, no es un aviso general.
        Assert.Contains("vacio.pdf: El archivo está vacío.", html);
    }

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);
        return cliente;
    }

    private async Task<string> HashAlmacenadoAsync(Guid idDeEstudio)
    {
        var hash = string.Empty;

        await _app.EnAlcanceAsync(async servicios =>
        {
            var contexto = servicios.GetRequiredService<ArchivoMedicoDbContext>();
            hash = await contexto.Archivos
                .IgnoreQueryFilters()
                .Where(a => a.EstudioId == idDeEstudio)
                .Select(a => a.HashSha256)
                .SingleAsync();
        });

        return hash;
    }

    private async Task AssertSinEstudiosAsync()
    {
        await _app.EnAlcanceAsync(async servicios =>
        {
            var contexto = servicios.GetRequiredService<ArchivoMedicoDbContext>();
            Assert.Equal(0, await contexto.Estudios.IgnoreQueryFilters().CountAsync());
        });
    }
}
