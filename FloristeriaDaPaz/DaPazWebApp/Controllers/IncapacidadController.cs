using DaPazWebApp.Models;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Dapper;

namespace DaPazWebApp.Controllers
{
    public class IncapacidadController : Controller
    {
        private readonly IConfiguration _configuration;

        public IncapacidadController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var incapacidades = connection.Query<IncapacidadViewModel>(
                    "SP_ObtenerIncapacidades",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(incapacidades);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                    .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre  + " " + e.apellido }).ToList();

                ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");

                // Preparar modelo con fechas inicializadas (el número se genera automáticamente)
                var model = new Incapacidad
                {
                    NumeroIncapacidad = "", // Se generará automáticamente por JavaScript y/o servidor
                    FechaInicio = DateTime.Today,
                    FechaFin = DateTime.Today
                };

                return View(model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Incapacidad model)
        {
            // SIEMPRE generar un número de incapacidad automáticamente
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Generar nuevo número siempre para evitar conflictos
                model.NumeroIncapacidad = GenerarNumeroIncapacidad(connection);
            }

            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido }).ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }

                return View(model);
            }

            // Validación específica para empleado
            if (model.IdEmpleado <= 0)
            {
                ModelState.AddModelError("IdEmpleado", "Debe seleccionar un empleado");
            }

            // Si hay errores de validación, recargar la vista
            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido }).ToList();
                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }
                
                return View(model);
            }

            try
            {
                using (var conn = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {

                    conn.Execute("SP_CrearIncapacidad", new
                    {
                        model.NumeroIncapacidad,
                        model.MotivoIncapacidad,
                        model.FechaInicio,
                        model.FechaFin,
                        model.CentroMedicoEmisor,
                        model.EntidadEmisora,
                        model.IdEmpleado
                    }, commandType: CommandType.StoredProcedure);
                }

                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // En caso de error, recargar la vista con los empleados
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido }).ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }

                ViewBag.Error = "Error al guardar la incapacidad. Por favor intente nuevamente.";
                return View(model);
            }
        }

        [HttpGet]
        public JsonResult GenerarNuevoNumero()
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var nuevoNumero = GenerarNumeroIncapacidad(connection);
                    return Json(new { numero = nuevoNumero });
                }
            }
            catch (Exception)
            {
                return Json(new { error = "Error al generar número" });
            }
        }

        [HttpPost]
        public JsonResult VerificarNumeroIncapacidad(string numeroIncapacidad)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var existe = connection.QueryFirstOrDefault<int>(
                        "SELECT COUNT(*) FROM Incapacidad WHERE NumeroIncapacidad = @numero",
                        new { numero = numeroIncapacidad },
                        commandType: CommandType.Text);

                    return Json(new { existe = existe > 0 });
                }
            }
            catch (Exception)
            {
                return Json(new { error = "Error al verificar número" });
            }
        }

        private string GenerarNumeroIncapacidad(SqlConnection connection)
        {
            try
            {
                // Obtener el último número de incapacidad de la base de datos
                var ultimoNumero = connection.QueryFirstOrDefault<string>(
                    "SELECT TOP 1 NumeroIncapacidad FROM Incapacidad ORDER BY IdIncapacidad DESC",
                    commandType: CommandType.Text);

                if (string.IsNullOrEmpty(ultimoNumero))
                {
                    // Si no hay incapacidades previas, empezar con INC-0001
                    return "INC-0001";
                }

                // Extraer el número del formato INC-XXXX
                if (ultimoNumero.StartsWith("INC-") && ultimoNumero.Length >= 8)
                {
                    var numeroStr = ultimoNumero.Substring(4);
                    if (int.TryParse(numeroStr, out int numero))
                    {
                        return $"INC-{(numero + 1):D4}";
                    }
                }

                // Si el formato no es válido, buscar el mayor ID y generar basado en eso
                var maxId = connection.QueryFirstOrDefault<int>(
                    "SELECT ISNULL(MAX(IdIncapacidad), 0) + 1 FROM Incapacidad",
                    commandType: CommandType.Text);

                return $"INC-{maxId:D4}";
            }
            catch (Exception)
            {
                // En caso de error, usar timestamp para garantizar unicidad
                var timestamp = DateTime.Now.ToString("MMddHHmm");
                return $"INC-{timestamp}";
            }
        }
    }
}