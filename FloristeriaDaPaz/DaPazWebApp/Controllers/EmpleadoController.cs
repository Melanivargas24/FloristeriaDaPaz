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
                @"SELECT e.idEmpleado, e.salario, e.fechaIngreso, e.fechaSalida, e.idUsuario, 
                         u.nombre, u.apellido, u.correo
                  FROM Empleado e
                  INNER JOIN Usuario u ON e.idUsuario = u.idUsuario"
            ).ToList();

            return View(empleados);
        }
    }
}