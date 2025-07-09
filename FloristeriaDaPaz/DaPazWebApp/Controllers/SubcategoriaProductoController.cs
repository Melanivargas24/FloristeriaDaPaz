using System.Data;
using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace DaPazWebApp.Controllers
{
    public class SubcategoriaProductoController : Controller
    {
        private readonly IConfiguration _configuration;

        public SubcategoriaProductoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult ConsultarSubcategoriasP()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var subcategorias = context.Query<SubcategoriaProductoModel>(
                "SP_ObtenerSubcategoriasProducto",
                commandType: CommandType.StoredProcedure
            ).ToList();

            return View(subcategorias);
        }

        [HttpGet]
        public IActionResult AgregarSubcategoriasP()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var categorias = context.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto", commandType: CommandType.StoredProcedure).ToList();
            ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto");

            return View();
        }

        [HttpPost]
        public IActionResult AgregarSubcategoriasP(SubcategoriaProductoModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute("SP_RegistrarSubcategoriaProducto",
    new { model.nombreSubcategoriaProducto, model.idCategoriaProducto },
    commandType: CommandType.StoredProcedure);

            return RedirectToAction("ConsultarSubcategoriasP");
        }

        [HttpGet]
public IActionResult EditarSubcategoriasP(int id)
{
    using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));

    var subcategoria = context.QueryFirstOrDefault<SubcategoriaProductoModel>(
        "SP_ObtenerSubcategoriaProductoPorId",
        new { idSubcategoriaProducto = id },
        commandType: CommandType.StoredProcedure
    );

    var categorias = context.Query<CategoriaProductoModel>(
        "SP_ObtenerCategoriaProducto",
        commandType: CommandType.StoredProcedure
    ).ToList();

    ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto", subcategoria?.idCategoriaProducto);

    return View(subcategoria);
}


        [HttpPost]
        public IActionResult EditarSubcategoriasP(SubcategoriaProductoModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute("SP_ModificarSubcategoriaProducto",
    new {
        model.idSubcategoriaProducto,
        model.nombreSubcategoriaProducto,
        model.idCategoriaProducto
    },
    commandType: CommandType.StoredProcedure);

            return RedirectToAction("ConsultarSubcategoriasP");
        }
    }
}

