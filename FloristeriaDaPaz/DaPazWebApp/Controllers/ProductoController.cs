﻿using DaPazWebApp.Helpers;
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
                var categoriasP = connection.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto",
                    commandType: CommandType.StoredProcedure).ToList();

                var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores",
                    commandType: CommandType.StoredProcedure).ToList();

                ViewBag.Categorias = new SelectList(categoriasP, "idCategoriaProducto", "nombreCategoriaProducto");
                ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");
            }

            return View(new Producto());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Producto model)
        {
            var file = Request.Form.Files.GetFile("Imagen");


            if (file != null && file.Length > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                model.Imagen = "/imagenes/" + fileName;
            }
            else
            {
                ModelState.AddModelError("Imagen", "Debe seleccionar una imagen.");
            }

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categoriasP = connection.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto",
                        commandType: CommandType.StoredProcedure).ToList();

                    var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categoriasP, "idCategoriaProducto", "nombreCategoriaProducto");
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
                        model.PrecioCompra,
                        model.Stock,
                        model.Imagen,
                        model.Estado,
                        model.IdCategoriaProducto,
                        model.IdSubcategoriaProducto,
                        model.IdProveedor
                    },
                    commandType: CommandType.StoredProcedure);

                // Registrar actividad en el historial
                var usuario = HttpContext.Session.GetString("Usuario") 
                           ?? HttpContext.Session.GetString("Nombre") 
                           ?? "Sistema";
                AuditoriaHelper.RegistrarActividad(
                    tipoActividad: "Crear",
                    modulo: "Producto",
                    descripcion: $"Nuevo producto creado: {model.NombreProducto}",
                    usuario: usuario,
                    detalles: $"Precio: ₡{model.Precio:N0}, Stock: {model.Stock}"
                );
            }

            return RedirectToAction("Index");
        }



        [HttpGet]
        public IActionResult Edit(int id)
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var categorias = context.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto",
                   commandType: CommandType.StoredProcedure).ToList();

                var proveedores = context.Query<ProveedorModel>("SP_ObtenerProveedores",
                    commandType: CommandType.StoredProcedure).ToList();

                ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto");
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
            // Manejo de nueva imagen (si se sube)
            var nuevaImagen = Request.Form.Files["ImagenNueva"];
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

                // Guardar nueva ruta
                model.Imagen = "/imagenes/" + fileName;
            }
            // Si no hay imagen nueva, se conserva la que viene en el hidden input (ya está en model.Imagen)

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var categorias = connection.Query<CategoriaProductoModel>("SP_ObtenerCategoriaProducto",
                        commandType: CommandType.StoredProcedure).ToList();

                    var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores",
                        commandType: CommandType.StoredProcedure).ToList();

                    ViewBag.Categorias = new SelectList(categorias, "idCategoriaProducto", "nombreCategoriaProducto");
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
                        model.PrecioCompra,
                        model.Stock,
                        model.Imagen,
                        model.Estado,
                        model.IdCategoriaProducto,
                        model.IdSubcategoriaProducto,
                        model.IdProveedor
                    },
                    commandType: CommandType.StoredProcedure
                );

                // Registrar actividad en el historial
                var usuario = HttpContext.Session.GetString("Usuario") 
                           ?? HttpContext.Session.GetString("Nombre") 
                           ?? "Sistema";
                AuditoriaHelper.RegistrarActividad(
                    tipoActividad: "Editar",
                    modulo: "Producto",
                    descripcion: $"Producto actualizado: {model.NombreProducto}",
                    usuario: usuario,
                    detalles: $"ID: {model.IdProducto}, Precio: ₡{model.Precio:N0}, Stock: {model.Stock}"
                );
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public JsonResult GetSubcategorias(int idCategoriaProducto)
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var subcategorias = context.Query<SubcategoriaProductoModel>(
                    "SP_ObtenerSubcategoriasPorCategoria",
                    new { idCategoriaProducto },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return Json(subcategorias);
            }
        }

        [HttpGet]
        public IActionResult DetallesPV(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var producto = connection.QuerySingleOrDefault<Producto>(
                    "SP_ObtenerProductoPorId",
                    new { IdProducto = id },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                if (producto == null)
                    return NotFound();

                // Obtener y asignar el nombre de la categoría
                if (producto.IdCategoriaProducto.HasValue)
                {
                    var categoria = connection.QuerySingleOrDefault<CategoriaProductoModel>(
                        "SP_ObtenerCategoriaProductoPorId",
                        new { idCategoriaProducto = producto.IdCategoriaProducto },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    producto.NombreCategoriaProducto = categoria?.nombreCategoriaProducto;
                }

                // Aplicar promociones al producto
                var promocion = PromocionHelper.ObtenerPromocionActiva(producto.IdProducto, _configuration);
                if (promocion != null && promocion.descuentoPorcentaje.HasValue)
                {
                    producto.TienePromocion = true;
                    producto.IdPromocion = promocion.idPromocion;
                    producto.NombrePromocion = promocion.nombrePromocion;
                    producto.PorcentajeDescuento = promocion.descuentoPorcentaje;
                    producto.PrecioConDescuento = Math.Round((decimal)(producto.Precio ?? 0) * (1 - (decimal)(promocion.descuentoPorcentaje.Value / 100.0)), 2);
                }
                else
                {
                    producto.TienePromocion = false;
                    producto.PrecioConDescuento = (decimal)(producto.Precio ?? 0);
                }

                return View("DetallesPV", producto);
            }
        }

    }
}