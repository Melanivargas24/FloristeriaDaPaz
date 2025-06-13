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

            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            context.Execute(
                "SP_ActualizarEmpleado",
                new
                {
                    model.idEmpleado,
                    model.salario,
                    model.fechaIngreso,
                    model.idUsuario,
                    model.nombre,
                    model.apellido,
                    model.correo,
                    model.telefono,
                },
                commandType: CommandType.StoredProcedure
            );

            return RedirectToAction("ConsultarEmpleados", "Empleado");
        }
    }
}