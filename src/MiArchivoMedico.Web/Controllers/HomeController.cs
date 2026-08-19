using System.Diagnostics;
using MiArchivoMedico.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiArchivoMedico.Web.Controllers;

public class HomeController : Controller
{
    // La política global exige autenticación: Index queda protegido sin atributo (RF-01, AC-01).
    public IActionResult Index() => RedirectToAction("Index", "Estudios");

    // La muestra el service worker cuando la red falla, así que tiene que resolverse sin sesión: cuando
    // se guarda en la caché no hay forma de saber quién la va a ver después (RF-26, AC-40, AC-41).
    [AllowAnonymous]
    [HttpGet("/sin-conexion")]
    public IActionResult SinConexion() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
