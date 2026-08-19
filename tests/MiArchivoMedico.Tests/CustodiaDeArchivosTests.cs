using System.Security.Cryptography;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class CustodiaDeArchivosTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-25: al cargar un archivo, el sistema registra su huella SHA-256")]
    public async Task ArchivoCargado_RegistraSuHuella()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var contenido = ArchivosFicticios.PdfValido();
        var esperada = Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant();

        await cliente.CrearEstudioAsync("Estudio con huella", Hoy, archivos:
            [("informe.pdf", contenido, "application/pdf")]);

        var archivo = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0].Archivos[0];
        Assert.Equal(esperada, archivo.Sha256.ToLowerInvariant());
    }

    [Fact(DisplayName = "AC-65: el nombre físico es un GUID y no contiene nada del nombre original")]
    public async Task NombreFisico_EsUnGuid()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con nombre", Hoy, archivos:
            [("informe.pdf", ArchivosFicticios.PdfValido(), "application/pdf")]);

        var fisicos = aplicacion.ArchivosEnAlmacenamiento();
        Assert.Single(fisicos);

        var nombre = Path.GetFileNameWithoutExtension(fisicos[0]);
        Assert.True(Guid.TryParse(nombre, out _), $"El nombre físico debía ser un GUID y fue «{nombre}».");
        Assert.DoesNotContain("informe", fisicos[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-66: el nombre original se sanitiza y conserva su extensión")]
    public async Task NombreOriginal_SeSanitiza()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio con nombre hostil", Hoy, archivos:
            [("../../etc/passwd.pdf", ArchivosFicticios.PdfValido(), "application/pdf")]);

        var archivo = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0].Archivos[0];

        Assert.DoesNotContain("..", archivo.NombreOriginal, StringComparison.Ordinal);
        Assert.DoesNotContain("/", archivo.NombreOriginal, StringComparison.Ordinal);
        Assert.DoesNotContain("\\", archivo.NombreOriginal, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", archivo.NombreOriginal, StringComparison.Ordinal);
        Assert.True(archivo.NombreOriginal.Length <= 255);
    }

    [Fact(DisplayName = "AC-57: en el almacenamiento, los bytes no corresponden al archivo en claro")]
    public async Task ArchivoEnDisco_EstaCifrado()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var contenido = ArchivosFicticios.PdfValido();

        await cliente.CrearEstudioAsync("Estudio cifrado", Hoy, archivos:
            [("informe.pdf", contenido, "application/pdf")]);

        var fisico = Assert.Single(aplicacion.ArchivosEnAlmacenamiento());
        var enDisco = await File.ReadAllBytesAsync(fisico);

        Assert.NotEqual(contenido, enDisco);
        // El encabezado de un PDF en claro empezaría con %PDF-
        Assert.False(enDisco.Length >= 5
            && enDisco[0] == (byte)'%' && enDisco[1] == (byte)'P'
            && enDisco[2] == (byte)'D' && enDisco[3] == (byte)'F',
            "El archivo quedó en claro en el almacenamiento.");
        // [IV 16 bytes][contenido cifrado]: siempre más largo que el original.
        Assert.True(enDisco.Length > contenido.Length);
    }
}
