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
                usuario = context.QueryFirstOrDefault<EditarUsuarioModel>(
                    "SP_ObtenerUsuarioPorId",
                    new { idUsuario = id },
                    commandType: CommandType.StoredProcedure);
            }
            if (usuario == null)
                return NotFound();

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


            var nuevaContrasenaEncriptada = Encrypt(model.nuevaContrasena);


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
                        contrasenaActual = Encrypt(model.contrasenaActual),
                        nuevaContrasena = nuevaContrasenaEncriptada,
                        model.direccion
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


        private string Encrypt(string texto)
        {
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
    }
}