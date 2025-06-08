using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #region Crear Cuenta

        [HttpGet]
        public IActionResult Signin()
        {
            var model = new UsersModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult Signin(UsersModel model)
        {
            if (!ModelState.IsValid)
            {
                // Si el modelo no es válido, vuelve a la vista con los errores.
                return View(model);
            }

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                context.Execute("SP_RegistrarUsuario",
                    new
                    {
                        model.nombre,
                        model.apellido,
                        model.correo,
                        model.telefono,
                        model.contrasena,
                    },
                    commandType: CommandType.StoredProcedure);
            }

            return RedirectToAction("Login", "Login");
        }

        #endregion

        #region IniciarSesion

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(UsersModel model)
        {
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var result = context.QueryFirstOrDefault<UsersModel>("SP_IniciarSesion",
                    new { model.correo, model.contrasena },
                    commandType: CommandType.StoredProcedure);

                // Si encuentra algo, iniciamos sesión
                if (result != null)
                {
                    // Usar HttpContext.Session para almacenar datos de sesión
                    HttpContext.Session.SetString("NombreUsuario", result.nombre ?? string.Empty);
                    HttpContext.Session.SetInt32("Rol", result.idRol ?? 0);
                    HttpContext.Session.SetInt32("IdUsuario", result.idUsuario ?? 0);
                    HttpContext.Session.SetString("Correo", result.correo ?? string.Empty);

                    return RedirectToAction("Index", "Home");
                }
                // Si no encuentra nada, mostramos mensaje de error
                else if (result == null)
                {
                    ViewBag.Error = "Su información no se ha podido validar correctamente";
                    return View();
                }

                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }
}
