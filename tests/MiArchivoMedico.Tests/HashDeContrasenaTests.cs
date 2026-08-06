using System.Buffers.Binary;
using MiArchivoMedico.Tests.Infraestructura;
using MiArchivoMedico.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MiArchivoMedico.Tests;

public class HashDeContrasenaTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public Task InitializeAsync() => _app.InitializeAsync();

    public Task DisposeAsync() => _app.DisposeAsync();

    [Fact(DisplayName = "AC-76: la contraseña se guarda con PBKDF2-HMAC-SHA256 y ≥ 100.000 iteraciones")]
    public async Task LaContrasenaSeGuardaConPbkdf2HmacSha256YSuficientesIteraciones()
    {
        await _app.EnAlcanceAsync(async servicios =>
        {
            var usuarios = servicios.GetRequiredService<UserManager<UsuarioApp>>();
            var usuario = await usuarios.FindByNameAsync(AplicacionDePrueba.Usuario);

            var almacenado = usuario!.PasswordHash!;
            Assert.NotEqual(AplicacionDePrueba.Contrasena, almacenado);

            // Formato de Identity V3: [0x01][prf BE][iteraciones BE][largo de sal BE][sal][subclave].
            var bytes = Convert.FromBase64String(almacenado);
            Assert.Equal((byte)0x01, bytes[0]);

            var prf = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(1, 4));
            Assert.Equal(1u, prf);   // 1 = HMAC-SHA256

            var iteraciones = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4));
            Assert.True(
                iteraciones >= 100_000,
                $"RNF-03 exige al menos 100.000 iteraciones; se configuraron {iteraciones}.");
        });
    }
}
