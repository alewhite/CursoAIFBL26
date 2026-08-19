using Microsoft.AspNetCore.Hosting;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class LimitesDeCargaTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-70: con 20 archivos en un estudio, la carga del siguiente se rechaza")]
    public async Task VeinteArchivos_RechazaElSiguiente()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var veinte = Enumerable.Range(1, 20)
            .Select(i => ($"informe{i}.pdf", ArchivosFicticios.PdfValido(), "application/pdf"))
            .ToArray();

        await cliente.CrearEstudioAsync("Estudio lleno", Hoy, archivos: veinte);
        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Equal(20, estudio.Archivos.Count);

        await cliente.AgregarArchivosAsync(estudio.Id,
            [("uno-mas.pdf", ArchivosFicticios.PdfValido(), "application/pdf")]);

        var despues = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Equal(20, despues.Archivos.Count);

        // El aviso se muestra en la pantalla a la que llega el usuario, no en el cuerpo de la
        // redirección, que va vacío.
        var detalle = await (await cliente.GetAsync($"/Estudios/Detalle/{estudio.Id}")).Content.ReadAsStringAsync();
        Assert.Contains("límite", detalle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-55, AC-97: una carga que superaría el cupo se rechaza antes de escribir")]
    public async Task CargaQueSuperaElCupo_SeRechaza()
    {
        using var aplicacion = new AplicacionDePruebaConCupo(2_000);
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var grande = new byte[4_000];
        ArchivosFicticios.PdfValido().CopyTo(grande, 0);
        Array.Copy("\n%%EOF\n"u8.ToArray(), 0, grande, grande.Length - 7, 7);

        var respuesta = await cliente.CrearEstudioAsync("Estudio que no entra", Hoy, archivos:
            [("grande.pdf", grande, "application/pdf")]);

        Assert.Empty(aplicacion.ArchivosEnAlmacenamiento());
        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("almacenamiento", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-64: el aviso de cupo no revela qué cuenta consumió el espacio")]
    public async Task AvisoDeCupo_NoRevelaOtrasCuentas()
    {
        using var aplicacion = new AplicacionDePruebaConCupo(2_000);
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var grande = new byte[4_000];
        ArchivosFicticios.PdfValido().CopyTo(grande, 0);

        var html = await (await cliente.CrearEstudioAsync("Sin espacio", Hoy, archivos:
            [("grande.pdf", grande, "application/pdf")])).Content.ReadAsStringAsync();

        Assert.DoesNotContain(AplicacionDePrueba.UsuarioDos, html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bruno", html, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Fábrica con un cupo diminuto, para alcanzar el límite sin cargar 20 GB.</summary>
public sealed class AplicacionDePruebaConCupo(long cupoEnBytes) : AplicacionDePrueba
{
    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        base.ConfigureWebHost(constructor);
        constructor.UseSetting("Almacenamiento:CupoTotalEnBytes", cupoEnBytes.ToString());
    }
}
