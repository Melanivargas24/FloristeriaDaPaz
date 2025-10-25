using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Controllers
{
    public class EmpleadoController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmpleadoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult AgregarE()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AgregarE(EmpleadoModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute("SP_AgregarEmpleado",
                    new
                    {
                        model.salario,
                        model.fechaIngreso,
                        model.Cargo,
                        model.idUsuario
                    },
                    commandType: CommandType.StoredProcedure);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult BuscarUsuarioPorCorreo(string correo)
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var usuario = context.QueryFirstOrDefault<UsersModel>(
                "SP_BuscarUsuarioPorCorreo",
                new { correo },
                commandType: CommandType.StoredProcedure
            );
            return Json(usuario);
        }

        [HttpGet]
        public IActionResult ConsultarEmpleados()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var empleados = context.Query<EmpleadoModel>(
                "SP_ConsultarEmpleados",
                commandType: CommandType.StoredProcedure)
                .OrderByDescending(e => e.idEmpleado)
                .ToList();

            return View(empleados);
        }

        [HttpGet]
        public IActionResult EditarE(int id)
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var empleado = context.QuerySingleOrDefault<EmpleadoModel>(
                "SP_ObtenerEmpleadoPorId",
                new { idEmpleado = id },
                commandType: CommandType.StoredProcedure
            );

            if (empleado == null)
                return NotFound();

            return View(empleado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditarE(EmpleadoModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Validaciones adicionales para evitar errores del sistema
            if (string.IsNullOrWhiteSpace(model.nombre))
            {
                ModelState.AddModelError("nombre", "El nombre es obligatorio");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.apellido))
            {
                ModelState.AddModelError("apellido", "El apellido es obligatorio");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.correo))
            {
                ModelState.AddModelError("correo", "El correo es obligatorio");
                return View(model);
            }

            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                
                // Actualizar empleado y usuario en una sola operación
                context.Execute(
                    "SP_ActualizarEmpleado",
                    new
                    {
                        model.idEmpleado,
                        model.nombre,
                        model.apellido,
                        model.correo,
                        model.salario,
                        model.fechaIngreso,
                        model.Cargo,
                        model.telefono,
                        model.idUsuario
                    },
                    commandType: CommandType.StoredProcedure
                );

                return RedirectToAction("ConsultarEmpleados", "Empleado");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al actualizar el empleado: " + ex.Message);
                return View(model);
            }
        }
    }
}