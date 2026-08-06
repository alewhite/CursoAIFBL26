using System.Diagnostics;
using MiArchivoMedico.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiArchivoMedico.Web.Controllers;

public class HomeController : Controller
{
    // La política global exige autenticación: Index queda protegido sin atributo (RF-01, AC-01).
    public IActionResult Index() => RedirectToAction("Index", "Estudios");

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
