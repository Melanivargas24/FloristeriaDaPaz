using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Identity;

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
                return View(model);
            }

            var contrasena = Encrypt(model.contrasena);

            using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Encriptar la contraseña antes de guardarla
                var contrasenaEncriptada = Encrypt(model.contrasena!);

                context.Execute("SP_RegistrarUsuario",
                    new
                    {
                        model.nombre,
                        model.apellido,
                        model.correo,
                        model.telefono,
                        contrasena = contrasenaEncriptada

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
        public async Task<IActionResult> Login(UsersModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Por favor, complete todos los campos correctamente.";
                return View(model);
            }

            try
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var contrasena = Encrypt(model.contrasena);

                    var result = context.QueryFirstOrDefault<UsersModel>(
                        "SP_IniciarSesion",
                        new { model.correo, contrasena },
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                    {
                        ViewBag.Error = "Correo o contraseña incorrectos.";
                        return View(model);
                    }

                    // Si encontró usuario válido
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, result.nombre!),
                new Claim(ClaimTypes.NameIdentifier, result.idUsuario.ToString()),
                new Claim(ClaimTypes.Role, result.idRol.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    HttpContext.Session.SetString("Nombre", result.nombre!);
                    // Guardar rol y usuario con las claves que usa el layout
                    HttpContext.Session.SetInt32("IdRol", result.idRol ?? 0);
                    HttpContext.Session.SetInt32("IdUsuario", result.idUsuario ?? 0);

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                // Aquí puedes hacer log de ex si quieres
                ViewBag.Error = "Ocurrió un error durante el inicio de sesión.";
                return View(model);
            }
        }

        #endregion

        #region Recuperar Contraseña

        [HttpGet]
        public IActionResult RecuperarContrasenna()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RecuperarContrasenna(UsersModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Mensaje = "Debe ingresar un correo válido.";
                return View();
            }

            try
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var result = context.QueryFirstOrDefault<UsersModel>(
                        "SP_ValidarUsuarioCorreo",
                        new { correo = model.correo },
                        commandType: CommandType.StoredProcedure
                    );

                    if (result != null)
                    {
                        var codigo = GenerarCodigo();
                        var nuevaContrasenna = Encrypt(codigo);
                        var contrasennaAnterior = string.Empty;

                        context.Execute(
                            "SP_ActualizarContrasenna",
                            new
                            {
                                Id = result.idUsuario,
                                Contrasenna = nuevaContrasenna,
                            },
                            commandType: CommandType.StoredProcedure
                        );

                        string contenido = $"Hola {result.nombre}, se ha generado el siguiente código de seguridad: {codigo}";
                        EnviarCorreo(result.correo!, "Actualización de Acceso", contenido);

                        ViewBag.Mensaje = "Se ha enviado un código de recuperación a su correo.";
                    }
                    else
                    {
                        ViewBag.Mensaje = "No se encontró ningún usuario con el correo ingresado.";
                    }
                }
            }
            catch (Exception)
            {
                ViewBag.Mensaje = "Ocurrió un error al procesar la solicitud. Intente nuevamente.";
            }

            return View();
        }

        #endregion

        #region Funcionalidades

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

        private string GenerarCodigo()
        {
            int length = 8;
            const string valid = "ABCDEFGHIJKLMNOPQRSTUVWXYZ012456789";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }

        private void EnviarCorreo(string destino, string asunto, string contenido)
        {
            string cuenta = _configuration.GetSection("Variables:CorreoEmail").Value!;
            string contrasenna = _configuration.GetSection("Variables:ClaveEmail").Value!;

            MailMessage message = new MailMessage();
            message.From = new MailAddress(cuenta);
            message.To.Add(new MailAddress(destino));
            message.Subject = asunto;
            message.Body = contenido;
            message.Priority = MailPriority.Normal;
            message.IsBodyHtml = true;

            SmtpClient client = new SmtpClient("smtp.office365.com", 587);
            client.Credentials = new System.Net.NetworkCredential(cuenta, contrasenna);
            client.EnableSsl = true;

            //Esto es para que no se intente enviar el correo si no hay una contraseña
            if (!string.IsNullOrEmpty(contrasenna))
            {
                client.Send(message);
            }
        }

        #endregion
        
    }
}
