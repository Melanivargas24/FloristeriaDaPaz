using System.Data;
using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DaPazWebApp.Controllers
{
    public class CategoriaArregloController : Controller
    {
        private readonly IConfiguration _configuration;

        public CategoriaArregloController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult ConsultarCategoriasA()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var categorias = context.Query<CategoriaArregloModel>(
                "SP_ObtenerCategoriaArreglo",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return View(categorias);
        }

        [HttpGet]
        public IActionResult AgregarCategoriasA()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AgregarCategoriasA(CategoriaArregloModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute("SP_RegistrarCategoriaArreglo",
                new { model.nombreCategoriaArreglo },
                commandType: CommandType.StoredProcedure);

            return RedirectToAction("ConsultarCategoriasA");
        }

        [HttpGet]
        public IActionResult EditarCategoriasA(int id)
        {
            CategoriaArregloModel categoria;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                categoria = context.QueryFirstOrDefault<CategoriaArregloModel>(
                    "SP_ObtenerCategoriaArregloPorId",
                    new { idCategoriaArreglo = id },
                    commandType: CommandType.StoredProcedure);
            }
            if (categoria == null)
                return NotFound();

            return View(categoria);
        }

        [HttpPost]
        public IActionResult EditarCategoriasA(CategoriaArregloModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute("SP_ModificarCategoriaArreglo",
                new { model.idCategoriaArreglo, model.nombreCategoriaArreglo },
                commandType: CommandType.StoredProcedure);

            return RedirectToAction("ConsultarCategoriasA");
        }
    }
}