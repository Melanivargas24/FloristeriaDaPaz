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
    }
}