using System.Net.Http.Headers;

namespace MiArchivoMedico.Tests.Infraestructura;

public sealed record ArchivoAEnviar(string Nombre, string TipoMime, byte[] Contenido);

/// <summary>Alta y edición de estudios a través del formulario real, con token antifalsificación.</summary>
public static class ClienteDeEstudios
{
    public static Task<HttpResponseMessage> CrearEstudioAsync(
        this HttpClient cliente,
        string titulo,
        string fecha = "2026-01-10",
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        string? etiquetas = null,
        params ArchivoAEnviar[] archivos) =>
        cliente.EnviarFormularioAsync(
            "/Estudios/Crear", "/Estudios/Crear",
            titulo, fecha, profesional, institucion, descripcion, etiquetas, archivos);

    public static Task<HttpResponseMessage> EditarEstudioAsync(
        this HttpClient cliente,
        Guid id,
        string titulo,
        string fecha = "2026-01-10",
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        string? etiquetas = null,
        params ArchivoAEnviar[] archivos) =>
        cliente.EnviarFormularioAsync(
            $"/Estudios/Editar/{id}", $"/Estudios/Editar/{id}",
            titulo, fecha, profesional, institucion, descripcion, etiquetas, archivos);

    /// <summary>Crea un estudio y devuelve su identificador, tomado de la redirección al detalle.</summary>
    public static async Task<Guid> CrearYObtenerIdAsync(
        this HttpClient cliente,
        string titulo,
        string fecha = "2026-01-10",
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        string? etiquetas = null,
        params ArchivoAEnviar[] archivos)
    {
        var respuesta = await cliente.CrearEstudioAsync(
            titulo, fecha, profesional, institucion, descripcion, etiquetas, archivos);

        Assert.True(
            respuesta.Headers.Location is not null,
            $"El alta no redirigió: respondió {(int)respuesta.StatusCode}.");

        var destino = respuesta.Headers.Location!.OriginalString;
        return Guid.Parse(destino[(destino.LastIndexOf('/') + 1)..]);
    }

    public static async Task<Guid[]> ObtenerIdsDeArchivosAsync(this HttpClient cliente, Guid idDeEstudio)
    {
        var detalle = await cliente.GetAsync($"/Estudios/Detalle/{idDeEstudio}");
        detalle.EnsureSuccessStatusCode();
        var html = await detalle.Content.ReadAsStringAsync();

        return System.Text.RegularExpressions.Regex
            .Matches(html, @"/Archivos/Descargar/(?<id>[0-9a-fA-F-]{36})")
            .Select(m => Guid.Parse(m.Groups["id"].Value))
            .Distinct()
            .ToArray();
    }

    public static async Task<string> ObtenerTokenAsync(this HttpClient cliente, string rutaDelFormulario)
    {
        var pagina = await cliente.GetAsync(rutaDelFormulario);
        pagina.EnsureSuccessStatusCode();
        var html = await pagina.Content.ReadAsStringAsync();

        var coincidencia = System.Text.RegularExpressions.Regex.Match(
            html, """<input name="__RequestVerificationToken" type="hidden" value="(?<valor>[^"]+)" />""");

        Assert.True(coincidencia.Success, "El formulario no incluyó el token antifalsificación.");
        return coincidencia.Groups["valor"].Value;
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        this HttpClient cliente,
        string rutaDelFormulario,
        string rutaDeEnvio,
        string titulo,
        string fecha,
        string? profesional,
        string? institucion,
        string? descripcion,
        string? etiquetas,
        ArchivoAEnviar[] archivos)
    {
        var token = await cliente.ObtenerTokenAsync(rutaDelFormulario);

        var contenido = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(titulo), "Titulo" },
            { new StringContent(fecha), "Fecha" },
            { new StringContent(profesional ?? string.Empty), "Profesional" },
            { new StringContent(institucion ?? string.Empty), "Institucion" },
            { new StringContent(descripcion ?? string.Empty), "Descripcion" },
            { new StringContent(etiquetas ?? string.Empty), "Etiquetas" },
        };

        foreach (var archivo in archivos)
        {
            var parte = new ByteArrayContent(archivo.Contenido);
            parte.Headers.ContentType = new MediaTypeHeaderValue(archivo.TipoMime);
            contenido.Add(parte, "Archivos", archivo.Nombre);
        }

        return await cliente.PostAsync(rutaDeEnvio, contenido);
    }
}
