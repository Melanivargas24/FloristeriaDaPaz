using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection;

namespace DaPazWebApp.Controllers
{
    public class PlanillaController : Controller
    {

        private readonly IConfiguration _configuration;

        public PlanillaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var sql = "SP_ObtenerPlanilla";

                var planillas = connection.Query<Planilla, EmpleadoModel, Planilla>(
                 "SP_ObtenerPlanilla",
                  (planilla, empleado) =>
                  {
                      planilla.Empleado = empleado;
                      return planilla;
                  },
                        splitOn: "nombre",
                          commandType: CommandType.StoredProcedure
                     ).ToList();


                return View(planillas);
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                    .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido}).ToList();

                ViewBag.Empleados = new SelectList(empleados, "idEmpleado", "NombreCompleto");

            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Planilla model)
        {
            if (ModelState.IsValid)
            {
                return View(model);
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("@IdEmpleado", model.IdEmpleado);
                        parameters.Add("@FechaPlanilla", DateTime.Today);
                        parameters.Add("@SalarioBruto", model.SalarioBruto);
                        parameters.Add("@Deducciones", model.Deducciones);
                        parameters.Add("@SalarioNeto", model.SalarioNeto);
                        parameters.Add("@IdPlanilla", dbType: DbType.Int32, direction: ParameterDirection.Output);

                        connection.Execute("SP_CrearPlanilla", parameters, transaction, commandType: CommandType.StoredProcedure);

                        int idPlanilla = parameters.Get<int>("@IdPlanilla");

                        foreach (var detalle in model.DetallesHoras)
                        {
                            connection.Execute("SP_AgregarDetalleHoras",
                                new
                                {
                                    IdPlanilla = idPlanilla,
                                    detalle.Fecha,
                                    detalle.HorasRegulares,
                                    detalle.HorasExtra,
                                    detalle.Porcentaje
                                },
                                transaction,
                                commandType: CommandType.StoredProcedure);
                        }

                        connection.Execute("SP_CalcularSalarioPlanilla",
                        new { IdPlanilla = idPlanilla },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}