using MiArchivoMedico.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MiArchivoMedico.Web.Data;

public static class InicializadorDeBaseDeDatos
{
    /// <summary>
    /// Aplica las migraciones pendientes, deja la base en modo WAL y da de alta las cuentas declaradas
    /// en la configuración externa (RNF-54, RNF-56).
    /// </summary>
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        using var alcance = servicios.CreateScope();
        var proveedor = alcance.ServiceProvider;

        var contexto = proveedor.GetRequiredService<ArchivoMedicoDbContext>();
        await contexto.Database.MigrateAsync();

        // WAL es persistente: queda grabado en el archivo, pero se reafirma en cada arranque por si la
        // base se restauró desde un respaldo tomado en otro modo.
        await contexto.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

        await SembrarCuentasAsync(proveedor);
    }

    private static async Task SembrarCuentasAsync(IServiceProvider proveedor)
    {
        var configuracion = proveedor.GetRequiredService<IConfiguration>();
        var cuentas = configuracion.GetSection("CuentasIniciales").Get<CuentaInicial[]>() ?? [];
        if (cuentas.Length == 0)
        {
            return;
        }

        var usuarios = proveedor.GetRequiredService<UserManager<UsuarioApp>>();
        var servicioDeCuentas = proveedor.GetRequiredService<ServicioDeCuentas>();
        var registro = proveedor.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(InicializadorDeBaseDeDatos));

        var creadas = 0;
        var rechazadas = 0;

        foreach (var cuenta in cuentas)
        {
            if (await usuarios.FindByNameAsync(cuenta.NombreDeUsuario) is not null)
            {
                continue;
            }

            var resultado = await servicioDeCuentas.CrearCuentaAsync(
                cuenta.NombreDeUsuario, cuenta.Email, cuenta.Contrasena);

            if (resultado.Exitosa)
            {
                creadas++;
            }
            else
            {
                rechazadas++;
            }
        }

        // Solo cantidades: ni nombres de usuario ni motivos que permitan identificar a una persona (RNF-09).
        if (creadas > 0 || rechazadas > 0)
        {
            registro.LogInformation(
                "Alta administrativa de cuentas: {Creadas} creadas, {Rechazadas} rechazadas.",
                creadas, rechazadas);
        }
    }
}
