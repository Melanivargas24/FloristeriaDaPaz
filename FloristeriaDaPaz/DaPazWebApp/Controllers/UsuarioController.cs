using System.Data;
using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DaPazWebApp.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IConfiguration _configuration;

        public UsuarioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult ConsultarUsuarios()
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var usuarios = context.Query<UsuarioConsultaModel>(
                "SP_ObtenerUsuarios",
                commandType: System.Data.CommandType.StoredProcedure
            ).ToList();

            return View(usuarios);
        }

        [HttpGet]
        public IActionResult EditarUsuario(int id)
        {
            EditarUsuarioModel usuario;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                usuario = context.QueryFirstOrDefault<EditarUsuarioModel>(
                    "SELECT idUsuario, nombre, apellido, telefono, correo FROM Usuario WHERE idUsuario = @id",
                    new { id });
            }
            if (usuario == null)
                return NotFound();

            usuario.idUsuario = id;
            return View(usuario);
        }

        [HttpPost]
        public IActionResult EditarUsuario(EditarUsuarioModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Por favor, corrige los errores del formulario.";
                return View(model);
            }

            int filasAfectadas = 0;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                filasAfectadas = context.Execute("SP_EditarPerfil",
                    new
                    {
                        model.idUsuario,
                        model.nombre,
                        model.apellido,
                        model.telefono,
                        model.correo,
                        model.contrasenaActual,
                        model.nuevaContrasena
                    },
                    commandType: CommandType.StoredProcedure);
            }

            if (filasAfectadas > 0)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                if (!string.IsNullOrEmpty(model.contrasenaActual) || !string.IsNullOrEmpty(model.nuevaContrasena))
                    ViewBag.Error = "No se pudo actualizar la contraseña. Verifica la contraseña actual.";
                else
                    ViewBag.Error = "No se pudo actualizar el perfil. Intenta nuevamente.";
                return View(model);
            }
        }
    }
}