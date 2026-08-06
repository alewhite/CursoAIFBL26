using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MiArchivoMedico.Web.Security;

public sealed record ResultadoAlta(bool Exitosa, string? Error)
{
    public static ResultadoAlta Ok() => new(true, null);

    public static ResultadoAlta Falla(string error) => new(false, error);
}

/// <summary>
/// Alta de cuentas. Es un procedimiento administrativo: se invoca desde el arranque de la aplicación,
/// nunca desde una ruta HTTP (RNF-54, AC-50).
/// </summary>
public sealed class ServicioDeCuentas
{
    /// <summary>Tope de cuentas activas de la instalación (RNF-56, AC-62).</summary>
    public const int MaximoDeCuentas = 5;

    private readonly UserManager<UsuarioApp> _usuarios;

    public ServicioDeCuentas(UserManager<UsuarioApp> usuarios) => _usuarios = usuarios;

    /// <summary>
    /// Crea una cuenta si la instalación no alcanzó el tope. El control de cupo no es transaccional:
    /// para una instalación familiar administrada por una sola persona alcanza, porque el alta es
    /// secuencial y manual.
    /// </summary>
    public async Task<ResultadoAlta> CrearCuentaAsync(string nombreDeUsuario, string email, string contrasena)
    {
        if (await _usuarios.Users.CountAsync() >= MaximoDeCuentas)
        {
            return ResultadoAlta.Falla(
                $"Se alcanzó el límite de {MaximoDeCuentas} cuentas: no es posible dar de alta una más.");
        }

        var usuario = new UsuarioApp { UserName = nombreDeUsuario, Email = email };
        var resultado = await _usuarios.CreateAsync(usuario, contrasena);

        return resultado.Succeeded
            ? ResultadoAlta.Ok()
            : ResultadoAlta.Falla(string.Join(" ", resultado.Errors.Select(e => e.Description)));
    }
}
