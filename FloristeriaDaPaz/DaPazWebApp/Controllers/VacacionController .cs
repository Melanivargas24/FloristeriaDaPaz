using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Controllers
{
    public class VacacionController : Controller
    {
        private readonly IConfiguration _configuration;

        public VacacionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var sql = @"
    SELECT v.idVacacion, v.idEmpleado, v.fechaInicio, v.fechaFin,
           e.idEmpleado, e.salario, e.fechaIngreso, e.fechaSalida, e.Cargo, e.idUsuario,
           u.nombre, u.apellido
    FROM dbo.Vacacion v
    INNER JOIN dbo.Empleado e ON v.idEmpleado = e.idEmpleado
    INNER JOIN dbo.Usuario u ON e.idUsuario = u.IdUsuario
    ORDER BY v.fechaInicio DESC";


                var results = connection.Query(sql).ToList();

                var vacaciones = results.Select(r => new Vacacion
                {
                    IdVacacion = r.idVacacion,
                    IdEmpleado = r.idEmpleado,
                    FechaInicio = r.fechaInicio,
                    FechaFin = r.fechaFin,
                    Empleado = new EmpleadoModel
                    {
                        idEmpleado = r.idEmpleado,
                        nombre = r.nombre,
                        apellido = r.apellido,
                        salario = r.salario,
                        fechaIngreso = r.fechaIngreso,
                        fechaSalida = r.fechaSalida,
                        Cargo = r.Cargo,
                        idUsuario = r.idUsuario
                    }
                }).ToList();

                return View(vacaciones);
            }
        }


        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var sql = @"SELECT e.idEmpleado, u.nombre, u.apellido 
                           FROM dbo.Empleado e 
                           INNER JOIN dbo.Usuario u ON e.idUsuario = u.IdUsuario 
                           WHERE e.fechaSalida IS NULL";

                var empleados = connection.Query(sql)
                    .Select(e => new { idEmpleado = (int)e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                    .ToList();

                ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Vacacion model)
        {
            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var sql = "SELECT idEmpleado, nombre, apellido FROM dbo.Empleado WHERE fechaSalida IS NULL";

                    var empleados = connection.Query<EmpleadoModel>(sql)
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                        .ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }
                return View(model);
            }

            // Validación adicional: fecha fin debe ser mayor que fecha inicio
            if (model.FechaFin <= model.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin debe ser posterior a la fecha de inicio");

                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var sql = "SELECT idEmpleado, nombre, apellido FROM dbo.Empleado WHERE fechaSalida IS NULL";

                    var empleados = connection.Query<EmpleadoModel>(sql)
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                        .ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }
                return View(model);
            }

            // Validación: verificar límite anual de 12 días de vacaciones
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var añoVacacion = model.FechaInicio.Year;
                var diasSolicitados = (model.FechaFin - model.FechaInicio).Days + 1;
                
                // Verificar días ya tomados en el año
                var sqlDiasUsados = @"
                    SELECT ISNULL(SUM(DATEDIFF(day, fechaInicio, fechaFin) + 1), 0) 
                    FROM dbo.Vacacion 
                    WHERE idEmpleado = @IdEmpleado 
                    AND (YEAR(fechaInicio) = @Anio OR YEAR(fechaFin) = @Anio)";

                var diasUsados = connection.QueryFirstOrDefault<int>(sqlDiasUsados, new
                {
                    IdEmpleado = model.IdEmpleado,
                    Anio = añoVacacion
                });

                var totalDias = diasUsados + diasSolicitados;
                
                if (totalDias > 12)
                {
                    var diasDisponibles = Math.Max(0, 12 - diasUsados);
                    ModelState.AddModelError("", $"No se pueden otorgar {diasSolicitados} días de vacaciones. El empleado ya ha utilizado {diasUsados} días en {añoVacacion}. Solo tiene {diasDisponibles} días disponibles (máximo 12 días por año).");

                    var sql = "SELECT idEmpleado, nombre, apellido FROM dbo.Empleado WHERE fechaSalida IS NULL";

                    var empleados = connection.Query<EmpleadoModel>(sql)
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                        .ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                    return View(model);
                }
                
                // Validación: verificar que no existan vacaciones superpuestas para el mismo empleado
                var sqlVerificacion = @"
                    SELECT COUNT(*) 
                    FROM dbo.Vacacion 
                    WHERE idEmpleado = @IdEmpleado 
                    AND (
                        (@FechaInicio BETWEEN fechaInicio AND fechaFin) OR
                        (@FechaFin BETWEEN fechaInicio AND fechaFin) OR
                        (fechaInicio BETWEEN @FechaInicio AND @FechaFin) OR
                        (fechaFin BETWEEN @FechaInicio AND @FechaFin)
                    )";

                var vacacionesSuperpuestas = connection.QueryFirstOrDefault<int>(sqlVerificacion, new
                {
                    IdEmpleado = model.IdEmpleado,
                    FechaInicio = model.FechaInicio,
                    FechaFin = model.FechaFin
                });

                if (vacacionesSuperpuestas > 0)
                {
                    ModelState.AddModelError("", "Ya existe una vacación registrada para este empleado en el rango de fechas seleccionado. Por favor seleccione fechas diferentes.");

                    var sql = "SELECT idEmpleado, nombre, apellido FROM dbo.Empleado WHERE fechaSalida IS NULL";

                    var empleados = connection.Query<EmpleadoModel>(sql)
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                        .ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                    return View(model);
                }
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    var sql = @"
                        INSERT INTO dbo.Vacacion (idEmpleado, fechaInicio, fechaFin) 
                        VALUES (@IdEmpleado, @FechaInicio, @FechaFin)";

                    connection.Execute(sql, new
                    {
                        IdEmpleado = model.IdEmpleado,
                        FechaInicio = model.FechaInicio,
                        FechaFin = model.FechaFin
                    });

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al crear la vacación: " + ex.Message);

                    var sql = "SELECT idEmpleado, nombre, apellido FROM dbo.Empleado WHERE fechaSalida IS NULL";

                    var empleados = connection.Query<EmpleadoModel>(sql)
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido })
                        .ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                    return View(model);
                }
            }
        }

        [HttpPost]
        public JsonResult VerificarDisponibilidadFechas(int idEmpleado, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var añoVacacion = fechaInicio.Year;
                    var diasSolicitados = (fechaFin - fechaInicio).Days + 1;
                    
                    // Verificar días ya utilizados en el año
                    var sqlDiasUsados = @"
                        SELECT ISNULL(SUM(DATEDIFF(day, fechaInicio, fechaFin) + 1), 0) 
                        FROM dbo.Vacacion 
                        WHERE idEmpleado = @IdEmpleado 
                        AND (YEAR(fechaInicio) = @Anio OR YEAR(fechaFin) = @Anio)";

                    var diasUsados = connection.QueryFirstOrDefault<int>(sqlDiasUsados, new
                    {
                        IdEmpleado = idEmpleado,
                        Anio = añoVacacion
                    });

                    var totalDias = diasUsados + diasSolicitados;
                    var diasDisponibles = Math.Max(0, 12 - diasUsados);
                    
                    // Verificar límite anual
                    if (totalDias > 12)
                    {
                        return Json(new 
                        { 
                            disponible = false, 
                            mensaje = $"Excede el límite anual. Días ya usados: {diasUsados}. Días solicitados: {diasSolicitados}. Días disponibles: {diasDisponibles}",
                            diasUsados = diasUsados,
                            diasSolicitados = diasSolicitados,
                            diasDisponibles = diasDisponibles,
                            limiteAnual = 12
                        });
                    }
                    
                    // Verificar fechas superpuestas
                    var sqlVerificacion = @"
                        SELECT COUNT(*) 
                        FROM dbo.Vacacion 
                        WHERE idEmpleado = @IdEmpleado 
                        AND (
                            (@FechaInicio BETWEEN fechaInicio AND fechaFin) OR
                            (@FechaFin BETWEEN fechaInicio AND fechaFin) OR
                            (fechaInicio BETWEEN @FechaInicio AND @FechaFin) OR
                            (fechaFin BETWEEN @FechaInicio AND @FechaFin)
                        )";

                    var vacacionesSuperpuestas = connection.QueryFirstOrDefault<int>(sqlVerificacion, new
                    {
                        IdEmpleado = idEmpleado,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    });

                    // Obtener detalles de las vacaciones superpuestas si existen
                    if (vacacionesSuperpuestas > 0)
                    {
                        var sqlDetalles = @"
                            SELECT fechaInicio, fechaFin 
                            FROM dbo.Vacacion 
                            WHERE idEmpleado = @IdEmpleado 
                            AND (
                                (@FechaInicio BETWEEN fechaInicio AND fechaFin) OR
                                (@FechaFin BETWEEN fechaInicio AND fechaFin) OR
                                (fechaInicio BETWEEN @FechaInicio AND @FechaFin) OR
                                (fechaFin BETWEEN @FechaInicio AND @FechaFin)
                            )";

                        var vacacionesExistentes = connection.Query(sqlDetalles, new
                        {
                            IdEmpleado = idEmpleado,
                            FechaInicio = fechaInicio,
                            FechaFin = fechaFin
                        }).ToList();

                        return Json(new 
                        { 
                            disponible = false, 
                            mensaje = "Ya existe una vacación registrada en estas fechas",
                            vacacionesExistentes = vacacionesExistentes.Select(v => new {
                                fechaInicio = ((DateTime)v.fechaInicio).ToString("dd/MM/yyyy"),
                                fechaFin = ((DateTime)v.fechaFin).ToString("dd/MM/yyyy")
                            })
                        });
                    }

                    return Json(new 
                    { 
                        disponible = true, 
                        mensaje = "Fechas disponibles",
                        diasUsados = diasUsados,
                        diasSolicitados = diasSolicitados,
                        diasDisponibles = diasDisponibles,
                        limiteAnual = 12
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { disponible = false, mensaje = "Error al verificar disponibilidad: " + ex.Message });
            }
        }
        
        [HttpGet]
        public JsonResult ObtenerDiasDisponibles(int idEmpleado, int anio)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var sqlDiasUsados = @"
                        SELECT ISNULL(SUM(DATEDIFF(day, fechaInicio, fechaFin) + 1), 0) 
                        FROM dbo.Vacacion 
                        WHERE idEmpleado = @IdEmpleado 
                        AND (YEAR(fechaInicio) = @Anio OR YEAR(fechaFin) = @Anio)";

                    var diasUsados = connection.QueryFirstOrDefault<int>(sqlDiasUsados, new
                    {
                        IdEmpleado = idEmpleado,
                        Anio = anio
                    });

                    var diasDisponibles = Math.Max(0, 12 - diasUsados);
                    
                    return Json(new 
                    {
                        diasUsados = diasUsados,
                        diasDisponibles = diasDisponibles,
                        limiteAnual = 12
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener días disponibles: " + ex.Message });
            }
        }
    }
}