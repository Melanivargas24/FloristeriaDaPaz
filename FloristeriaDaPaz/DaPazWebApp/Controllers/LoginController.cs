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
            // Obtener provincias directamente desde la base de datos usando Dapper
            List<Provincia> Provincia;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                Provincia = context.Query<Provincia>("SELECT idProvincia, nombreProvincia FROM Provincia").ToList();
            }

            var model = new UsersModel
            {
                Provincia = Provincia,
                Canton = new List<Canton>(), // Vacío hasta que se seleccione provincia
                Distrito = new List<Distrito>() // Vacío hasta que se seleccione cantón
            };
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
                        model.direccionExacta,
                        model.idDistrito,
                        model.idProvincia,
                        model.idCanton
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

        [HttpGet]
        public JsonResult GetCantones(int idProvincia)
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var cantones = context.Query<Canton>("SELECT idCanton, nombreCanton, idProvincia FROM Canton WHERE idProvincia = @idProvincia", new { idProvincia }).ToList();
            return Json(cantones);
        }

        [HttpGet]
        public JsonResult GetDistritos(int idCanton)
        {
            using var context = new SqlConnection(_configuration.GetConnectionString("BDConnection"));
            var distritos = context.Query<Distrito>("SELECT idDistrito, nombreDistrito, idCanton FROM Distrito WHERE idCanton = @idCanton", new { idCanton }).ToList();
            return Json(distritos);
        }
    }
}
