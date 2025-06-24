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
                return View(arreglos);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var categoriasA = connection.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                    commandType: CommandType.StoredProcedure).ToList();


                ViewBag.Categorias = new SelectList(categoriasA, "idCategoriaArreglo", "nombreCategoriaArreglo");
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

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categoriasA = connection.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categoriasA, "idCategoriaArreglo", "nombreCategoriaArreglo");
                }

                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute("SP_RegistrarArreglo",
                    new
                    {
                        model.nombreArreglo,
                        model.descripcion,
                        model.precio,
                        model.stock,
                        model.imagen,
                        model.estado,
                        model.idCategoriaArreglo
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
                var categorias = context.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                   commandType: CommandType.StoredProcedure).ToList();

                ViewBag.Categorias = new SelectList(categorias, "idCategoriaArreglo", "nombreCategoriaArreglo");

                var arreglo = context.QuerySingleOrDefault<Arreglo>(
                    "SELECT * FROM Arreglo WHERE idArreglo = @Id",
                    new { Id = id });

                if (arreglo == null)
                    return NotFound();

                return View(arreglo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Arreglo model)
        {
            // Manejo de nueva imagen (si se sube)
            var nuevaImagen = Request.Form.Files["imagen"];
            if (nuevaImagen != null && nuevaImagen.Length > 0)
            {
                var fileName = Path.GetFileName(nuevaImagen.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    nuevaImagen.CopyTo(stream);
                }

                model.imagen = "/imagenes/" + fileName;
            }
            else
            {
                // Si no se subió nueva, mantener la anterior del input hidden
                model.imagen = Request.Form["imagen"];
            }

            // Si no hay imagen nueva, se conserva la que viene en el modelo

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categorias = connection.Query<CategoriaArregloModel>("SP_ObtenerCategoriaArreglo",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categorias, "idCategoriaArreglo", "nombreCategoriaArreglo");
                }

                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute(
                    "SP_ModificarArreglo",
                    new
                    {
                        model.idArreglo,
                        model.nombreArreglo,
                        model.descripcion,
                        model.precio,
                        model.stock,
                        model.imagen,
                        model.estado,
                        model.idCategoriaArreglo
                    },
                    commandType: CommandType.StoredProcedure
                );
            }

            return RedirectToAction("Index");
        }
    }
}

    