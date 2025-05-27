using System.Diagnostics;
using DaPazWebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace CasoEstudio1_JN_CesarArce.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

    }
}
