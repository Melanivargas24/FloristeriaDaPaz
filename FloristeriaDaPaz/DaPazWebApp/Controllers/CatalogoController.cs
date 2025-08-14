using Dapper;
using DaPazWebApp.Models;
using DaPazWebApp.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly IConfiguration _configuration;

        public CatalogoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(string buscar, string ordenarPor, string arregloCategoria, string productoCategoria)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));

            // Obtener datos - Solo productos con stock disponible usando procedimiento almacenado
            var productos = connection.Query("SP_ObtenerProductosConStock", commandType: CommandType.StoredProcedure)
                .Select(p => new CatalogoModel
                {
                    Id = p.IdProducto,
                    Nombre = p.NombreProducto,
                    Precio = p.Precio,
                    Imagen = p.Imagen,
                    Categoria = p.nombreCategoriaProducto ?? "Sin categoría",
                    Tipo = "Producto",
                    PrecioOriginal = p.Precio
                }).ToList();

            // Aplicar promociones a productos
            foreach (var producto in productos)
            {
                var promocion = PromocionHelper.ObtenerPromocionActiva(producto.Id, _configuration);
                if (promocion != null && promocion.descuentoPorcentaje.HasValue)
                {
                    producto.TienePromocion = true;
                    producto.IdPromocion = promocion.idPromocion;
                    producto.NombrePromocion = promocion.nombrePromocion;
                    producto.PorcentajeDescuento = promocion.descuentoPorcentaje;
                    producto.PrecioConDescuento = Math.Round(producto.Precio * (1 - (decimal)(promocion.descuentoPorcentaje.Value / 100.0)), 2);
                }
                else
                {
                    producto.TienePromocion = false;
                    producto.PrecioConDescuento = producto.Precio;
                }
            }

            var arreglos = connection.Query("SP_ObtenerArreglos", commandType: CommandType.StoredProcedure)
                .Select(a => new CatalogoModel
                {
                    Id = a.idArreglo,
                    Nombre = a.nombreArreglo,
                    Precio = a.precio,
                    Imagen = a.imagen,
                    Categoria = a.nombreCategoriaArreglo,
                    Tipo = "Arreglo",
                    PrecioOriginal = a.precio,
                    PrecioConDescuento = a.precio,
                    TienePromocion = false // Los arreglos por ahora no tienen promociones individuales
                }).ToList();

            // ViewBag categorías
            ViewBag.CategoriasArreglos = arreglos.Select(a => a.Categoria)
                .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();

            ViewBag.CategoriasProductos = productos.Select(p => p.Categoria)
                .Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c).ToList();

            List<CatalogoModel> catalogo;

            // Reglas de filtro exclusivas: si se elige una categoría de arreglos, ignora productos, y viceversa
            if (!string.IsNullOrWhiteSpace(arregloCategoria) && arregloCategoria != "all")
            {
                catalogo = arreglos
                    .Where(a => a.Categoria.ToLower() == arregloCategoria.ToLower()).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(productoCategoria) && productoCategoria != "all")
            {
                catalogo = productos
                    .Where(p => p.Categoria.ToLower() == productoCategoria.ToLower()).ToList();
            }
            else
            {
                catalogo = productos.Concat(arreglos).ToList();
            }

            // Búsqueda
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.ToLower();
                catalogo = catalogo
                    .Where(c => c.Nombre.ToLower().Contains(buscar) || c.Categoria.ToLower().Contains(buscar))
                    .ToList();
            }

            // Ordenamiento
            switch (ordenarPor)
            {
                case "precioAsc":
                    catalogo = catalogo.OrderBy(c => c.Precio).ToList();
                    break;
                case "precioDesc":
                    catalogo = catalogo.OrderByDescending(c => c.Precio).ToList();
                    break;
                case "recientes":
                default:
                    catalogo = catalogo.OrderByDescending(c => c.Id).ToList();
                    break;
            }

            return View("Catalogo", catalogo);
        }
    }
}