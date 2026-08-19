using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiArchivoMedico.Web.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Estudios");

    /// <summary>
    /// Única pantalla anónima de la aplicación además del ingreso. No usa el layout y no muestra ningún
    /// dato médico, para que la copia que guarda el service worker sea idéntica para cualquiera
    /// (RF-26, RNF-51, AC-40, AC-41).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/sin-conexion")]
    public IActionResult SinConexion() => View();

    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error() => View();
}
