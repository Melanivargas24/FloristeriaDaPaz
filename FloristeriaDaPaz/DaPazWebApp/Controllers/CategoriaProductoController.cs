using System.Data;
using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DaPazWebApp.Controllers
{
    public class CategoriaProductoController : Controller
    {
        private readonly IConfiguration _configuration;

        public CategoriaProductoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult ConsultarCategoriasP()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var categorias = context.Query<CategoriaProductoModel>(
                "SP_ObtenerCategoriaProducto",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return View(categorias);
        }

        [HttpGet]
        public IActionResult AgregarCategoriasP()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AgregarCategoriasP(CategoriaProductoModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute("SP_RegistrarCategoriaProducto",
                new { model.nombreCategoriaProducto },
                commandType: CommandType.StoredProcedure);

            return RedirectToAction("ConsultarCategoriasP");
        }

        [HttpGet]
        public IActionResult EditarCategoriasP(int id)
        {
            CategoriaProductoModel categoria;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                categoria = context.QueryFirstOrDefault<CategoriaProductoModel>(
                    "SP_ObtenerCategoriaProductoPorId",
                    new { idCategoriaProducto = id },
                    commandType: CommandType.StoredProcedure);
            }
            if (categoria == null)
                return NotFound();

            return View(categoria);
        }

        [HttpPost]
        public IActionResult EditarCategoriasP(CategoriaProductoModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute("SP_ModificarCategoriaProducto",
                new { model.idCategoriaProducto,model.nombreCategoriaProducto },
                commandType: CommandType.StoredProcedure);

            return RedirectToAction("ConsultarCategoriasP");
        }
    }
}