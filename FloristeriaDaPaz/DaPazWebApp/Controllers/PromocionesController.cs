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
        return View(promociones);
    }

    [HttpGet]
    public IActionResult Create()
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();

        ViewBag.Productos = new SelectList(productos, "IdProducto", "NombreProducto");

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
            return View(model);
        }

        using var conn = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
        conn.Execute("SP_AgregarPromocion", new
        {
            model.nombrePromocion,
            model.descuentoPorcentaje,
            model.fechaInicio,
            model.fechaFin,
            model.idProducto
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
            model.idProducto
        }, commandType: CommandType.StoredProcedure);

        return RedirectToAction("Index");
    }
}
