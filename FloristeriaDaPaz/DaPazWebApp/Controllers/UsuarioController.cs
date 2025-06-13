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
    }
}