using System.Net;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Security;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

/// <summary>El alta de cuentas es administrativa y acotada a 5 (RNF-54, RNF-56).</summary>
public class AltaDeCuentasTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Theory(DisplayName = "AC-50: no existe ninguna ruta de registro de cuentas")]
    [InlineData("/Cuenta/Registro")]
    [InlineData("/Cuenta/Register")]
    [InlineData("/Identity/Account/Register")]
    [InlineData("/Account/Register")]
    public async Task NoExisteNingunaRutaDeRegistro(string ruta)
    {
        // Se consulta con sesión iniciada: sin ella, la política global respondería una redirección al
        // inicio de sesión y ocultaría si la ruta existe o no.
        var cliente = _app.CrearCliente();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.Usuario, AplicacionDePrueba.Contrasena);

        var porGet = await cliente.GetAsync(ruta);
        var porPost = await cliente.PostAsync(ruta, new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.NotFound, porGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, porPost.StatusCode);
    }

    [Fact(DisplayName = "AC-62: con 5 cuentas activas, el alta de una sexta se rechaza")]
    public async Task ConCincoCuentas_ElAltaDeUnaSextaSeRechaza()
    {
        _app.CuentasIniciales.AddRange(
        [
            ("paciente.tres", "paciente.tres@ejemplo.invalid", "Ficticia-2026-tres"),
            ("paciente.cuatro", "paciente.cuatro@ejemplo.invalid", "Ficticia-2026-cuatro"),
            ("paciente.cinco", "paciente.cinco@ejemplo.invalid", "Ficticia-2026-cinco"),
        ]);

        await _app.EnAlcanceAsync(async servicios =>
        {
            var cuentas = servicios.GetRequiredService<ServicioDeCuentas>();

            var resultado = await cuentas.CrearCuentaAsync(
                "paciente.seis", "paciente.seis@ejemplo.invalid", "Ficticia-2026-seis");

            Assert.False(resultado.Exitosa);
            Assert.Contains("límite de 5 cuentas", resultado.Error);
        });
    }
}
