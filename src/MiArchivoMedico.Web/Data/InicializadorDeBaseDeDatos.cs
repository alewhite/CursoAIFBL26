using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Data;

public static class InicializadorDeBaseDeDatos
{
    /// <summary>Máximo de cuentas activas del MVP (RNF-56).</summary>
    public const int MaximoDeCuentas = 5;

    /// <summary>
    /// Aplica migraciones, reafirma el modo WAL y siembra las cuentas configuradas, omitiendo las que
    /// ya existen. Rechaza el alta que supere el máximo o cuya contraseña no llegue al mínimo.
    /// </summary>
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArchivoMedicoDbContext>();

        await contexto.Database.MigrateAsync();
        await contexto.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

        var cuentas = alcance.ServiceProvider
            .GetRequiredService<IConfiguration>()
            .GetSection("CuentasIniciales")
            .Get<List<CuentaInicial>>() ?? [];

        var administrador = alcance.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

        foreach (var cuenta in cuentas)
        {
            if (string.IsNullOrWhiteSpace(cuenta.NombreDeUsuario)) continue;
            if (await administrador.FindByNameAsync(cuenta.NombreDeUsuario) is not null) continue;

            if (cuenta.Contrasena.Length < CuentaInicial.LargoMinimoDeContrasena)
            {
                throw new InvalidOperationException(
                    $"El alta de una cuenta trae una contraseña de {cuenta.Contrasena.Length} caracteres y " +
                    $"RNF-69 exige al menos {CuentaInicial.LargoMinimoDeContrasena}.");
            }

            if (await administrador.Users.CountAsync() >= MaximoDeCuentas)
            {
                throw new InvalidOperationException(
                    $"Se alcanzó el límite de {MaximoDeCuentas} cuentas activas que fija RNF-56: " +
                    "no se dio de alta ninguna cuenta adicional.");
            }

            var usuario = new Usuario { UserName = cuenta.NombreDeUsuario, Email = cuenta.Email };
            var resultado = await administrador.CreateAsync(usuario, cuenta.Contrasena);
            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo dar de alta una cuenta inicial: " +
                    string.Join("; ", resultado.Errors.Select(e => e.Code)));
            }
        }
    }
}
