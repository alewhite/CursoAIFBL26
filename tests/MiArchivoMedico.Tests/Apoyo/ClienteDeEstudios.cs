namespace MiArchivoMedico.Tests.Apoyo;

/// <summary>Extensiones de HttpClient para las operaciones sobre estudios, con el antiforgery resuelto.</summary>
public static class ClienteDeEstudios
{
    public static async Task<HttpResponseMessage> CrearEstudioAsync(
        this HttpClient cliente,
        string titulo,
        DateOnly fecha,
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        IEnumerable<(string Nombre, byte[] Contenido, string TipoDeContenido)>? archivos = null)
    {
        var formulario = await cliente.GetAsync("/Estudios/Crear");
        var token = ClienteDeSesion.ExtraerTokenAntifalsificacion(await formulario.Content.ReadAsStringAsync());

        var contenido = new MultipartFormDataContent
        {
            { new StringContent(titulo), "Titulo" },
            { new StringContent(fecha.ToString("yyyy-MM-dd")), "Fecha" },
        };
        if (profesional is not null) contenido.Add(new StringContent(profesional), "Profesional");
        if (institucion is not null) contenido.Add(new StringContent(institucion), "Institucion");
        if (descripcion is not null) contenido.Add(new StringContent(descripcion), "Descripcion");
        if (token is not null) contenido.Add(new StringContent(token), "__RequestVerificationToken");

        foreach (var (nombre, bytes, tipo) in archivos ?? [])
        {
            var parte = new ByteArrayContent(bytes);
            parte.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipo);
            contenido.Add(parte, "Archivos", nombre);
        }

        return await cliente.PostAsync("/Estudios/Crear", contenido);
    }

    public static async Task<HttpResponseMessage> BuscarAsync(
        this HttpClient cliente, string? termino = null, string? institucion = null)
    {
        var listado = await cliente.GetAsync("/Estudios");
        var token = ClienteDeSesion.ExtraerTokenAntifalsificacion(await listado.Content.ReadAsStringAsync());

        var campos = new Dictionary<string, string>();
        if (termino is not null) campos["Termino"] = termino;
        if (institucion is not null) campos["Institucion"] = institucion;
        if (token is not null) campos["__RequestVerificationToken"] = token;

        return await cliente.PostAsync("/Estudios/Buscar", new FormUrlEncodedContent(campos));
    }
}
