using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace DaPazWebApp.Controllers
{
    public class ProveedorController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProveedorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var proveedores = context.Query<ProveedorModel>("SP_ObtenerProveedores",
                    commandType: CommandType.StoredProcedure)
                    .OrderByDescending(p => p.IdProveedor)
                    .ToList();
                return View(proveedores);
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Obtener provincias para el dropdown
                var provincias = connection.Query(
                    "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                    commandType: CommandType.Text
                ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                ViewBag.Provincias = new SelectList(provincias, "Value", "Text");
                ViewBag.Cantones = new SelectList(new List<object>(), "Value", "Text");
                ViewBag.Distritos = new SelectList(new List<object>(), "Value", "Text");
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProveedorModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar dropdowns en caso de error
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var provincias = connection.Query(
                        "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                        commandType: CommandType.Text
                    ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                    ViewBag.Provincias = new SelectList(provincias, "Value", "Text");
                    ViewBag.Cantones = new SelectList(new List<object>(), "Value", "Text");
                    ViewBag.Distritos = new SelectList(new List<object>(), "Value", "Text");
                }
                return View(model);
            }

            try
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    context.Execute("SP_RegistrarProveedor",
                        new
                        {
                            nombreProveedor = model.nombreProveedor,
                            telefonoProveedor = model.telefonoProveedor,
                            correoProveedor = model.correoProveedor,
                            direccionProveedor = model.direccionProveedor,
                            estado = model.estado ?? "Activo",
                            idDistrito = model.IdDistrito ?? 1 // Valor por defecto si es null
                        },
                        commandType: CommandType.StoredProcedure);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear proveedor: {ex.Message}");
                ModelState.AddModelError("", $"Error al crear proveedor: {ex.Message}");
                
                // Recargar dropdowns en caso de error
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var provincias = connection.Query(
                        "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                        commandType: CommandType.Text
                    ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                    ViewBag.Provincias = new SelectList(provincias, "Value", "Text");
                    ViewBag.Cantones = new SelectList(new List<object>(), "Value", "Text");
                    ViewBag.Distritos = new SelectList(new List<object>(), "Value", "Text");
                }
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ProveedorModel proveedor;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                proveedor = context.QueryFirstOrDefault<ProveedorModel>(
                    "SP_ObtenerProveedorPorId",
                    new { idProveedor = id },
                    commandType: CommandType.StoredProcedure);
            }
            if (proveedor == null)
                return NotFound();

            return View(proveedor);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProveedorModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute(
                    "SP_ModificarProveedor",
                    new
                    {
                        idProveedor = model.IdProveedor,
                        nombreProveedor = model.nombreProveedor,
                        direccionProveedor = model.direccionProveedor,
                        correoProveedor = model.correoProveedor,
                        telefonoProveedor = model.telefonoProveedor,
                        estado = model.estado,
                        idDistrito = model.IdDistrito ?? 1
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult GetCantonesByProvincia(int idProvincia)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var cantones = connection.Query(
                    "SELECT idCanton, nombreCanton FROM Canton WHERE idProvincia = @idProvincia ORDER BY nombreCanton",
                    new { idProvincia },
                    commandType: CommandType.Text
                ).Select(c => new { value = c.idCanton, text = c.nombreCanton }).ToList();

                return Json(cantones);
            }
        }

        [HttpGet]
        public JsonResult GetDistritosByCanton(int idCanton)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var distritos = connection.Query(
                    "SELECT idDistrito, nombreDistrito FROM Distrito WHERE idCanton = @idCanton ORDER BY nombreDistrito",
                    new { idCanton },
                    commandType: CommandType.Text
                ).Select(d => new { value = d.idDistrito, text = d.nombreDistrito }).ToList();

                return Json(distritos);
            }
        }
    }
}
