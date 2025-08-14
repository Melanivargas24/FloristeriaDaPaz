using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

public class PromocionesController : Controller
{
    private readonly IConfiguration _configuration;

    public PromocionesController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        var promociones = connection.Query<Promociones>("SP_ConsultarPromociones", commandType: CommandType.StoredProcedure).ToList();
        
        // Cargar nombres de productos para mostrar en la vista
        foreach (var promo in promociones)
        {
            var producto = connection.QuerySingleOrDefault<Producto>(
                "SP_ObtenerProductoPorId", 
                new { IdProducto = promo.idProducto }, 
                commandType: CommandType.StoredProcedure);
            promo.nombreProducto = producto?.NombreProducto ?? "Producto no encontrado";
        }
        
        return View(promociones);
    }

    [HttpGet]
    public IActionResult Create()
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();

        ViewBag.Productos = new SelectList(productos, "IdProducto", "NombreProducto");
        ViewBag.Estados = new SelectList(new[] 
        { 
            new { Value = "Activa", Text = "Activa" }, 
            new { Value = "Inactiva", Text = "Inactiva" } 
        }, "Value", "Text");

        return View(new Promociones());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Promociones model)
    {
        if (!ModelState.IsValid)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();
            ViewBag.Productos = new SelectList(productos, "IdProducto", "NombreProducto");
            ViewBag.Estados = new SelectList(new[] 
            { 
                new { Value = "Activa", Text = "Activa" }, 
                new { Value = "Inactiva", Text = "Inactiva" } 
            }, "Value", "Text");
            return View(model);
        }

        using var conn = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        conn.Execute("SP_AgregarPromocion", new
        {
            model.nombrePromocion,
            model.descuentoPorcentaje,
            model.fechaInicio,
            model.fechaFin,
            model.idProducto,
            model.estado
        }, commandType: CommandType.StoredProcedure);

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        var promo = connection.QuerySingleOrDefault<Promociones>("SP_ObtenerPromocionPorId",
            new { idPromocion = id }, commandType: CommandType.StoredProcedure);

        if (promo == null) return NotFound();

        var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();
        ViewBag.Productos = new SelectList(productos, "IdProducto", "NombreProducto", promo.idProducto);
        ViewBag.Estados = new SelectList(new[] 
        { 
            new { Value = "Activa", Text = "Activa" }, 
            new { Value = "Inactiva", Text = "Inactiva" } 
        }, "Value", "Text", promo.estado);

        return View(promo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Promociones model)
    {
        if (!ModelState.IsValid)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();
            ViewBag.Productos = new SelectList(productos, "IdProducto", "NombreProducto", model.idProducto);
            ViewBag.Estados = new SelectList(new[] 
            { 
                new { Value = "Activa", Text = "Activa" }, 
                new { Value = "Inactiva", Text = "Inactiva" } 
            }, "Value", "Text", model.estado);
            return View(model);
        }

        using var conn = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        conn.Execute("SP_ModificarPromocion", new
        {
            model.idPromocion,
            model.nombrePromocion,
            model.descuentoPorcentaje,
            model.fechaInicio,
            model.fechaFin,
            model.idProducto,
            model.estado
        }, commandType: CommandType.StoredProcedure);

        return RedirectToAction("Index");
    }
}
