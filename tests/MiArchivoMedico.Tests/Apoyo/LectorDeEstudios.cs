using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;
using MiArchivoMedico.Web.Servicios;

namespace MiArchivoMedico.Tests.Apoyo;

/// <summary>
/// Lee el estado persistido con el usuario actual fijado a una cuenta, para poder comprobar en la base
/// lo que la aplicación guardó sin romper el aislamiento que se está verificando.
/// </summary>
public static class LectorDeEstudios
{
    public static async Task<List<Estudio>> EstudiosDeAsync(this AplicacionDePrueba aplicacion, string nombreDeUsuario)
    {
        using var contexto = await AbrirAsync(aplicacion, nombreDeUsuario);
        return await contexto.Estudios
            .Include(e => e.Archivos)
            .Include(e => e.Etiquetas)
            .OrderByDescending(e => e.CreadoEn)
            .ToListAsync();
    }

    public static async Task<ArchivoMedicoDbContext> AbrirAsync(
        this AplicacionDePrueba aplicacion, string nombreDeUsuario)
    {
        var alcance = aplicacion.Services.CreateScope();
        var administrador = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var usuario = await administrador.FindByNameAsync(nombreDeUsuario)
            ?? throw new InvalidOperationException($"No existe la cuenta {nombreDeUsuario}.");

        var opciones = alcance.ServiceProvider.GetRequiredService<DbContextOptions<ArchivoMedicoDbContext>>();
        var reloj = alcance.ServiceProvider.GetRequiredService<TimeProvider>();
        return new ArchivoMedicoDbContext(opciones, new UsuarioActualFijo(usuario.Id), reloj);
    }

    public static string[] ArchivosEnAlmacenamiento(this AplicacionDePrueba aplicacion) =>
        Directory.Exists(aplicacion.RutaDeAlmacenamiento)
            ? Directory.GetFiles(aplicacion.RutaDeAlmacenamiento, "*", SearchOption.TopDirectoryOnly)
            : [];
}
