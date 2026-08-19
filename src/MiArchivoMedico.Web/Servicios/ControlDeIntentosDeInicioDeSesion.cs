using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Data;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Web.Servicios;

/// <summary>
/// Bloqueo por fuerza bruta (RNF-60, RNF-65). Cinco fallos dentro de una ventana de 15 minutos
/// bloquean durante 15 minutos contados **desde el quinto fallo**, así que un intento hecho durante el
/// bloqueo no extiende la ventana (AC-86). Un ingreso exitoso reinicia el contador.
/// </summary>
public sealed class ControlDeIntentosDeInicioDeSesion(ArchivoMedicoDbContext contexto, TimeProvider reloj)
{
    public const int FallosParaBloquear = 5;
    public static readonly TimeSpan Ventana = TimeSpan.FromMinutes(15);

    public async Task<bool> EstaBloqueadoAsync(string nombreDeUsuario)
    {
        var registro = await BuscarAsync(nombreDeUsuario);
        if (registro is null) return false;

        var vencido = reloj.GetUtcNow() - registro.UltimoFalloEn >= Ventana;
        return registro.Fallos >= FallosParaBloquear && !vencido;
    }

    public async Task RegistrarFalloAsync(string nombreDeUsuario)
    {
        var ahora = reloj.GetUtcNow();
        var registro = await BuscarAsync(nombreDeUsuario);

        if (registro is null)
        {
            contexto.Intentos.Add(new IntentoDeInicioDeSesion
            {
                NombreDeUsuarioNormalizado = Normalizar(nombreDeUsuario),
                Fallos = 1,
                UltimoFalloEn = ahora,
            });
        }
        else if (ahora - registro.UltimoFalloEn >= Ventana)
        {
            // La ventana anterior venció: empieza una cuenta nueva.
            registro.Fallos = 1;
            registro.UltimoFalloEn = ahora;
        }
        else if (registro.Fallos < FallosParaBloquear)
        {
            registro.Fallos++;
            registro.UltimoFalloEn = ahora;
        }
        // Alcanzado el tope, un intento más no mueve la marca: los 15 minutos corren desde el quinto
        // fallo y no se reinician con cada golpe (AC-86).

        await contexto.SaveChangesAsync();
        await DepurarVencidosAsync(ahora);
    }

    public async Task ReiniciarAsync(string nombreDeUsuario)
    {
        var registro = await BuscarAsync(nombreDeUsuario);
        if (registro is null) return;

        contexto.Intentos.Remove(registro);
        await contexto.SaveChangesAsync();
    }

    /// <summary>
    /// El contador no está acotado a las cuentas existentes, así que probar nombres inventados podría
    /// hacerlo crecer. Las entradas cuya ventana ya venció se eliminan.
    /// </summary>
    private async Task DepurarVencidosAsync(DateTimeOffset ahora)
    {
        var limite = ahora - Ventana;
        var vencidos = await contexto.Intentos.Where(i => i.UltimoFalloEn < limite).ToListAsync();
        if (vencidos.Count == 0) return;

        contexto.Intentos.RemoveRange(vencidos);
        await contexto.SaveChangesAsync();
    }

    private Task<IntentoDeInicioDeSesion?> BuscarAsync(string nombreDeUsuario)
    {
        var normalizado = Normalizar(nombreDeUsuario);
        return contexto.Intentos.FirstOrDefaultAsync(i => i.NombreDeUsuarioNormalizado == normalizado);
    }

    private static string Normalizar(string nombreDeUsuario) =>
        NormalizadorDeTexto.Normalizar(nombreDeUsuario);
}
