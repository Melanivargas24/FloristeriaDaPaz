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
    public class ArregloController : Controller
    {

        private readonly IConfiguration _configuration;

        public ArregloController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var arreglos = context.Query<Arreglo>("SP_ObtenerArreglos",
                    commandType: CommandType.StoredProcedure)
                    .OrderByDescending(a => a.idArreglo)
                    .ToList();

                // Cargar productos para cada arreglo
                foreach (var arreglo in arreglos)
                {
                    var productos = context.Query<ArregloProductoModel>(
                        "SP_ObtenerProductosPorArreglo",
                        new { idArreglo = arreglo.idArreglo },
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    arreglo.Productos = productos;
                }

                return View(arreglos);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var categoriasA = context.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                    commandType: CommandType.StoredProcedure).ToList();

                var productos = context.Query<ArregloProductoModel>(
    "SP_ObtenerProductosIdNombre",
    commandType: CommandType.StoredProcedure
).ToList();

                ViewBag.Categorias = new SelectList(categoriasA, "idCategoriaArreglo", "nombreCategoriaArreglo");
                ViewBag.Productos = productos;
            }

            return View(new Arreglo());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Arreglo model, IFormFile imagen)
        {
            if (imagen != null && imagen.Length > 0)
            {
                var fileName = Path.GetFileName(imagen.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imagen.CopyTo(stream);
                }

                model.imagen = "/imagenes/" + fileName;
            }
            else
            {
                ModelState.AddModelError("imagen", "Debe seleccionar una imagen.");
            }

            // Recibe productos y cantidades del formulario
            var productosSeleccionados = Request.Form["productosSeleccionados"];
            var cantidades = Request.Form["cantidades"];

            if (string.IsNullOrEmpty(productosSeleccionados) || string.IsNullOrEmpty(cantidades))
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto al arreglo.");
            }

            if (!ModelState.IsValid)
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categoriasA = context.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                        commandType: CommandType.StoredProcedure).ToList();
                    var productos = context.Query<ArregloProductoModel>(
    "SP_ObtenerProductosIdNombre",
    commandType: CommandType.StoredProcedure
).ToList();

                    ViewBag.Categorias = new SelectList(categoriasA, "idCategoriaArreglo", "nombreCategoriaArreglo");
                    ViewBag.Productos = productos;
                }

                return View(model);
            }

            int idArreglo;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Registrar el arreglo y obtener el ID generado
                idArreglo = context.QuerySingle<int>(
                    "SP_RegistrarArreglo",
                    new
                    {
                        model.nombreArreglo,
                        model.descripcion,
                        model.precio,
                        model.imagen,
                        model.estado,
                        model.idCategoriaArreglo
                },
                commandType: CommandType.StoredProcedure
        );
            }

            // Guardar productos del arreglo
            var productosArray = productosSeleccionados.ToString().Split(',');
            var cantidadesArray = cantidades.ToString().Split(',');

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                for (int i = 0; i < productosArray.Length; i++)
                {
                    context.Execute("SP_RegistrarArregloProducto",
                        new
                        {
                            idArreglo = idArreglo,
                            idProducto = int.Parse(productosArray[i]),
                            cantidadProducto = int.Parse(cantidadesArray[i])
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var categorias = context.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                   commandType: CommandType.StoredProcedure).ToList();

                var productos = context.Query<ArregloProductoModel>(
    "SP_ObtenerProductosIdNombre",
    commandType: CommandType.StoredProcedure
).ToList();

                ViewBag.Categorias = new SelectList(categorias, "idCategoriaArreglo", "nombreCategoriaArreglo");
                ViewBag.Productos = productos;

                var arreglo = context.QuerySingleOrDefault<Arreglo>(
            "SP_ObtenerArregloPorId",
            new { idArreglo = id },
            commandType: CommandType.StoredProcedure
        );

                if (arreglo == null)
                    return NotFound();

                // Cargar productos asociados al arreglo
                var productosArreglo = context.Query<ArregloProductoModel>(
                    "SP_ObtenerProductosPorArreglo",
                    new { idArreglo = id },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                arreglo.Productos = productosArreglo;

                return View(arreglo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Arreglo model, IFormFile imagen)
        {
            // Manejo de nueva imagen (si se sube)
            if (imagen != null && imagen.Length > 0)
            {
                var fileName = Path.GetFileName(imagen.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imagen.CopyTo(stream);
                }

                model.imagen = "/imagenes/" + fileName;
            }
            else
            {
                // Recuperar imagen desde el input oculto del formulario
                var imagenOculta = Request.Form["imagen"];
                if (!string.IsNullOrEmpty(imagenOculta))
                {
                    model.imagen = imagenOculta;
                }

                // Si sigue sin imagen, recuperarla desde la base de datos
                if (string.IsNullOrEmpty(model.imagen))
                {
                    using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                    {
                        var imagenActual = context.QuerySingleOrDefault<string>(
                        "SP_ObtenerImagenArreglo",
                        new { idArreglo = model.idArreglo },
                        commandType: CommandType.StoredProcedure
            );

                        if (!string.IsNullOrEmpty(imagenActual))
                        {
                            model.imagen = imagenActual;
                        }
                    }
                }

                // Si después de todo no hay imagen, agregar error
                if (string.IsNullOrEmpty(model.imagen))
                {
                    ModelState.AddModelError("imagen", "Debe seleccionar una imagen.");
                }
                else
                {
                    // ✅ Elimina validación automática del campo imagen si ya se resolvió
                    ModelState.Remove("imagen");
                }
            }

            // Recibe productos y cantidades del formulario
            var productosSeleccionados = Request.Form["productosSeleccionados"];
            var cantidades = Request.Form["cantidades"];

            if (string.IsNullOrEmpty(productosSeleccionados) || string.IsNullOrEmpty(cantidades))
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto al arreglo.");
            }

            if (!ModelState.IsValid)
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categorias = context.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                        commandType: CommandType.StoredProcedure).ToList();

                    var productos = context.Query<ArregloProductoModel>(
    "SP_ObtenerProductosIdNombre",
    commandType: CommandType.StoredProcedure
).ToList();

                    ViewBag.Categorias = new SelectList(categorias, "idCategoriaArreglo", "nombreCategoriaArreglo");
                    ViewBag.Productos = productos;
                }

                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Actualizar datos del arreglo
                context.Execute(
                    "SP_ModificarArreglo",
                    new
                    {
                        model.idArreglo,
                        model.nombreArreglo,
                        model.descripcion,
                        model.precio,
                        model.imagen,
                        model.estado,
                        model.idCategoriaArreglo
                    },
                    commandType: CommandType.StoredProcedure
                );

                // Eliminar productos actuales del arreglo
                context.Execute("SP_EliminarProductosDeArreglo",
            new { idArreglo = model.idArreglo },
            commandType: CommandType.StoredProcedure
        );

                // Insertar los nuevos productos del arreglo
                var productosArray = productosSeleccionados.ToString().Split(',');
                var cantidadesArray = cantidades.ToString().Split(',');

                for (int i = 0; i < productosArray.Length; i++)
                {
                    context.Execute("SP_RegistrarArregloProducto",
                        new
                        {
                            idArreglo = model.idArreglo,
                            idProducto = int.Parse(productosArray[i]),
                            cantidadProducto = int.Parse(cantidadesArray[i])
                        },
                        commandType: CommandType.StoredProcedure
                    );
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult DetallesAV(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var arreglo = connection.QuerySingleOrDefault<Arreglo>(
                    "SP_ObtenerArregloPorId",
                    new { IdArreglo = id },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                if (arreglo == null)
                    return NotFound();
                // Cargar nombre de la categoría si no viene
                if (string.IsNullOrEmpty(arreglo.nombreCategoriaArreglo))
                {
                    var categoria = connection.QuerySingleOrDefault<CategoriaArregloModel>(
                        "SELECT nombreCategoriaArreglo FROM CategoriaArreglo WHERE idCategoriaArreglo = @idCategoriaArreglo",
                        new { idCategoriaArreglo = arreglo.idCategoriaArreglo }
                    );
                    arreglo.nombreCategoriaArreglo = categoria?.nombreCategoriaArreglo ?? "";
                }
                // Cargar productos asociados
                var productos = connection.Query<ArregloProductoModel>(
                    "SP_ObtenerProductosPorArreglo",
                    new { idArreglo = id },
                    commandType: System.Data.CommandType.StoredProcedure
                ).ToList();
                arreglo.Productos = productos;
                return View("DetallesAV", arreglo);
            }
        }

    }
}

