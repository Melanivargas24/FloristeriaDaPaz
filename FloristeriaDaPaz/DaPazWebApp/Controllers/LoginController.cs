using DaPazWebApp.Models;
using Dapper;
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

                if (result == null)
                {
                    ViewBag.Error = "Correo o contraseña incorrectos.";
                    return View();
                }

                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }
}
