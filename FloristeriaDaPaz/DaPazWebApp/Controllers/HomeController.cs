using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using NuGet.Protocol.Plugins;
using System.Data;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace DaPazWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Shop()
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Obtener arreglos
                var arreglos = context.Query<Arreglo>("SP_ObtenerArreglos",
                    commandType: CommandType.StoredProcedure)
                    .OrderByDescending(a => a.idArreglo)
                    .ToList();

                // Obtener categorías para el filtro
                var categorias = context.Query<CategoriaArregloModel>(
                    "SP_ObtenerCategoriaArreglo",
                    commandType: CommandType.StoredProcedure).ToList();

                // Enviar categorías a la vista
                ViewBag.Categorias = categorias;

                return View(arreglos);
            }
        }


        public IActionResult About()
        {
            return View();
        }

        public IActionResult Services()
        {
            return View();
        }
    }

}

