using Microsoft.AspNetCore.Mvc;

namespace BatX3_HSS_GUI.WebApi.Controllers;

public class DiagnosticsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
