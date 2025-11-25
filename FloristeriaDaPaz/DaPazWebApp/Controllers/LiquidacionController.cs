using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Controllers
{
    public class LiquidacionController : Controller
    {
        private readonly IConfiguration _configuration;

        public LiquidacionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var sql = @"
            SELECT l.idLiquidacion, l.idEmpleado, l.fechaLiquidacion, l.montoLiquidacion,
                   e.idEmpleado, e.salario, e.fechaIngreso, e.fechaSalida, e.Cargo, e.idUsuario,
                   u.nombre, u.apellido
            FROM dbo.Liquidacion l
            INNER JOIN dbo.Empleado e ON l.idEmpleado = e.idEmpleado
            INNER JOIN dbo.Usuario u ON e.idUsuario = u.IdUsuario
            ORDER BY l.fechaLiquidacion DESC";

                var results = connection.Query(sql).ToList();

                var liquidaciones = results.Select(r => new Liquidacion
                {
                    IdLiquidacion = r.idLiquidacion,
                    IdEmpleado = r.idEmpleado,
                    FechaLiquidacion = r.fechaLiquidacion,
                    MontoLiquidacion = r.montoLiquidacion,
                    Empleado = new EmpleadoModel
                    {
                        idEmpleado = r.idEmpleado,
                        salario = r.salario,
                        fechaIngreso = r.fechaIngreso,
                        fechaSalida = r.fechaSalida,
                        Cargo = r.Cargo,
                        idUsuario = r.idUsuario,
                        nombre = r.nombre,
                        apellido = r.apellido
                    }
                }).ToList();

                return View(liquidaciones);
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var sql = @"SELECT e.idEmpleado, e.salario, u.nombre, u.apellido 
                           FROM dbo.Empleado e 
                           INNER JOIN dbo.Usuario u ON e.idUsuario = u.IdUsuario 
                           WHERE e.fechaSalida IS NULL";

                var empleados = connection.Query(sql)
                    .Select(e => new { idEmpleado = (int)e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                    .ToList();

                ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
            }

            // Establecer fecha por defecto como hoy
            var model = new Liquidacion
            {
                FechaLiquidacion = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Liquidacion model)
        {
            // Validación específica para empleado
            if (model.IdEmpleado <= 0)
            {
                ModelState.AddModelError("IdEmpleado", "Debe seleccionar un empleado para crear la liquidación");
            }

            // Validación adicional para monto
            if (model.MontoLiquidacion <= 0)
            {
                ModelState.AddModelError("MontoLiquidacion", "El monto de liquidación debe ser mayor a 0");
            }

            if (model.MontoLiquidacion > 999999999.99m)
            {
                ModelState.AddModelError("MontoLiquidacion", "El monto de liquidación es demasiado alto");
            }

            // Validación adicional: verificar que el empleado existe y está activo
            if (model.IdEmpleado > 0)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var empleadoExiste = connection.QueryFirstOrDefault<int>(
                        "SELECT COUNT(*) FROM dbo.Empleado WHERE idEmpleado = @IdEmpleado AND fechaSalida IS NULL",
                        new { IdEmpleado = model.IdEmpleado });

                    if (empleadoExiste == 0)
                    {
                        ModelState.AddModelError("IdEmpleado", "El empleado seleccionado no existe o ya tiene fecha de salida");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var sql = "SELECT idEmpleado, nombre, apellido, salario FROM dbo.Empleado WHERE fechaSalida IS NULL";

                    var empleados = connection.Query<EmpleadoModel>(sql)
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                        .ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }
                return View(model);
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insertar la liquidación
                        var sqlLiquidacion = @"
                            INSERT INTO dbo.Liquidacion (idEmpleado, fechaLiquidacion, montoLiquidacion) 
                            VALUES (@IdEmpleado, @FechaLiquidacion, @MontoLiquidacion)";

                        connection.Execute(sqlLiquidacion, new
                        {
                            IdEmpleado = model.IdEmpleado,
                            FechaLiquidacion = model.FechaLiquidacion,
                            MontoLiquidacion = model.MontoLiquidacion
                        }, transaction);

                        // Actualizar la fecha de salida del empleado
                        var sqlEmpleado = @"
                            UPDATE dbo.Empleado 
                            SET fechaSalida = @FechaLiquidacion 
                            WHERE idEmpleado = @IdEmpleado";

                        connection.Execute(sqlEmpleado, new
                        {
                            IdEmpleado = model.IdEmpleado,
                            FechaLiquidacion = model.FechaLiquidacion
                        }, transaction);

                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Error al crear la liquidación: " + ex.Message);

                        var sql = "SELECT idEmpleado, nombre, apellido, salario FROM dbo.Empleado WHERE fechaSalida IS NULL";

                        var empleados = connection.Query<EmpleadoModel>(sql)
                            .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                            .ToList();

                        ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                        return View(model);
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult GetEmpleadoSalario(int idEmpleado)
        {
            try
            {
                if (idEmpleado <= 0)
                {
                    return Json(new { error = "ID de empleado inválido" });
                }

                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var sql = @"SELECT e.salario, e.fechaIngreso 
                               FROM dbo.Empleado e 
                               WHERE e.idEmpleado = @IdEmpleado AND e.fechaSalida IS NULL";

                    var empleado = connection.QueryFirstOrDefault<dynamic>(sql, new { IdEmpleado = idEmpleado });

                    if (empleado != null)
                    {
                        // Calcular antigüedad en años
                        var fechaIngreso = (DateTime)empleado.fechaIngreso;
                        var antiguedad = DateTime.Now.Year - fechaIngreso.Year;
                        
                        // Ajustar si aún no ha cumplido años este año
                        if (DateTime.Now.DayOfYear < fechaIngreso.DayOfYear)
                        {
                            antiguedad--;
                        }

                        // Calcular liquidación sugerida (ejemplo: salario * años de antigüedad)
                        var liquidacionSugerida = (decimal)empleado.salario * Math.Max(antiguedad, 1);

                        return Json(new
                        {
                            salario = empleado.salario,
                            antiguedad = antiguedad,
                            liquidacionSugerida = liquidacionSugerida,
                            fechaIngreso = fechaIngreso.ToString("dd/MM/yyyy")
                        });
                    }

                    return Json(new { error = "Empleado no encontrado o ya tiene fecha de salida" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener información del empleado: " + ex.Message });
            }
        }
    }
}