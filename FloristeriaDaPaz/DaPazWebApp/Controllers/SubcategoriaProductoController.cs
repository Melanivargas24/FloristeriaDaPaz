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
            // Validar que se haya seleccionado una categoría
            if (model.idCategoriaProducto <= 0)
            {
                ModelState.AddModelError("idCategoriaProducto", "Debe seleccionar una categoría.");
            }
            
            if (!ModelState.IsValid)
            {
                // Recargar las categorías para el dropdown
                using var contextValidation = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                var categorias = contextValidation.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto", model.idCategoriaProducto);
                return View(model);
            }

            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                context.Execute("SP_RegistrarSubcategoriaProducto",
                    new { model.nombreSubcategoriaProducto, model.idCategoriaProducto },
                    commandType: CommandType.StoredProcedure);
                    
                TempData["Success"] = "Subcategoría agregada exitosamente.";
                return RedirectToAction("ConsultarSubcategoriasP");
            }
            catch (Exception ex)
            {
                // Recargar las categorías para el dropdown
                using var contextValidation = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                var categorias = contextValidation.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto", model.idCategoriaProducto);
                
                ViewBag.Error = $"Error al agregar subcategoría: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult EditarSubcategoriasP(int id)
        {
            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));

                var subcategoria = context.QueryFirstOrDefault<SubcategoriaProductoModel>(
                    "SP_ObtenerSubcategoriaProductoPorId",
                    new { idSubcategoriaProducto = id },
                    commandType: CommandType.StoredProcedure
                );
                
                if (subcategoria == null)
                {
                    TempData["Error"] = "Subcategoría no encontrada.";
                    return RedirectToAction("ConsultarSubcategoriasP");
                }

                var categorias = context.Query<CategoriaProductoModel>(
                    "SP_ObtenerCategoriaProducto",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto", subcategoria.idCategoriaProducto);

                return View(subcategoria);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar subcategoría: {ex.Message}";
                return RedirectToAction("ConsultarSubcategoriasP");
            }
        }


        [HttpPost]
        public IActionResult EditarSubcategoriasP(SubcategoriaProductoModel model)
        {
            // Validar que se haya seleccionado una categoría
            if (model.idCategoriaProducto <= 0)
            {
                ModelState.AddModelError("idCategoriaProducto", "Debe seleccionar una categoría.");
            }
            
            if (!ModelState.IsValid)
            {
                // Recargar las categorías para el dropdown
                using var contextValidation = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                var categorias = contextValidation.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto", model.idCategoriaProducto);
                return View(model);
            }

            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                context.Execute("SP_ModificarSubcategoriaProducto",
                    new {
                        model.idSubcategoriaProducto,
                        model.nombreSubcategoriaProducto,
                        model.idCategoriaProducto
                    },
                    commandType: CommandType.StoredProcedure);
                    
                TempData["Success"] = "Subcategoría actualizada exitosamente.";
                return RedirectToAction("ConsultarSubcategoriasP");
            }
            catch (Exception ex)
            {
                // Recargar las categorías para el dropdown
                using var contextValidation = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                var categorias = contextValidation.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto", model.idCategoriaProducto);
                
                ViewBag.Error = $"Error al actualizar subcategoría: {ex.Message}";
                return View(model);
            }
        }
    }
}

