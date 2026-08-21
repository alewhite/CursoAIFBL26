using MiArchivoMedico.Web.Accounts;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MiArchivoMedico.Web.Data;

/// <summary>
/// Contexto de persistencia de la aplicación. Por ahora solo aporta el esquema de ASP.NET Core
/// Identity; las entidades del dominio llegan con las features siguientes.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> opciones)
    : IdentityDbContext<AppUser>(opciones);
