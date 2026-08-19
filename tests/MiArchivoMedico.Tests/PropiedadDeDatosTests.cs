using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Tests.Apoyo;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MiArchivoMedico.Tests;

/// <summary>
/// El requisito crítico del sistema: cinco cuentas comparten la instalación y nunca los datos
/// (RNF-53). Un identificador ajeno responde 404, indistinguible de uno inexistente.
/// </summary>
public class PropiedadDeDatosTests(AplicacionDePrueba aplicacion) : IClassFixture<AplicacionDePrueba>
{
    [Fact(DisplayName = "AC-47: el detalle de un estudio de otro propietario responde 404")]
    public async Task DetalleAjeno_Responde404()
    {
        var estudioDeBruno = await SembrarEstudioAsync(AplicacionDePrueba.UsuarioDos, "Ecografia de Bruno");

        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        var respuesta = await cliente.GetAsync($"/Estudios/Detalle/{estudioDeBruno}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain("Bruno", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-47: un identificador ajeno y uno inexistente son indistinguibles")]
    public async Task AjenoEInexistente_SonIndistinguibles()
    {
        var estudioDeBruno = await SembrarEstudioAsync(AplicacionDePrueba.UsuarioDos, "Otro de Bruno");

        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        var ajeno = await cliente.GetAsync($"/Estudios/Detalle/{estudioDeBruno}");
        var inexistente = await cliente.GetAsync($"/Estudios/Detalle/{Guid.NewGuid()}");

        Assert.Equal(ajeno.StatusCode, inexistente.StatusCode);
        Assert.Equal(
            await ajeno.Content.ReadAsStringAsync(),
            await inexistente.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "AC-49: el listado muestra solo los estudios propios")]
    public async Task Listado_MuestraSoloLosPropios()
    {
        await SembrarEstudioAsync(AplicacionDePrueba.UsuarioUno, "Analisis de Ana");
        await SembrarEstudioAsync(AplicacionDePrueba.UsuarioDos, "Radiografia de Bruno");

        var cliente = aplicacion.CrearClienteSinRedirecciones();
        await cliente.IniciarSesionAsync(AplicacionDePrueba.UsuarioUno, AplicacionDePrueba.Contrasena);

        var html = await (await cliente.GetAsync("/Estudios")).Content.ReadAsStringAsync();

        Assert.Contains("Analisis de Ana", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Radiografia de Bruno", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Siembra directamente por el contexto, con el usuario actual fijado a la cuenta indicada. Es el
    /// único camino admitido para construir el estado de partida sin romper el aislamiento que se está
    /// verificando.
    /// </summary>
    private async Task<Guid> SembrarEstudioAsync(string nombreDeUsuario, string titulo)
    {
        using var alcance = aplicacion.Services.CreateScope();
        var administrador = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = await administrador.FindByNameAsync(nombreDeUsuario)
            ?? throw new InvalidOperationException($"No existe la cuenta de prueba {nombreDeUsuario}.");

        var opciones = alcance.ServiceProvider.GetRequiredService<DbContextOptions<ArchivoMedicoDbContext>>();
        var reloj = alcance.ServiceProvider.GetRequiredService<TimeProvider>();
        using var contexto = new ArchivoMedicoDbContext(opciones, new UsuarioActualFijo(usuario.Id), reloj);

        var estudio = new Estudio
        {
            Titulo = titulo,
            Fecha = DateOnly.FromDateTime(reloj.GetUtcNow().UtcDateTime),
        };
        contexto.Estudios.Add(estudio);
        await contexto.SaveChangesAsync();
        return estudio.Id;
    }
}
