using DaPazWebApp.Models;
using DaPazWebApp.Helpers;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;

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
                    // Ventas diarias
                    dashboardData.VentasDiarias = connection.Query<VentaDiariaModel>(
                        "SP_ObtenerVentasDiarias",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Ventas semanales
                    dashboardData.VentasSemanales = connection.Query<VentaSemanalModel>(
                        "SP_ObtenerVentasSemanales",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Ventas mensuales
                    var ventasMensuales = connection.Query<VentaMensualModel>(
                        "SP_ObtenerVentasMensuales",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Asignar nombres de meses en español
                    var mesesEspanol = new Dictionary<int, string>
                    {
                        { 1, "Enero" }, { 2, "Febrero" }, { 3, "Marzo" }, { 4, "Abril" },
                        { 5, "Mayo" }, { 6, "Junio" }, { 7, "Julio" }, { 8, "Agosto" },
                        { 9, "Septiembre" }, { 10, "Octubre" }, { 11, "Noviembre" }, { 12, "Diciembre" }
                    };

                    foreach (var venta in ventasMensuales)
                    {
                        venta.MesNombre = mesesEspanol.ContainsKey(venta.Mes) ? mesesEspanol[venta.Mes] : venta.Mes.ToString();
                    }
                    dashboardData.VentasMensuales = ventasMensuales;

                    // Estadísticas generales
                    dashboardData.EstadisticasGenerales = connection.QueryFirstOrDefault<EstadisticasGeneralesModel>(
                        "SP_ObtenerEstadisticasGenerales",
                        commandType: CommandType.StoredProcedure
                    ) ?? new EstadisticasGeneralesModel();

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

                    // En caso de error de base de datos, inicializar con listas vacías
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

        public IActionResult BalanceFinanciero()
        {
            var balanceData = new BalanceFinancieroViewModel();

            // Diccionario para convertir números de mes a nombres en español
            var mesesEspanol = new Dictionary<int, string>
            {
                { 1, "Enero" }, { 2, "Febrero" }, { 3, "Marzo" }, { 4, "Abril" },
                { 5, "Mayo" }, { 6, "Junio" }, { 7, "Julio" }, { 8, "Agosto" },
                { 9, "Septiembre" }, { 10, "Octubre" }, { 11, "Noviembre" }, { 12, "Diciembre" }
            };

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    // Resumen financiero general
                    balanceData.ResumenFinanciero = connection.QueryFirstOrDefault<ResumenFinancieroModel>(
                        "SP_ObtenerResumenFinanciero",
                        commandType: CommandType.StoredProcedure
                    ) ?? new ResumenFinancieroModel();

                    // Movimientos financieros recientes
                    balanceData.MovimientosRecientes = connection.Query<MovimientoFinancieroModel>(
                        "SP_ObtenerMovimientosRecientes",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Ingresos por mes
                    var ingresosPorMes = connection.Query<IngresoMensualModel>(
                        "SP_ObtenerIngresosPorMes",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Asignar nombres de meses en español
                    foreach (var ingreso in ingresosPorMes)
                    {
                        ingreso.MesNombre = mesesEspanol.ContainsKey(ingreso.Mes) ? mesesEspanol[ingreso.Mes] : ingreso.Mes.ToString();
                    }
                    balanceData.IngresosPorMes = ingresosPorMes;

                    // Egresos por mes
                    var egresosPorMes = connection.Query<EgresoMensualModel>(
                        "SP_ObtenerEgresosPorMes",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Asignar nombres de meses en español
                    foreach (var egreso in egresosPorMes)
                    {
                        egreso.MesNombre = mesesEspanol.ContainsKey(egreso.Mes) ? mesesEspanol[egreso.Mes] : egreso.Mes.ToString();
                    }
                    balanceData.EgresosPorMes = egresosPorMes;

                    // Flujo de caja diario
                    balanceData.FlujoCaja = connection.Query<FlujoCajaModel>(
                        "SP_ObtenerFlujoCaja",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // Análisis financiero
                    balanceData.AnalisisFinanciero = connection.QueryFirstOrDefault<AnalisisFinancieroModel>(
                        "SP_ObtenerAnalisisFinanciero",
                        commandType: CommandType.StoredProcedure
                    ) ?? new AnalisisFinancieroModel();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en Balance Financiero: {ex.Message}");
                    // En caso de error de base de datos, inicializar con datos vacíos
                    balanceData.ResumenFinanciero = new ResumenFinancieroModel();
                    balanceData.MovimientosRecientes = new List<MovimientoFinancieroModel>();
                    balanceData.IngresosPorMes = new List<IngresoMensualModel>();
                    balanceData.EgresosPorMes = new List<EgresoMensualModel>();
                    balanceData.FlujoCaja = new List<FlujoCajaModel>();
                    balanceData.AnalisisFinanciero = new AnalisisFinancieroModel();
                }
            }

            return View(balanceData);
        }

        public IActionResult HistorialActividades()
        {
            var historialData = new HistorialActividadesViewModel();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    // 1. Actividades recientes
                    historialData.ActividadesRecientes = connection.Query<ActividadRecienteModel>(
                        "SP_ObtenerActividadesRecientes",
                        new { CantidadRegistros = 50 },
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // 2. Actividades por tipo
                    historialData.ActividadesPorTipo = connection.Query<ActividadPorTipoModel>(
                        "SP_ObtenerActividadesPorTipo",
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // 3. Actividades por usuario
                    historialData.ActividadesPorUsuario = connection.Query<ActividadPorUsuarioModel>(
                        "SP_ObtenerActividadesPorUsuario",
                        new { TopUsuarios = 10 },
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // 4. Detalle de actividades
                    historialData.DetalleActividades = connection.Query<ActividadDetalleModel>(
                        "SP_ObtenerDetalleActividadesSimple",
                        new { CantidadRegistros = 100 },
                        commandType: CommandType.StoredProcedure
                    ).ToList();

                    // 5. Resumen calculado
                    var actividades = historialData.ActividadesRecientes;
                    historialData.ResumenActividades = new ResumenActividadesModel
                    {
                        TotalActividadesHoy = actividades.Count(a => a.FechaActividad.Date == DateTime.Now.Date),
                        TotalActividadesSemana = actividades.Count(a => a.FechaActividad >= DateTime.Now.AddDays(-7)),
                        TotalActividadesMes = actividades.Count(a => a.FechaActividad >= DateTime.Now.AddMonths(-1)),
                        UsuariosActivosHoy = actividades.Where(a => a.FechaActividad.Date == DateTime.Now.Date).Select(a => a.Usuario).Distinct().Count(),
                        ErroresHoy = 0,
                        ActividadMasFrecuente = historialData.ActividadesPorTipo.OrderByDescending(t => t.CantidadActividades).FirstOrDefault()?.TipoActividad ?? "N/A",
                        UsuarioMasActivo = historialData.ActividadesPorUsuario.FirstOrDefault()?.Usuario ?? "N/A",
                        PromedioActividadesDiarias = (decimal)(actividades.Count / 7.0)
                    };

                    // 6. Datos por fecha vacíos (no necesarios para la vista simplificada)
                    historialData.ActividadesPorFecha = new List<ActividadPorFechaModel>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en Historial de Actividades: {ex.Message}");
                    // En caso de error de base de datos, inicializar con datos vacíos
                    
                    historialData.ActividadesRecientes = new List<ActividadRecienteModel>();
                    historialData.ActividadesPorTipo = new List<ActividadPorTipoModel>();
                    historialData.ActividadesPorUsuario = new List<ActividadPorUsuarioModel>();
                    historialData.DetalleActividades = new List<ActividadDetalleModel>();
                    
                    // Inicializar resumen con valores por defecto
                    historialData.ResumenActividades = new ResumenActividadesModel
                    {
                        TotalActividadesHoy = 0,
                        TotalActividadesSemana = 0,
                        TotalActividadesMes = 0,
                        UsuariosActivosHoy = 0,
                        ErroresHoy = 0,
                        ActividadMasFrecuente = "N/A",
                        UsuarioMasActivo = "N/A",
                        PromedioActividadesDiarias = 0
                    };
                    
                    historialData.ActividadesPorFecha = new List<ActividadPorFechaModel>();
                }
            }

            return View(historialData);
        }
    }
}
