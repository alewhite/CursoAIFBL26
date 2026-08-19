using System.Net;
using MiArchivoMedico.Tests.Apoyo;

namespace MiArchivoMedico.Tests;

public class EdicionDeEstudioTests
{
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

    [Fact(DisplayName = "AC-14: al modificar la institución, el metadato cambia y la huella del archivo no")]
    public async Task EditarInstitucion_NoAlteraElArchivo()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Estudio para editar", Hoy, institucion: "Hospital Central", archivos:
            [("informe.pdf", ArchivosFicticios.PdfValido(), "application/pdf")]);

        var antes = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        var huellaPrevia = antes.Archivos[0].Sha256;

        await cliente.EditarEstudioAsync(antes.Id, "Estudio para editar", Hoy, institucion: "Clínica del Sur");

        var despues = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Equal("Clínica del Sur", despues.Institucion);
        Assert.Equal(huellaPrevia, despues.Archivos[0].Sha256);
        Assert.Single(aplicacion.ArchivosEnAlmacenamiento());
    }

    [Fact(DisplayName = "RF-10: al editar, las columnas normalizadas se recalculan solas")]
    public async Task Editar_RecalculaLasColumnasNormalizadas()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Título viejo", Hoy);
        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];

        await cliente.EditarEstudioAsync(estudio.Id, "Ecografía Abdominal", Hoy);

        var despues = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Equal("ecografia abdominal", despues.TituloNormalizado);

        // Y por lo tanto la búsqueda lo encuentra por el título nuevo.
        var html = await (await cliente.BuscarAsync("abdominal")).Content.ReadAsStringAsync();
        Assert.Contains("Ecografía Abdominal", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "RNF-70: la edición respeta los mismos largos máximos que el alta")]
    public async Task Editar_RespetaLosLargosMaximos()
    {
        using var aplicacion = new AplicacionDePrueba();
        var cliente = await aplicacion.ClienteAutenticadoAsync();

        await cliente.CrearEstudioAsync("Título válido", Hoy);
        var estudio = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];

        await cliente.EditarEstudioAsync(estudio.Id, new string('a', 201), Hoy);

        var despues = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioUno))[0];
        Assert.Equal("Título válido", despues.Titulo);
    }

    [Fact(DisplayName = "RNF-53: no se puede editar el estudio de otro propietario")]
    public async Task EstudioAjeno_NoSePuedeEditar()
    {
        using var aplicacion = new AplicacionDePrueba();
        var deBruno = await aplicacion.ClienteAutenticadoAsync(AplicacionDePrueba.UsuarioDos);
        await deBruno.CrearEstudioAsync("Estudio de Bruno", Hoy, institucion: "Sanatorio Norte");
        var deBrunoId = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioDos))[0].Id;

        var deAna = await aplicacion.ClienteAutenticadoAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await deAna.GetAsync($"/Estudios/Editar/{deBrunoId}")).StatusCode);

        await deAna.EditarEstudioAsync(deBrunoId, "Secuestrado", Hoy, institucion: "Otra");

        var despues = (await aplicacion.EstudiosDeAsync(AplicacionDePrueba.UsuarioDos))[0];
        Assert.Equal("Estudio de Bruno", despues.Titulo);
        Assert.Equal("Sanatorio Norte", despues.Institucion);
    }
}
