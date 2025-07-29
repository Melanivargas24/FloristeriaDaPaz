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
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Incapacidad model)
        {
            if (ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                        .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido  }).ToList();

                    ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");
                }

                return View(model);
            }

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
    }
    }