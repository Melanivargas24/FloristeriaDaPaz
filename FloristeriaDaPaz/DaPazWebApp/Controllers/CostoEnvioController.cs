using System.Data;
using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DaPazWebApp.Controllers
{
    public class CostoEnvioController : Controller
    {
        private readonly IConfiguration _configuration;

        public CostoEnvioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: CostoEnvio
        public IActionResult Index()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var distritos = context.Query<DistritoCostoEnvioModel>(
                @"SELECT 
                    d.idDistrito,
                    d.nombreDistrito,
                    d.costoEnvio,
                    c.nombreCanton,
                    p.nombreProvincia
                FROM dbo.Distrito d
                INNER JOIN dbo.Canton c ON d.idCanton = c.idCanton
                INNER JOIN dbo.Provincia p ON c.idProvincia = p.idProvincia
                ORDER BY p.nombreProvincia, c.nombreCanton, d.nombreDistrito",
                commandType: CommandType.Text)
                .OrderBy(d => d.nombreProvincia)
                .ThenBy(d => d.nombreCanton)
                .ThenBy(d => d.nombreDistrito)
                .ToList();

            return View(distritos);
        }

        // GET: CostoEnvio/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var distrito = context.QueryFirstOrDefault<DistritoCostoEnvioModel>(
                @"SELECT 
                    d.idDistrito,
                    d.nombreDistrito,
                    d.costoEnvio,
                    c.nombreCanton,
                    p.nombreProvincia
                FROM dbo.Distrito d
                INNER JOIN dbo.Canton c ON d.idCanton = c.idCanton
                INNER JOIN dbo.Provincia p ON c.idProvincia = p.idProvincia
                WHERE d.idDistrito = @idDistrito",
                new { idDistrito = id },
                commandType: CommandType.Text);

            if (distrito == null)
                return NotFound();

            return View(distrito);
        }

        // POST: CostoEnvio/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(DistritoCostoEnvioModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                var filasAfectadas = context.Execute(
                    "UPDATE dbo.Distrito SET costoEnvio = @costoEnvio WHERE idDistrito = @idDistrito",
                    new { costoEnvio = model.costoEnvio, idDistrito = model.idDistrito },
                    commandType: CommandType.Text);

                if (filasAfectadas > 0)
                {
                    TempData["Success"] = "Costo de envío actualizado correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "No se pudo actualizar el costo de envío.";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al actualizar: {ex.Message}";
                return View(model);
            }
        }

        // API para obtener el costo de envío de un distrito específico
        [HttpGet]
        public JsonResult GetCostoEnvioPorDistrito(int idDistrito)
        {
            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                var costoEnvio = context.QueryFirstOrDefault<decimal?>(
                    "SELECT costoEnvio FROM dbo.Distrito WHERE idDistrito = @idDistrito",
                    new { idDistrito },
                    commandType: CommandType.Text);

                return Json(new { success = true, costoEnvio = costoEnvio ?? 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
