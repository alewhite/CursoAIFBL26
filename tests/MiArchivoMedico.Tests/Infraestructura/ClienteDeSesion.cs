using System.Net;
using System.Text.RegularExpressions;

namespace MiArchivoMedico.Tests.Infraestructura;

/// <summary>Operaciones de sesión sobre el formulario real, incluido el token antifalsificación.</summary>
public static partial class ClienteDeSesion
{
    /// <summary>Página que exige sesión. La raíz no sirve: redirige al listado y devuelve 302.</summary>
    private const string RutaProtegida = "/Estudios";

    public static async Task<HttpResponseMessage> IniciarSesionAsync(
        this HttpClient cliente, string usuario, string contrasena)
    {
        var formulario = await cliente.GetAsync("/Cuenta/Login");
        formulario.EnsureSuccessStatusCode();
        var token = ExtraerToken(await formulario.Content.ReadAsStringAsync());

        return await cliente.PostAsync("/Cuenta/Login", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("NombreDeUsuario", usuario),
            new KeyValuePair<string, string>("Contrasena", contrasena),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        ]));
    }

    public static async Task<HttpResponseMessage> CerrarSesionAsync(this HttpClient cliente)
    {
        var pagina = await cliente.GetAsync(RutaProtegida);
        pagina.EnsureSuccessStatusCode();
        var token = ExtraerToken(await pagina.Content.ReadAsStringAsync());

        return await cliente.PostAsync("/Cuenta/Logout", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        ]));
    }

    /// <summary>Una sesión válida devuelve la página; una inválida redirige al inicio de sesión.</summary>
    public static async Task<bool> TieneSesionValidaAsync(this HttpClient cliente)
    {
        var respuesta = await cliente.GetAsync(RutaProtegida);
        return respuesta.StatusCode == HttpStatusCode.OK;
    }

    private static string ExtraerToken(string html)
    {
        var coincidencia = TokenAntifalsificacion().Match(html);
        Assert.True(coincidencia.Success, "El formulario no incluyó el token antifalsificación.");
        return coincidencia.Groups["valor"].Value;
    }

    [GeneratedRegex(
        """<input name="__RequestVerificationToken" type="hidden" value="(?<valor>[^"]+)" />""")]
    private static partial Regex TokenAntifalsificacion();
}
