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
    public class ProductoController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProductoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var productos = context.Query<Producto>("SP_ObtenerProductos",
                    commandType: CommandType.StoredProcedure)
                    .OrderByDescending(p => p.IdProducto)
                    .ToList();
                return View(productos);
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categorias = connection.Query<CategoriaProducto>("SP_ObtenerCategorias",
                        commandType: CommandType.StoredProcedure).ToList();

                    var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categorias, "IdCategoriaProducto", "NombreCategoriaProducto");
                    ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");
                }
            

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Producto model)
        {

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categorias = connection.Query<CategoriaProducto>("SP_ObtenerCategorias",
                        commandType: CommandType.StoredProcedure).ToList();

                    var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categorias, "IdCategoriaProducto", "NombreCategoriaProducto");
                    ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");
                }
                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute("SP_AgregarProducto",
                    new
                    {
                        model.NombreProducto,
                        model.Descripcion,
                        model.Precio,
                        model.Stock,
                        model.Imagen,
                        model.Estado,
                        model.IdCategoriaProducto,
                        model.IdProveedor

                    },
                    commandType: CommandType.StoredProcedure);
            }
            return RedirectToAction("Index");


        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var categorias = context.Query<CategoriaProducto>("SP_ObtenerCategorias",
                   commandType: CommandType.StoredProcedure).ToList();

                var proveedores = context.Query<ProveedorModel>("SP_ObtenerProveedores",
                    commandType: CommandType.StoredProcedure).ToList();

                ViewBag.Categorias = new SelectList(categorias, "IdCategoriaProducto", "NombreCategoriaProducto");
                ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");

                var producto = context.QuerySingleOrDefault<Producto>(
                    "SELECT * FROM Producto WHERE IdProducto = @Id",
                    new { Id = id });

                if (producto == null)
                    return NotFound();

                return View(producto);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Producto model)
        {
            if (!ModelState.IsValid) {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categorias = connection.Query<CategoriaProducto>("SP_ObtenerCategorias",
                        commandType: CommandType.StoredProcedure).ToList();

                    var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categorias, "IdCategoriaProducto", "NombreCategoriaProducto");
                    ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");
                }
                return View(model);
        }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute(
                    "SP_ModificarProducto",
                    new
                    {
                        model.IdProducto,
                        model.NombreProducto,
                        model.Descripcion,
                        model.Precio,
                        model.Stock,
                        model.Imagen,
                        model.Estado,
                        model.IdCategoriaProducto,
                        model.IdProveedor
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            return RedirectToAction("Index");
        }

    }
}

