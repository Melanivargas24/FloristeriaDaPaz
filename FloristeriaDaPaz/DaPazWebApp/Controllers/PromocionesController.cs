
﻿using DaPazWebApp.Models;
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
        
        // Actualizar automáticamente el estado de promociones vencidas
        var fechaActual = DateTime.Now.Date;
        var promocionesParaActualizar = new List<Promociones>();
        
        foreach (var promo in promociones)
        {
            // Si la promoción está activa pero la fecha fin ya pasó, marcarla como inactiva
            if (promo.estado == "Activa" && promo.fechaFin.HasValue && promo.fechaFin.Value.Date < fechaActual)
            {
                promo.estado = "Inactiva";
                promocionesParaActualizar.Add(promo);
            }
        }
        
        // Actualizar en la base de datos las promociones que vencieron
        if (promocionesParaActualizar.Any())
        {
            foreach (var promo in promocionesParaActualizar)
            {
                connection.Execute("SP_ModificarPromocion", new
                {
                    promo.idPromocion,
                    promo.nombrePromocion,
                    promo.descuentoPorcentaje,
                    promo.fechaInicio,
                    promo.fechaFin,
                    promo.idProducto,
                    estado = "Inactiva"
                }, commandType: CommandType.StoredProcedure);
            }
        }
        
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
        // Validaciones adicionales del servidor
        if (model.fechaInicio.HasValue && model.fechaFin.HasValue)
        {
            if (model.fechaInicio.Value > model.fechaFin.Value)
            {
                ModelState.AddModelError("fechaInicio", "La fecha de inicio no puede ser posterior a la fecha de fin");
            }

            if (model.fechaInicio.Value.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("fechaInicio", "La fecha de inicio no puede ser anterior a la fecha actual");
            }
        }

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
        // Validaciones adicionales del servidor
        if (model.fechaInicio.HasValue && model.fechaFin.HasValue)
        {
            if (model.fechaInicio.Value > model.fechaFin.Value)
            {
                ModelState.AddModelError("fechaInicio", "La fecha de inicio no puede ser posterior a la fecha de fin");
            }
        }

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
