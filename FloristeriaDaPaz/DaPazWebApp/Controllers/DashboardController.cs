using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Vista principal del dashboard con menú de opciones
            return View();
        }


        public IActionResult Ventas()
        {
            var dashboardData = new DashboardViewModel();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    // Ventas diarias (últimos 7 días)
                    dashboardData.VentasDiarias = connection.Query<VentaDiariaModel>(
                        "SP_ObtenerVentasDiarias",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Ventas semanales (últimas 4 semanas)
                    dashboardData.VentasSemanales = connection.Query<VentaSemanalModel>(
                        "SP_ObtenerVentasSemanales",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Ventas mensuales (últimos 12 meses)
                    dashboardData.VentasMensuales = connection.Query<VentaMensualModel>(
                        "SP_ObtenerVentasMensuales",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Estadísticas generales
                    dashboardData.EstadisticasGenerales = connection.QueryFirstOrDefault<EstadisticasGeneralesModel>(
                        "SP_ObtenerEstadisticasGenerales",
                        commandType: CommandType.StoredProcedure
                    ) ?? new EstadisticasGeneralesModel();

                    // Si no hay datos de ventas de hoy desde el SP, calcular desde ventas diarias
                    if (dashboardData.EstadisticasGenerales.VentasHoy == 0 && dashboardData.VentasDiarias.Any())
                    {
                        var ventaHoy = dashboardData.VentasDiarias.FirstOrDefault(v => v.Fecha.Date == DateTime.Today);
                        if (ventaHoy != null)
                        {
                            dashboardData.EstadisticasGenerales.VentasHoy = ventaHoy.TotalVentas;
                            dashboardData.EstadisticasGenerales.VentasHoyCount = ventaHoy.CantidadVentas;
                        }
                    }

                    // Calcular ventas de la semana actual si no están disponibles
                    if (dashboardData.EstadisticasGenerales.VentasSemana == 0 && dashboardData.VentasDiarias.Any())
                    {
                        var inicioSemana = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                        var ventasSemana = dashboardData.VentasDiarias
                            .Where(v => v.Fecha >= inicioSemana)
                            .Sum(v => v.TotalVentas);
                        dashboardData.EstadisticasGenerales.VentasSemana = ventasSemana;
                    }

                    // Calcular ventas del mes actual si no están disponibles
                    if (dashboardData.EstadisticasGenerales.VentasMes == 0 && dashboardData.VentasMensuales.Any())
                    {
                        var ventaMesActual = dashboardData.VentasMensuales
                            .FirstOrDefault(v => v.Mes == DateTime.Today.Month && v.Año == DateTime.Today.Year);
                        if (ventaMesActual != null)
                        {
                            dashboardData.EstadisticasGenerales.VentasMes = ventaMesActual.TotalVentas;
                        }
                    }

                    // Top productos más vendidos
                    dashboardData.TopProductos = connection.Query<TopProductoModel>(
                        "SP_ObtenerTopProductos",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Resumen de inventario y productos con bajo stock
                    dashboardData.ResumenInventario = connection.QueryFirstOrDefault<ResumenInventarioModel>(
                        "SP_ObtenerResumenInventarioVentas",
                        commandType: CommandType.StoredProcedure
                    ) ?? new ResumenInventarioModel();

                    // Productos con bajo stock (stock <= 5 y > 0) y sin stock
                    dashboardData.ProductosBajoStock = connection.Query<ProductoBajoStockModel>(
                        "SP_ObtenerProductosBajoStockVentas",
                        commandType: CommandType.StoredProcedure
                    ).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en Dashboard: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    // En caso de error, crear valores por defecto
                    dashboardData.EstadisticasGenerales = new EstadisticasGeneralesModel();
                    dashboardData.VentasDiarias = new List<VentaDiariaModel>();
                    dashboardData.VentasSemanales = new List<VentaSemanalModel>();
                    dashboardData.VentasMensuales = new List<VentaMensualModel>();
                    dashboardData.TopProductos = new List<TopProductoModel>();
                    dashboardData.ProductosBajoStock = new List<ProductoBajoStockModel>();
                    dashboardData.ResumenInventario = new ResumenInventarioModel();
                }
            }

            return View("Ventas", dashboardData);
        }

        [HttpGet]
        public JsonResult ObtenerVentasPorPeriodo(string periodo)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var parametros = new { Periodo = periodo };
                var ventas = connection.Query(
                    "SP_ObtenerVentasPorPeriodo",
                    parametros,
                    commandType: CommandType.StoredProcedure
                ).ToList();

                return Json(ventas);
            }
        }
        
        public IActionResult Inventario()
        {
            var inventarioData = new InventarioViewModel();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    // Resumen de inventario
                    inventarioData.ResumenInventario = connection.QueryFirstOrDefault<ResumenInventarioModel>(
                        "SP_ObtenerResumenInventario",
                        commandType: CommandType.StoredProcedure
                    ) ?? new ResumenInventarioModel();

                    // Productos con bajo stock (stock <= 5 pero > 0)
                    inventarioData.ProductosBajoStock = connection.Query<ProductoBajoStockModel>(
                        "SP_ObtenerProductosBajoStock",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Productos sin stock
                    inventarioData.ProductosSinStock = connection.Query<ProductoBajoStockModel>(
                        "SP_ObtenerProductosSinStock",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Productos por categoría
                    inventarioData.ProductosPorCategoria = connection.Query<InventarioPorCategoriaModel>(
                        "SP_ObtenerInventarioPorCategoria",
                        commandType: CommandType.StoredProcedure
                    ).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en Inventario: {ex.Message}");
                    // En caso de error, crear valores por defecto
                    inventarioData.ResumenInventario = new ResumenInventarioModel();
                    inventarioData.ProductosBajoStock = new List<ProductoBajoStockModel>();
                    inventarioData.ProductosSinStock = new List<ProductoBajoStockModel>();
                    inventarioData.ProductosPorCategoria = new List<InventarioPorCategoriaModel>();
                }
            }

            return View(inventarioData);
        }
    }
}
