﻿using DaPazWebApp.Models;
using DaPazWebApp.Helpers;
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

            // Validación adicional para asegurar que la contraseña no sea nula o vacía
            if (string.IsNullOrWhiteSpace(model.contrasena))
            {
                ModelState.AddModelError("contrasena", "La contraseña es obligatoria");
                return View(model);
            }

            // Validación adicional para la longitud mínima de la contraseña
            if (model.contrasena.Length < 6)
            {
                ModelState.AddModelError("contrasena", "La contraseña debe tener al menos 6 caracteres");
                return View(model);
            }

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

            // Validación adicional para asegurar que la contraseña no sea nula o vacía
            if (string.IsNullOrWhiteSpace(model.contrasena))
            {
                ViewBag.Error = "La contraseña es obligatoria";
                return View(model);
            }

            // Validación adicional para el correo
            if (string.IsNullOrWhiteSpace(model.correo))
            {
                ViewBag.Error = "El correo es obligatorio";
                return View(model);
            }

            try
            {
                using (var context = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var contrasena = Encrypt(model.contrasena!);

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
                new Claim(ClaimTypes.NameIdentifier, result.idUsuario?.ToString() ?? "0"),
                new Claim(ClaimTypes.Role, result.idRol?.ToString() ?? "0")
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    HttpContext.Session.SetString("Usuario", result.nombre!);
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
            // Validación específica solo del correo para recuperación de contraseña
            if (string.IsNullOrWhiteSpace(model.correo))
            {
                ViewBag.Mensaje = "Debe ingresar un correo electrónico.";
                return View();
            }

            // Validar formato de correo
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ViewBag.Mensaje = "Debe ingresar un correo electrónico válido.";
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

                        try
                        {
                            context.Execute(
                                "SP_ActualizarContrasenna",
                                new
                                {
                                    Id = result.idUsuario,
                                    Contrasenna = nuevaContrasenna,
                                },
                                commandType: CommandType.StoredProcedure
                            );
                        }
                        catch (Exception dbEx)
                        {
                            ViewBag.Mensaje = $"Error al actualizar contraseña: {dbEx.Message}";
                            return View();
                        }

                        try
                        {
                            var habilitarCorreo = _configuration.GetSection("Variables:HabilitarEnvioCorreo").Value;
                            
                            if (habilitarCorreo == "true")
                            {
                                string contenido = $"Hola {result.nombre}, se ha generado el siguiente código de seguridad: {codigo}";
                                EnviarCorreo(result.correo!, "Actualización de Acceso", contenido);
                                ViewBag.Mensaje = "Se ha enviado un código de recuperación a su correo electrónico.";
                            }
                            else
                            {
                                ViewBag.Mensaje = $"Su contraseña se ha actualizado exitosamente. Su nuevo código es: {codigo}";
                            }
                        }
                        catch (Exception)
                        {
                            // Si falla el envío de correo, pero la contraseña se actualizó
                            ViewBag.Mensaje = $"Su contraseña se ha actualizado exitosamente. Su nuevo código es: {codigo}. (No se pudo enviar por correo)";
                        }
                    }
                    else
                    {
                        ViewBag.Mensaje = "No se encontró ningún usuario con el correo ingresado.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = $"Error en la operación: {ex.Message}";
            }

            return View();
        }

        #endregion

        #region Cerrar Sesión

        [HttpPost]
        public async Task<IActionResult> CerrarSesion()
        {
            // Limpiar la sesión
            HttpContext.Session.Clear();
            
            // Cerrar la autenticación por cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Index", "Home");
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
            try
            {
                string cuenta = _configuration.GetSection("Variables:CorreoEmail").Value!;
                string contrasenna = _configuration.GetSection("Variables:ClaveEmail").Value!;

                // Verificar si las credenciales están configuradas
                if (string.IsNullOrEmpty(cuenta) || string.IsNullOrEmpty(contrasenna))
                {
                    throw new Exception("Credenciales de correo no configuradas");
                }

                MailMessage message = new MailMessage();
                message.From = new MailAddress(cuenta);
                message.To.Add(new MailAddress(destino));
                message.Subject = asunto;
                message.Body = contenido;
                message.Priority = MailPriority.Normal;
                message.IsBodyHtml = true;

                SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
                client.Credentials = new System.Net.NetworkCredential(cuenta, contrasenna);
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;

                client.Send(message);
            }
            catch (Exception ex)
            {
                // Re-lanzar la excepción con más detalles
                throw new Exception($"Error al enviar correo: {ex.Message}");
            }
        }

        #endregion
        
    }
}