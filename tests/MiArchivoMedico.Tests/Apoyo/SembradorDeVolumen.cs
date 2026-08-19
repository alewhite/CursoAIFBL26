using Microsoft.EntityFrameworkCore;
using MiArchivoMedico.Web.Dominio;

namespace MiArchivoMedico.Tests.Apoyo;

/// <summary>
/// Genera la colección de 2.000 estudios ficticios sobre la que se miden AC-51 y AC-52. Es
/// reproducible: la misma semilla produce siempre la misma colección, de modo que dos mediciones
/// separadas en el tiempo sean comparables.
///
/// Todo el contenido es inventado (RNF-10): las instituciones y los profesionales son nombres
/// fabricados, no existen.
/// </summary>
public static class SembradorDeVolumen
{
    public const int EstudiosDelTecho = 2_000;

    private static readonly string[] Instituciones =
        ["Hospital Central", "Clínica del Sur", "Sanatorio Norte", "Centro Médico Este", "Instituto Oeste"];

    private static readonly string[] Profesionales =
        ["Dra. Rivas", "Dr. Peralta", "Dra. Suárez", "Dr. Molina", "Dra. Ferreyra"];

    private static readonly string[] Tipos =
        ["Ecografía abdominal", "Análisis de sangre", "Radiografía de tórax", "Resonancia de rodilla",
         "Electrocardiograma", "Control anual", "Tomografía de cráneo", "Espirometría"];

    private static readonly string[] Etiquetas =
        ["cardiología", "control", "urgencia", "traumatología", "clínica", "seguimiento"];

    public static async Task SembrarAsync(
        AplicacionDePrueba aplicacion, string nombreDeUsuario, int cantidad = EstudiosDelTecho, int semilla = 20260819)
    {
        using var contexto = await aplicacion.AbrirAsync(nombreDeUsuario);
        var azar = new Random(semilla);
        var hoy = DateOnly.FromDateTime(AplicacionDePrueba.MomentoInicial.UtcDateTime);

        for (var i = 0; i < cantidad; i++)
        {
            var estudio = new Estudio
            {
                Titulo = $"{Tipos[azar.Next(Tipos.Length)]} {i:D4}",
                Fecha = hoy.AddDays(-azar.Next(3_650)),
                Profesional = Profesionales[azar.Next(Profesionales.Length)],
                Institucion = Instituciones[azar.Next(Instituciones.Length)],
                Descripcion = $"Estudio ficticio número {i} generado para medir rendimiento.",
            };

            estudio.Etiquetas.Add(new EtiquetaDeEstudio { Texto = Etiquetas[azar.Next(Etiquetas.Length)] });
            contexto.Estudios.Add(estudio);

            // De a tandas, para no acumular 2.000 entidades en el rastreador.
            if ((i + 1) % 200 == 0) await contexto.SaveChangesAsync();
        }

        await contexto.SaveChangesAsync();
    }

    public static async Task<int> ContarAsync(AplicacionDePrueba aplicacion, string nombreDeUsuario)
    {
        using var contexto = await aplicacion.AbrirAsync(nombreDeUsuario);
        return await contexto.Estudios.CountAsync();
    }
}
