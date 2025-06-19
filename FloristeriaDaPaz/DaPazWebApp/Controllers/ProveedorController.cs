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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ProveedorModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute("SP_RegistrarProveedor",
                    new
                    {
                        model.nombreProveedor,
                        model.correoProveedor,
                        model.telefonoProveedor,
                        model.direccionProveedor,
                        model.estado,
                        model.IdDistrito
                    },
                    commandType: CommandType.StoredProcedure);
            }
            return RedirectToAction("Index");


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
                        model.IdProveedor,
                        model.nombreProveedor,
                        model.direccionProveedor,
                        model.correoProveedor,
                        model.telefonoProveedor,
                        model.estado,
                        model.IdDistrito
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            return RedirectToAction("Index");
        }
    }
}
