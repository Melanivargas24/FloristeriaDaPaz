﻿using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Rotativa.AspNetCore;
using System.Data;
using System.Reflection;
using System.Threading;

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
                var planillas = connection.Query<PlanillaViewModel>(
                    "SP_ObtenerPlanilla",
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return View(planillas);
            }
        }

        public IActionResult ImprimirPlanilla(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var resultado = connection.Query(
                    "SP_PlanillaCompleta",
                    new { idPlanilla = id },
                    commandType: CommandType.StoredProcedure
                ).ToList();

                if (!resultado.Any())
                {
                    return NotFound();
                }

                var primera = resultado.First();

                var viewModel = new DetallePlanillaCompletoViewModel
                {
                    IdPlanilla = primera.idPlanilla,
                    FechaPlanilla = primera.fechaPlanilla,
                    SalarioBruto = primera.salarioBruto,
                    Deducciones = primera.deducciones,
                    SalarioNeto = primera.salarioNeto,
                    TotalDeducciones = primera.TotalDeducciones,
                    SalarioEmpleado = primera.salarioEmpleado,
                    FechaIngreso = primera.fechaIngreso,
                    IdUsuario = primera.idUsuario,
                    Nombre = primera.nombre,
                    Apellido = primera.apellido,
                    Correo = primera.correo,
                    Telefono = primera.telefono,
                    TotalHorasRegulares = resultado.Sum(r => (decimal)r.HorasRegulares),
                    TotalHorasExtra = resultado.Sum(r => (decimal)r.HorasExtra),
                    PorcentajePromedio = resultado.Average(r => (decimal)r.Porcentaje),
                    DetallesHoras = resultado.Select(r => new DetalleHorasViewModel
                    {
                        Fecha = r.fecha,
                        HorasRegulares = r.HorasRegulares,
                        Precio = r.Precio,
                        HorasExtra = r.HorasExtra,
                        Porcentaje = r.Porcentaje,
                        Total = r.TotalFila
                    }).ToList(),

                    SalarioNetoCalculado = resultado.Sum(r => (decimal)r.TotalFila) - primera.deducciones
                };

                var deducciones = connection.Query<DeduccionViewModel>(
                    "SELECT Nombre, Monto FROM DetalleDeduccion WHERE IdPlanilla = @IdPlanilla",
                    new { IdPlanilla = id }
                ).ToList();

                viewModel.DeduccionesDetalle = deducciones;

                return new ViewAsPdf("ImprimirPlanilla", viewModel)
                {
                    FileName = $"Planilla_{viewModel.Nombre}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
                };
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var empleados = connection.Query<EmpleadoModel>("SP_ConsultarEmpleados")
                    .Select(e => new { e.idEmpleado, NombreCompleto = e.nombre + " " + e.apellido }).ToList();

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

        /*
       
        // ✅ PDF con consulta SQL directa
        public IActionResult DescargarPdf(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var query = @"
    SELECT 
        p.idPlanilla AS IdPlanilla,
        p.fechaPlanilla AS FechaPlanilla,
        p.salarioBruto AS SalarioBruto,
        p.deducciones AS Deducciones,
        p.salarioNeto AS SalarioNeto,
        ISNULL(d.Nombre, 'Sin deducción') AS NombreDeduccion,
        ISNULL(pd.MontoReducido, 0) AS MontoDeduccion,
        '' AS NombreEmpleado,        -- valor por defecto
        '' AS ApellidoEmpleado,      -- valor por defecto
        e.fechaIngreso AS FechaIngreso,
        e.Cargo AS Cargo
    FROM Planilla p
    INNER JOIN Empleado e ON p.idEmpleado = e.idEmpleado
    LEFT JOIN PlanillaDeduccion pd ON p.idPlanilla = pd.IdPlanilla
    LEFT JOIN Deduccion d ON pd.IdDeduccion = d.IdDeduccion
    WHERE p.idPlanilla = @IdPlanilla;
";


                var planilla = connection.QueryFirstOrDefault<PlanillaViewModel>(
                    query,
                    new { IdPlanilla = id }
                );

                if (planilla == null)
                    return NotFound();

                return new ViewAsPdf("Comprobante", planilla)
                {
                    FileName = $"Planilla_{planilla.NombreEmpleado}_{planilla.FechaPlanilla:yyyyMMdd}.pdf"
                };
            }
        }
        */
    }
}
