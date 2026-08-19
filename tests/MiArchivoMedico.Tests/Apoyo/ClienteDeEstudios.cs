namespace MiArchivoMedico.Tests.Apoyo;

/// <summary>Extensiones de HttpClient para las operaciones sobre estudios, con el antiforgery resuelto.</summary>
public static class ClienteDeEstudios
{
    public static Task<HttpResponseMessage> CrearEstudioAsync(
        this HttpClient cliente,
        string titulo,
        DateOnly fecha,
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        string? etiquetas = null,
        IEnumerable<(string Nombre, byte[] Contenido, string TipoDeContenido)>? archivos = null) =>
        cliente.CrearEstudioConFechaCrudaAsync(
            titulo, fecha.ToString("yyyy-MM-dd"), profesional, institucion, descripcion, etiquetas, archivos);

    /// <summary>Permite enviar una fecha con formato arbitrario, para ejercitar el rechazo de AC-11.</summary>
    public static async Task<HttpResponseMessage> CrearEstudioConFechaCrudaAsync(
        this HttpClient cliente,
        string titulo,
        string fecha,
        string? profesional = null,
        string? institucion = null,
        string? descripcion = null,
        string? etiquetas = null,
        IEnumerable<(string Nombre, byte[] Contenido, string TipoDeContenido)>? archivos = null)
    {
        var formulario = await cliente.GetAsync("/Estudios/Crear");
        var html = await formulario.Content.ReadAsStringAsync();
        var token = ClienteDeSesion.ExtraerTokenAntifalsificacion(html);
        var marca = ExtraerMarcaDeEnvio(html);

        var contenido = new MultipartFormDataContent
        {
            { new StringContent(titulo), "Titulo" },
            { new StringContent(fecha), "Fecha" },
        };
        if (profesional is not null) contenido.Add(new StringContent(profesional), "Profesional");
        if (institucion is not null) contenido.Add(new StringContent(institucion), "Institucion");
        if (descripcion is not null) contenido.Add(new StringContent(descripcion), "Descripcion");
        if (etiquetas is not null) contenido.Add(new StringContent(etiquetas), "Etiquetas");
        if (token is not null) contenido.Add(new StringContent(token), "__RequestVerificationToken");
        if (marca is not null) contenido.Add(new StringContent(marca), "MarcaDeEnvio");

        foreach (var (nombre, bytes, tipo) in archivos ?? [])
        {
            var parte = new ByteArrayContent(bytes);
            parte.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipo);
            contenido.Add(parte, "Archivos", nombre);
        }

        return await cliente.PostAsync("/Estudios/Crear", contenido);
    }

    /// <summary>Reenvía exactamente el mismo formulario, para ejercitar el control de envío único.</summary>
    public static async Task<HttpResponseMessage> ReenviarCreacionAsync(
        this HttpClient cliente, string titulo, DateOnly fecha, string? token, string? marca)
    {
        var contenido = new MultipartFormDataContent
        {
            { new StringContent(titulo), "Titulo" },
            { new StringContent(fecha.ToString("yyyy-MM-dd")), "Fecha" },
        };
        if (token is not null) contenido.Add(new StringContent(token), "__RequestVerificationToken");
        if (marca is not null) contenido.Add(new StringContent(marca), "MarcaDeEnvio");

        return await cliente.PostAsync("/Estudios/Crear", contenido);
    }

    public static string? ExtraerMarcaDeEnvio(string html) =>
        System.Text.RegularExpressions.Regex.Match(html, """name="MarcaDeEnvio"[^>]*value="(?<valor>[^"]+)""")
            is { Success: true } m ? m.Groups["valor"].Value : null;

    public static async Task<HttpResponseMessage> AgregarArchivosAsync(
        this HttpClient cliente, Guid estudioId,
        IEnumerable<(string Nombre, byte[] Contenido, string TipoDeContenido)> archivos)
    {
        var detalle = await cliente.GetAsync($"/Estudios/Detalle/{estudioId}");
        var token = ClienteDeSesion.ExtraerTokenAntifalsificacion(await detalle.Content.ReadAsStringAsync());

        var contenido = new MultipartFormDataContent();
        if (token is not null) contenido.Add(new StringContent(token), "__RequestVerificationToken");

        foreach (var (nombre, bytes, tipo) in archivos)
        {
            var parte = new ByteArrayContent(bytes);
            parte.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipo);
            contenido.Add(parte, "Archivos", nombre);
        }

        return await cliente.PostAsync($"/Estudios/AgregarArchivos/{estudioId}", contenido);
    }

    public static async Task<HttpResponseMessage> EliminarEstudioAsync(this HttpClient cliente, Guid id)
    {
        var confirmacion = await cliente.GetAsync($"/Estudios/Eliminar/{id}");
        var token = ClienteDeSesion.ExtraerTokenAntifalsificacion(await confirmacion.Content.ReadAsStringAsync());

        var campos = new Dictionary<string, string>();
        if (token is not null) campos["__RequestVerificationToken"] = token;

        return await cliente.PostAsync($"/Estudios/Eliminar/{id}", new FormUrlEncodedContent(campos));
    }

    public static async Task<HttpClient> ClienteAutenticadoAsync(
        this AplicacionDePrueba aplicacion, string? usuario = null)
    {
        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(usuario ?? AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);
        return cliente;
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
