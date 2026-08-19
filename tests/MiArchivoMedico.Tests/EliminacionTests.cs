using System.Net;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class EliminacionTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-17: antes de eliminar, el sistema pide confirmación y avisa que es irreversible")]
    public async Task Eliminar_PideConfirmacion()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var estudio = await SembrarAsync(aplicacion, cliente);

        var confirmacion = await cliente.GetAsync($"/Estudios/Eliminar/{estudio}");
        var html = await confirmacion.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, confirmacion.StatusCode);
        Assert.Contains("irreversible", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", html, StringComparison.Ordinal);   // cuántos archivos alcanza
        Assert.Single(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
    }

    [Fact(DisplayName = "AC-18: al cancelar la confirmación, el estudio y sus archivos siguen disponibles")]
    public async Task Cancelar_NoEliminaNada()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var estudio = await SembrarAsync(aplicacion, cliente);

        await cliente.GetAsync($"/Estudios/Eliminar/{estudio}");   // se abre la confirmación y no se envía

        Assert.Single(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
        Assert.Single(aplicacion.ArchivosEnAlmacenamiento());
    }

    [Fact(DisplayName = "AC-19: confirmada la eliminación, el estudio y sus archivos dejan de estar disponibles")]
    public async Task Confirmar_EliminaEstudioYArchivos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();
        var estudio = await SembrarAsync(aplicacion, cliente);

        await cliente.EliminarEstudioAsync(estudio);

        Assert.Empty(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno));
        Assert.Empty(aplicacion.ArchivosEnAlmacenamiento());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/Estudios/Detalle/{estudio}")).StatusCode);
    }

    [Fact(DisplayName = "AC-102: eliminado un estudio, el espacio vuelve al cupo compartido")]
    public async Task Eliminar_LiberaElCupo()
    {
        using var aplicacion = new AplicacionDePruebaConCupo(6_000);
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        var pdf = new byte[3_000];
        ArchivosFicticios.PdfValido().CopyTo(pdf, 0);
        Array.Copy("\n%%EOF\n"u8.ToArray(), 0, pdf, pdf.Length - 7, 7);

        await cliente.CrearEstudioAsync("Ocupa casi todo", Hoy, archivos: [("uno.pdf", pdf, "application/pdf")]);
        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Single(estudio.Archivos);

        // Con el cupo casi lleno, otro archivo del mismo tamaño no entra.
        await cliente.CrearEstudioAsync("No entra", Hoy, archivos: [("dos.pdf", pdf, "application/pdf")]);
        Assert.Single(aplicacion.ArchivosEnAlmacenamiento());

        await cliente.EliminarEstudioAsync(estudio.Id);

        // Liberado el espacio, la misma carga se completa.
        await cliente.CrearEstudioAsync("Ahora sí entra", Hoy, archivos: [("tres.pdf", pdf, "application/pdf")]);
        Assert.Single(aplicacion.ArchivosEnAlmacenamiento());
    }

    [Fact(DisplayName = "RNF-53: no se puede eliminar el estudio de otro propietario")]
    public async Task EstudioAjeno_NoSePuedeEliminar()
    {
        using var aplicacion = new AplicacionDePrueba();
        var deBruno = await aplicacion.ClienteAutenticadoAsync(AplicacionDePrueba.UsuarioDos);
        var estudio = await SembrarAsync(aplicacion, deBruno, AplicacionDePrueba.UsuarioDos);

        var deAna = await aplicacion.ClienteAutenticadoAsync();
        var respuesta = await deAna.GetAsync($"/Estudios/Eliminar/{estudio}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Single(await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioDos));
    }

    private static async Task<Guid> SembrarAsync(
        AplicacionDePrueba aplicacion, HttpClient cliente, string? usuario = null)
    {
        await cliente.CrearEstudioAsync("Estudio para eliminar", Hoy, archivos:
            [("informe.pdf", ArchivosFicticios.PdfValido(), "application/pdf")]);

        return (await aplicacion.EstudiosDeAsync(usuario ?? AplicacionDePrueba.UsuarioUno))[0].Id;
    }
}
