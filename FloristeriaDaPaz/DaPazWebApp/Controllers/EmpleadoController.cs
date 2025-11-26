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
            // Limpiar errores de campos que no se usan en este formulario
            ModelState.Remove("nombre");
            ModelState.Remove("apellido");
            ModelState.Remove("correo");
            ModelState.Remove("telefono");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validaciones adicionales para campos obligatorios
            if (model.salario <= 0)
            {
                ModelState.AddModelError("salario", "El salario debe ser mayor a 0");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Cargo))
            {
                ModelState.AddModelError("Cargo", "El cargo es obligatorio");
                return View(model);
            }

            if (model.idUsuario <= 0)
            {
                ModelState.AddModelError("idUsuario", "Debe seleccionar un usuario válido");
                return View(model);
            }

            if (model.fechaIngreso == default(DateTime))
            {
                ModelState.AddModelError("fechaIngreso", "La fecha de ingreso es obligatoria");
                return View(model);
            }

            try
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    // Verificar si el usuario ya es empleado activo
                    var yaEsEmpleado = context.QueryFirstOrDefault<int>(
                        "SELECT COUNT(*) FROM Empleado WHERE idUsuario = @idUsuario AND fechaSalida IS NULL",
                        new { idUsuario = model.idUsuario },
                        commandType: CommandType.Text);

                    if (yaEsEmpleado > 0)
                    {
                        ModelState.AddModelError("idUsuario", "Este usuario ya es un empleado activo");
                        return View(model);
                    }

                    var result = context.Execute("SP_AgregarEmpleado",
                        new
                        {
                            model.salario,
                            model.fechaIngreso,
                            model.Cargo,
                            model.idUsuario
                        },
                        commandType: CommandType.StoredProcedure);
                }

                return RedirectToAction("ConsultarEmpleados", "Empleado");
            }
            catch (Exception ex)
            {
                // Detectar errores específicos de base de datos
                if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate") || 
                    ex.Message.Contains("empleado") || ex.Message.Contains("usuario"))
                {
                    ModelState.AddModelError("idUsuario", "Este usuario ya es un empleado o hay un conflicto con los datos");
                }
                else
                {
                    ModelState.AddModelError("", "Error al agregar el empleado. Por favor, intenta nuevamente.");
                }
                return View(model);
            }
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
            
            if (usuario != null)
            {
                // Verificar si ya es empleado activo
                var yaEsEmpleado = context.QueryFirstOrDefault<bool>(
                    "SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END FROM Empleado WHERE idUsuario = @idUsuario AND fechaSalida IS NULL",
                    new { idUsuario = usuario.idUsuario },
                    commandType: CommandType.Text);
                    
                return Json(new { 
                    idUsuario = usuario.idUsuario,
                    nombre = usuario.nombre,
                    apellido = usuario.apellido,
                    correo = usuario.correo,
                    yaEsEmpleado = yaEsEmpleado
                });
            }
            
            return Json(null);
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
            // Validación adicional para prevenir valores negativos
            if (model.salario <= 0)
            {
                ModelState.AddModelError("salario", "El salario debe ser mayor a 0");
            }
            
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

            // Validación adicional para el salario
            if (model.salario <= 0)
            {
                ModelState.AddModelError("salario", "El salario debe ser mayor a 0");
                return View(model);
            }

            try
            {
                using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
                
                // Validar si el correo ya existe para otro empleado/usuario
                var correoExistente = context.QueryFirstOrDefault<int>(
                    "SELECT COUNT(*) FROM Usuario WHERE correo = @correo AND idUsuario != @idUsuario",
                    new { correo = model.correo, idUsuario = model.idUsuario },
                    commandType: CommandType.Text);

                if (correoExistente > 0)
                {
                    ModelState.AddModelError("correo", "Este correo electrónico ya está registrado por otro usuario");
                    return View(model);
                }
                
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
                // Detectar errores específicos de base de datos
                if (ex.Message.Contains("UNIQUE") || ex.Message.Contains("duplicate") || 
                    ex.Message.Contains("correo") || ex.Message.Contains("email"))
                {
                    ModelState.AddModelError("correo", "Este correo electrónico ya está registrado por otro usuario");
                }
                else
                {
                    ModelState.AddModelError("", "Ocurrió un error al actualizar el empleado. Por favor, intenta nuevamente.");
                }
                return View(model);
            }
        }
    }
}