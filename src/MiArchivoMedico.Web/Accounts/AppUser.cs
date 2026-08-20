using Microsoft.AspNetCore.Identity;

namespace MiArchivoMedico.Web.Accounts;

/// <summary>
/// Cuenta del grupo familiar. No agrega ninguna propiedad propia: las columnas de sesión única y
/// de bloqueo por intentos fallidos pertenecen a FEAT-001c, y las que llegan heredadas
/// (<c>AccessFailedCount</c>, <c>LockoutEnd</c>, <c>SecurityStamp</c>) vienen con el esquema de
/// Identity y no se configuran en este sub-ticket.
/// </summary>
public sealed class AppUser : IdentityUser;
