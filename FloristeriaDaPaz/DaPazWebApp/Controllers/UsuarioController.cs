using System.Data;
using System.Security.Cryptography;
using System.Text;
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
                commandType: System.Data.CommandType.StoredProcedure)
                .OrderByDescending(u => u.idUsuario)
                .ToList();

            return View(usuarios);
        }

        [HttpGet]
        public IActionResult EditarUsuario(int id)
        {
            EditarUsuarioModel usuario;
            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Usar el SP que incluye información de ubicación completa
                var usuarioCompleto = context.QueryFirstOrDefault(
                    "SP_ObtenerUsuarioConUbicacion",
                    new { idUsuario = id },
                    commandType: CommandType.StoredProcedure);

                if (usuarioCompleto == null)
                    return NotFound();

                // Mapear a EditarUsuarioModel
                usuario = new EditarUsuarioModel
                {
                    idUsuario = usuarioCompleto.idUsuario,
                    nombre = usuarioCompleto.nombre,
                    apellido = usuarioCompleto.apellido,
                    correo = usuarioCompleto.correo,
                    telefono = usuarioCompleto.telefono,
                    direccion = usuarioCompleto.direccion,
                    idDistrito = usuarioCompleto.idDistrito,
                    idCanton = usuarioCompleto.idCanton,
                    idProvincia = usuarioCompleto.idProvincia
                };

                // Cargar dropdowns de ubicación
                var provincias = context.Query(
                    "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                    commandType: CommandType.Text
                ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                ViewBag.Provincias = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(provincias, "Value", "Text", usuario.idProvincia);
                ViewBag.Cantones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                ViewBag.Distritos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
            }

            return View(usuario);
        }

        [HttpPost]
        public IActionResult EditarUsuario(EditarUsuarioModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar dropdowns en caso de error
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var provincias = connection.Query(
                        "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                        commandType: CommandType.Text
                    ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                    ViewBag.Provincias = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(provincias, "Value", "Text");
                    ViewBag.Cantones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                    ViewBag.Distritos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                }
                ViewBag.Error = "Por favor, corrige los errores del formulario.";
                return View(model);
            }


            var nuevaContrasenaEncriptada = string.IsNullOrEmpty(model.nuevaContrasena) ? 
                null : Encrypt(model.nuevaContrasena);
            var contrasenaActualEncriptada = string.IsNullOrEmpty(model.contrasenaActual) ? 
                null : Encrypt(model.contrasenaActual);

            int filasAfectadas = 0;
            try
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    // Log para depuración
                    Console.WriteLine($"Actualizando usuario ID: {model.idUsuario}");
                    Console.WriteLine($"idDistrito: {model.idDistrito}");
                    
                    filasAfectadas = context.Execute("SP_EditarPerfil",
                        new
                        {
                            idUsuario = model.idUsuario,
                            nombre = model.nombre,
                            apellido = model.apellido,
                            telefono = model.telefono,
                            correo = model.correo,
                            contrasenaActual = contrasenaActualEncriptada,
                            nuevaContrasena = nuevaContrasenaEncriptada,
                            direccion = model.direccion,
                            idDistrito = model.idDistrito  // Permitir NULL
                        },
                        commandType: CommandType.StoredProcedure);
                        
                    Console.WriteLine($"Filas afectadas: {filasAfectadas}");
                }
            }
            catch (Exception ex)
            {
                // Log del error para depuración
                Console.WriteLine($"Error al actualizar usuario: {ex.Message}");
                ViewBag.Error = $"Error al actualizar el perfil: {ex.Message}";
                
                // Recargar dropdowns
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var provincias = connection.Query(
                        "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                        commandType: CommandType.Text
                    ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                    ViewBag.Provincias = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(provincias, "Value", "Text");
                    ViewBag.Cantones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                    ViewBag.Distritos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                }
                return View(model);
            }

            if (filasAfectadas > 0)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Recargar dropdowns en caso de error
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var provincias = connection.Query(
                        "SELECT idProvincia, nombreProvincia FROM Provincia ORDER BY nombreProvincia",
                        commandType: CommandType.Text
                    ).Select(p => new { Value = p.idProvincia, Text = p.nombreProvincia }).ToList();

                    ViewBag.Provincias = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(provincias, "Value", "Text");
                    ViewBag.Cantones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                    ViewBag.Distritos = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Value", "Text");
                }

                if (!string.IsNullOrEmpty(model.contrasenaActual) || !string.IsNullOrEmpty(model.nuevaContrasena))
                    ViewBag.Error = "No se pudo actualizar la contraseña. Verifica la contraseña actual.";
                else
                    ViewBag.Error = "No se pudo actualizar el perfil. Intenta nuevamente.";
                return View(model);
            }
        }

        private string Encrypt(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;
                
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_configuration.GetSection("Variables:llaveCifrado").Value!);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(texto);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }
        private string Decrypt(string texto)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(texto);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_configuration.GetSection("Variables:llaveCifrado").Value!);
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        [HttpGet]
        public JsonResult GetCantonesByProvincia(int idProvincia)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var cantones = connection.Query(
                    "SELECT idCanton, nombreCanton FROM Canton WHERE idProvincia = @idProvincia ORDER BY nombreCanton",
                    new { idProvincia },
                    commandType: CommandType.Text
                ).Select(c => new { value = c.idCanton, text = c.nombreCanton }).ToList();

                return Json(cantones);
            }
        }

        [HttpGet]
        public JsonResult GetDistritosByCanton(int idCanton)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var distritos = connection.Query(
                    "SELECT idDistrito, nombreDistrito FROM Distrito WHERE idCanton = @idCanton ORDER BY nombreDistrito",
                    new { idCanton },
                    commandType: CommandType.Text
                ).Select(d => new { value = d.idDistrito, text = d.nombreDistrito }).ToList();

                return Json(distritos);
            }
        }
    }
}