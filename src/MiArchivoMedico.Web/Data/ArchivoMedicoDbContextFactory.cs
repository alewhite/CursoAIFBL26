using MiArchivoMedico.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Contexto para las herramientas de diseño (<c>dotnet ef migrations</c>). Usa una ruta descartable:
/// generar una migración no debe requerir la cadena de conexión real ni tocar la base de producción.
/// </summary>
public sealed class ArchivoMedicoDbContextFactory : IDesignTimeDbContextFactory<ArchivoMedicoDbContext>
{
    public ArchivoMedicoDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<ArchivoMedicoDbContext>()
            .UseSqlite("Data Source=migraciones-en-diseno.db")
            .Options;

        return new ArchivoMedicoDbContext(opciones, new UsuarioDeDiseno());
    }

    private sealed class UsuarioDeDiseno : IUsuarioActual
    {
        public string? Id => null;
    }
}
