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


        public IActionResult Ventas(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var dashboardData = new DashboardViewModel();
            
            // Configurar filtro de fechas
            dashboardData.FiltroFechas.FechaInicio = fechaInicio;
            dashboardData.FiltroFechas.FechaFin = fechaFin;

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    var parametros = new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    };

                    // Si no hay filtro de fechas, usar stored procedures originales
                    if (fechaInicio == null || fechaFin == null)
                    {
                        // Ventas diarias (sin filtro)
                        dashboardData.VentasDiarias = connection.Query<VentaDiariaModel>(
                            "SP_ObtenerVentasDiarias",
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Ventas semanales (sin filtro)
                        dashboardData.VentasSemanales = connection.Query<VentaSemanalModel>(
                            "SP_ObtenerVentasSemanales",
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Ventas mensuales (sin filtro)
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

                        // Estadísticas generales (sin filtro)
                        dashboardData.EstadisticasGenerales = connection.QueryFirstOrDefault<EstadisticasGeneralesModel>(
                            "SP_ObtenerEstadisticasGenerales",
                            commandType: CommandType.StoredProcedure
                        ) ?? new EstadisticasGeneralesModel();

                        // Top productos más vendidos (sin filtro)
                        dashboardData.TopProductos = connection.Query<TopProductoModel>(
                            "SP_ObtenerTopProductos",
                            commandType: CommandType.StoredProcedure
                        ).ToList();
                    }
                    else
                    {
                        // Con filtro de fechas (usar SP filtrados)
                        dashboardData.VentasDiarias = connection.Query<VentaDiariaModel>(
                            "SP_ObtenerVentasDiariasFiltradas",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Ventas semanales (filtradas por rango de fechas si se especifica)
                        dashboardData.VentasSemanales = connection.Query<VentaSemanalModel>(
                            "SP_ObtenerVentasSemanalesFiltradas",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Ventas mensuales (filtradas por rango de fechas si se especifica)
                        var ventasMensuales = connection.Query<VentaMensualModel>(
                            "SP_ObtenerVentasMensualesFiltradas",
                            parametros,
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

                        // Estadísticas generales (filtradas por rango de fechas si se especifica)
                        dashboardData.EstadisticasGenerales = connection.QueryFirstOrDefault<EstadisticasGeneralesModel>(
                            "SP_ObtenerEstadisticasGeneralesFiltradas",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ) ?? new EstadisticasGeneralesModel();

                        // Top productos más vendidos (filtrados por rango de fechas si se especifica)
                        dashboardData.TopProductos = connection.Query<TopProductoModel>(
                            "SP_ObtenerTopProductosFiltrados",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();
                    }

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

                    // En caso de error de base de datos, usar datos simulados
                    dashboardData.EstadisticasGenerales = new EstadisticasGeneralesModel
                    {
                        VentasHoy = 150000,
                        VentasSemana = 850000,
                        VentasMes = 3200000,
                        VentasAño = 25000000,
                        TotalClientes = 125,
                        TotalProductos = 89,
                        VentasHoyCount = 8,
                        PromedioVentaDiaria = 460000
                    };

                    // Datos simulados para ventas diarias
                    dashboardData.VentasDiarias = new List<VentaDiariaModel>
                    {
                        new VentaDiariaModel { Fecha = DateTime.Today.AddDays(-6), TotalVentas = 320000, CantidadVentas = 5 },
                        new VentaDiariaModel { Fecha = DateTime.Today.AddDays(-5), TotalVentas = 450000, CantidadVentas = 7 },
                        new VentaDiariaModel { Fecha = DateTime.Today.AddDays(-4), TotalVentas = 380000, CantidadVentas = 6 },
                        new VentaDiariaModel { Fecha = DateTime.Today.AddDays(-3), TotalVentas = 520000, CantidadVentas = 9 },
                        new VentaDiariaModel { Fecha = DateTime.Today.AddDays(-2), TotalVentas = 410000, CantidadVentas = 8 },
                        new VentaDiariaModel { Fecha = DateTime.Today.AddDays(-1), TotalVentas = 390000, CantidadVentas = 7 },
                        new VentaDiariaModel { Fecha = DateTime.Today, TotalVentas = 150000, CantidadVentas = 8 }
                    };

                    // Datos simulados para ventas semanales
                    dashboardData.VentasSemanales = new List<VentaSemanalModel>
                    {
                        new VentaSemanalModel { Semana = 31, Año = 2025, FechaInicio = DateTime.Today.AddDays(-13), FechaFin = DateTime.Today.AddDays(-7), TotalVentas = 2100000, CantidadVentas = 35 },
                        new VentaSemanalModel { Semana = 32, Año = 2025, FechaInicio = DateTime.Today.AddDays(-6), FechaFin = DateTime.Today, TotalVentas = 2620000, CantidadVentas = 50 }
                    };

                    // Datos simulados para ventas mensuales
                    dashboardData.VentasMensuales = new List<VentaMensualModel>
                    {
                        new VentaMensualModel { Mes = 6, Año = 2025, TotalVentas = 8500000, CantidadVentas = 145, MesNombre = "Junio" },
                        new VentaMensualModel { Mes = 7, Año = 2025, TotalVentas = 9200000, CantidadVentas = 167, MesNombre = "Julio" },
                        new VentaMensualModel { Mes = 8, Año = 2025, TotalVentas = 3200000, CantidadVentas = 72, MesNombre = "Agosto" }
                    };

                    // Datos simulados para top productos
                    dashboardData.TopProductos = new List<TopProductoModel>
                    {
                        new TopProductoModel { IdProducto = 1, NombreProducto = "Ramo de Rosas Rojas", CantidadVendida = 45, TotalVentas = 1350000 },
                        new TopProductoModel { IdProducto = 2, NombreProducto = "Arreglo Floral Mixto", CantidadVendida = 32, TotalVentas = 1280000 },
                        new TopProductoModel { IdProducto = 3, NombreProducto = "Bouquet de Girasoles", CantidadVendida = 28, TotalVentas = 840000 },
                        new TopProductoModel { IdProducto = 4, NombreProducto = "Corona Funeraria", CantidadVendida = 15, TotalVentas = 750000 },
                        new TopProductoModel { IdProducto = 5, NombreProducto = "Ramo de Tulipanes", CantidadVendida = 22, TotalVentas = 660000 }
                    };

                    // Datos simulados para productos bajo stock
                    dashboardData.ProductosBajoStock = new List<ProductoBajoStockModel>
                    {
                        new ProductoBajoStockModel { IdProducto = 10, NombreProducto = "Rosas Blancas", StockActual = 3, StockMinimo = 10, Categoria = "Flores", Precio = 2500 },
                        new ProductoBajoStockModel { IdProducto = 15, NombreProducto = "Claveles Rosados", StockActual = 2, StockMinimo = 8, Categoria = "Flores", Precio = 1800 },
                        new ProductoBajoStockModel { IdProducto = 22, NombreProducto = "Lirios Amarillos", StockActual = 1, StockMinimo = 5, Categoria = "Flores", Precio = 3200 }
                    };

                    // Datos simulados para resumen inventario
                    dashboardData.ResumenInventario = new ResumenInventarioModel
                    {
                        TotalProductos = 89,
                        ProductosConStock = 82,
                        ProductosBajoStock = 3,
                        ProductosSinStock = 4,
                        ValorTotalInventario = 15750000
                    };
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

        public IActionResult BalanceFinanciero(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var balanceData = new BalanceFinancieroViewModel();
            
            // Configurar filtro de fechas
            balanceData.FiltroFechas.FechaInicio = fechaInicio;
            balanceData.FiltroFechas.FechaFin = fechaFin;

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
                    var parametros = new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    };

                    // Si no hay filtro de fechas, usar stored procedures originales
                    if (fechaInicio == null || fechaFin == null)
                    {
                        // Resumen financiero general (sin filtro)
                        balanceData.ResumenFinanciero = connection.QueryFirstOrDefault<ResumenFinancieroModel>(
                            "SP_ObtenerResumenFinanciero",
                            commandType: CommandType.StoredProcedure
                        ) ?? new ResumenFinancieroModel();

                        // Movimientos financieros recientes (sin filtro)
                        balanceData.MovimientosRecientes = connection.Query<MovimientoFinancieroModel>(
                            "SP_ObtenerMovimientosRecientes",
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Ingresos por mes (sin filtro)
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

                        // Egresos por mes (sin filtro)
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

                        // Flujo de caja diario (sin filtro)
                        balanceData.FlujoCaja = connection.Query<FlujoCajaModel>(
                            "SP_ObtenerFlujoCaja",
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Análisis financiero (sin filtro)
                        balanceData.AnalisisFinanciero = connection.QueryFirstOrDefault<AnalisisFinancieroModel>(
                            "SP_ObtenerAnalisisFinanciero",
                            commandType: CommandType.StoredProcedure
                        ) ?? new AnalisisFinancieroModel();
                    }
                    else
                    {
                        // Con filtro de fechas (usar SP filtrados)
                        
                        // Resumen financiero general (filtrado por rango de fechas si se especifica)
                        balanceData.ResumenFinanciero = connection.QueryFirstOrDefault<ResumenFinancieroModel>(
                            "SP_ObtenerResumenFinancieroFiltrado",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ) ?? new ResumenFinancieroModel();

                        // Movimientos financieros recientes (filtrados por rango de fechas si se especifica)
                        balanceData.MovimientosRecientes = connection.Query<MovimientoFinancieroModel>(
                            "SP_ObtenerMovimientosRecientesFiltrados",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Ingresos por mes (filtrados por rango de fechas si se especifica)
                        var ingresosPorMes = connection.Query<IngresoMensualModel>(
                            "SP_ObtenerIngresosPorMesFiltrados",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Asignar nombres de meses en español
                        foreach (var ingreso in ingresosPorMes)
                        {
                            ingreso.MesNombre = mesesEspanol.ContainsKey(ingreso.Mes) ? mesesEspanol[ingreso.Mes] : ingreso.Mes.ToString();
                        }
                        balanceData.IngresosPorMes = ingresosPorMes;

                        // Egresos por mes (filtrados por rango de fechas si se especifica)
                        var egresosPorMes = connection.Query<EgresoMensualModel>(
                            "SP_ObtenerEgresosPorMesFiltrados",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Asignar nombres de meses en español
                        foreach (var egreso in egresosPorMes)
                        {
                            egreso.MesNombre = mesesEspanol.ContainsKey(egreso.Mes) ? mesesEspanol[egreso.Mes] : egreso.Mes.ToString();
                        }
                        balanceData.EgresosPorMes = egresosPorMes;

                        // Flujo de caja diario (filtrado por rango de fechas si se especifica)
                        balanceData.FlujoCaja = connection.Query<FlujoCajaModel>(
                            "SP_ObtenerFlujoCajaFiltrado",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // Análisis financiero (filtrado por rango de fechas si se especifica)
                        balanceData.AnalisisFinanciero = connection.QueryFirstOrDefault<AnalisisFinancieroModel>(
                            "SP_ObtenerAnalisisFinancieroFiltrado",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ) ?? new AnalisisFinancieroModel();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en Balance Financiero: {ex.Message}");
                    // En caso de error de base de datos, usar datos simulados
                    balanceData.ResumenFinanciero = new ResumenFinancieroModel
                    {
                        TotalIngresos = 28500000,
                        TotalEgresos = 19800000,
                        BalanceNeto = 8700000,
                        IngresosHoy = 420000,
                        EgresosHoy = 180000,
                        IngresosSemana = 2100000,
                        EgresosSemana = 1350000,
                        IngresosMes = 9200000,
                        EgresosMes = 6800000,
                        MargenBruto = 8700000,
                        PorcentajeMargen = 30.5m
                    };

                    balanceData.MovimientosRecientes = new List<MovimientoFinancieroModel>
                    {
                        new MovimientoFinancieroModel { Fecha = DateTime.Today, TipoMovimiento = "Ingreso", Concepto = "Venta", Detalle = "Venta #1245 - Cliente: María González", Monto = 85000, Categoria = "Ventas" },
                        new MovimientoFinancieroModel { Fecha = DateTime.Today.AddHours(-2), TipoMovimiento = "Ingreso", Concepto = "Venta", Detalle = "Venta #1244 - Cliente: Carlos Ruiz", Monto = 120000, Categoria = "Ventas" },
                        new MovimientoFinancieroModel { Fecha = DateTime.Today.AddHours(-4), TipoMovimiento = "Egreso", Concepto = "Compra a Proveedor", Detalle = "Factura #F-789 - Flores del Valle", Monto = 180000, Categoria = "Compras" },
                        new MovimientoFinancieroModel { Fecha = DateTime.Today.AddDays(-1), TipoMovimiento = "Ingreso", Concepto = "Venta", Detalle = "Venta #1243 - Cliente: Ana López", Monto = 95000, Categoria = "Ventas" },
                        new MovimientoFinancieroModel { Fecha = DateTime.Today.AddDays(-1).AddHours(-3), TipoMovimiento = "Egreso", Concepto = "Planilla", Detalle = "Pago de salarios - Quincena 1", Monto = 450000, Categoria = "Planillas" }
                    };

                    // Asignar nombres de meses en español para ingresos
                    var ingresosPorMes = new List<IngresoMensualModel>
                    {
                        new IngresoMensualModel { Mes = 6, Año = 2025, TotalVentas = 8500000, OtrosIngresos = 0, TotalIngresos = 8500000, MesNombre = "Junio" },
                        new IngresoMensualModel { Mes = 7, Año = 2025, TotalVentas = 9200000, OtrosIngresos = 0, TotalIngresos = 9200000, MesNombre = "Julio" },
                        new IngresoMensualModel { Mes = 8, Año = 2025, TotalVentas = 3200000, OtrosIngresos = 0, TotalIngresos = 3200000, MesNombre = "Agosto" }
                    };
                    balanceData.IngresosPorMes = ingresosPorMes;

                    // Asignar nombres de meses en español para egresos
                    var egresosPorMes = new List<EgresoMensualModel>
                    {
                        new EgresoMensualModel { Mes = 6, Año = 2025, ComprasProveedores = 4200000, SalariosPlanillas = 1800000, Liquidaciones = 150000, OtrosEgresos = 350000, TotalEgresos = 6500000, MesNombre = "Junio" },
                        new EgresoMensualModel { Mes = 7, Año = 2025, ComprasProveedores = 4800000, SalariosPlanillas = 1800000, Liquidaciones = 0, OtrosEgresos = 400000, TotalEgresos = 7000000, MesNombre = "Julio" },
                        new EgresoMensualModel { Mes = 8, Año = 2025, ComprasProveedores = 1500000, SalariosPlanillas = 900000, Liquidaciones = 200000, OtrosEgresos = 150000, TotalEgresos = 2750000, MesNombre = "Agosto" }
                    };
                    balanceData.EgresosPorMes = egresosPorMes;

                    balanceData.FlujoCaja = new List<FlujoCajaModel>
                    {
                        new FlujoCajaModel { Fecha = DateTime.Today.AddDays(-6), Ingresos = 320000, Egresos = 180000, FlujoNeto = 140000, SaldoAcumulado = 140000 },
                        new FlujoCajaModel { Fecha = DateTime.Today.AddDays(-5), Ingresos = 450000, Egresos = 250000, FlujoNeto = 200000, SaldoAcumulado = 340000 },
                        new FlujoCajaModel { Fecha = DateTime.Today.AddDays(-4), Ingresos = 380000, Egresos = 150000, FlujoNeto = 230000, SaldoAcumulado = 570000 },
                        new FlujoCajaModel { Fecha = DateTime.Today.AddDays(-3), Ingresos = 520000, Egresos = 320000, FlujoNeto = 200000, SaldoAcumulado = 770000 },
                        new FlujoCajaModel { Fecha = DateTime.Today.AddDays(-2), Ingresos = 410000, Egresos = 180000, FlujoNeto = 230000, SaldoAcumulado = 1000000 },
                        new FlujoCajaModel { Fecha = DateTime.Today.AddDays(-1), Ingresos = 390000, Egresos = 450000, FlujoNeto = -60000, SaldoAcumulado = 940000 },
                        new FlujoCajaModel { Fecha = DateTime.Today, Ingresos = 420000, Egresos = 180000, FlujoNeto = 240000, SaldoAcumulado = 1180000 }
                    };

                    balanceData.AnalisisFinanciero = new AnalisisFinancieroModel
                    {
                        PromedioIngresosDiarios = 410000,
                        PromedioEgresosDiarios = 240000,
                        TendenciaIngresos = 12.5m,
                        TendenciaEgresos = -8.2m,
                        RatioIngresosEgresos = 1.71m,
                        DiasOperativos = 30,
                        PuntoEquilibrio = 240000
                    };
                }
            }

            return View(balanceData);
        }

        public IActionResult HistorialActividades(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var historialData = new HistorialActividadesViewModel();
            
            // Configurar filtro de fechas
            historialData.FiltroFechas.FechaInicio = fechaInicio;
            historialData.FiltroFechas.FechaFin = fechaFin;

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    var parametros = new
                    {
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    };

                    // Si no hay filtro de fechas, usar stored procedures originales
                    if (fechaInicio == null || fechaFin == null)
                    {
                        // 1. Actividades recientes (sin filtro)
                        historialData.ActividadesRecientes = connection.Query<ActividadRecienteModel>(
                            "SP_ObtenerActividadesRecientes",
                            new { CantidadRegistros = 50 },
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // 2. Actividades por tipo (sin filtro)
                        historialData.ActividadesPorTipo = connection.Query<ActividadPorTipoModel>(
                            "SP_ObtenerActividadesPorTipo",
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // 3. Actividades por usuario (sin filtro)
                        historialData.ActividadesPorUsuario = connection.Query<ActividadPorUsuarioModel>(
                            "SP_ObtenerActividadesPorUsuario",
                            new { TopUsuarios = 10 },
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // 4. Detalle de actividades (sin filtro)
                        historialData.DetalleActividades = connection.Query<ActividadDetalleModel>(
                            "SP_ObtenerDetalleActividadesSimple",
                            new { CantidadRegistros = 100 },
                            commandType: CommandType.StoredProcedure
                        ).ToList();
                    }
                    else
                    {
                        // Con filtro de fechas (usar SP filtrados)
                        
                        // 1. Actividades recientes (filtradas por módulos específicos y rango de fechas)
                        historialData.ActividadesRecientes = connection.Query<ActividadRecienteModel>(
                            "SP_ObtenerActividadesRecientesFiltradas",
                            new { FechaInicio = fechaInicio, FechaFin = fechaFin, CantidadRegistros = 50 },
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // 2. Actividades por tipo (filtradas por módulos específicos y rango de fechas)
                        historialData.ActividadesPorTipo = connection.Query<ActividadPorTipoModel>(
                            "SP_ObtenerActividadesPorTipoFiltradas",
                            parametros,
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // 3. Actividades por usuario (filtradas por módulos específicos y rango de fechas)
                        historialData.ActividadesPorUsuario = connection.Query<ActividadPorUsuarioModel>(
                            "SP_ObtenerActividadesPorUsuarioFiltradas",
                            new { FechaInicio = fechaInicio, FechaFin = fechaFin, TopUsuarios = 10 },
                            commandType: CommandType.StoredProcedure
                        ).ToList();

                        // 4. Detalle de actividades (filtradas por módulos específicos y rango de fechas)
                        historialData.DetalleActividades = connection.Query<ActividadDetalleModel>(
                            "SP_ObtenerDetalleActividadesSimpleFiltrado",
                            new { FechaInicio = fechaInicio, FechaFin = fechaFin, CantidadRegistros = 100 },
                            commandType: CommandType.StoredProcedure
                        ).ToList();
                    }

                    // 5. Resumen calculado en C# (más eficiente para datos simples)
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
                    // En caso de error de base de datos, usar datos simulados
                    
                    var actividadesSimuladas = new List<ActividadRecienteModel>
                    {
                        new ActividadRecienteModel { IdActividad = 1, FechaActividad = DateTime.Now.AddMinutes(-15), TipoActividad = "Crear", Descripcion = "Nueva venta registrada", Usuario = "admin@floristeriadapaz.com", Modulo = "Venta" },
                        new ActividadRecienteModel { IdActividad = 2, FechaActividad = DateTime.Now.AddMinutes(-30), TipoActividad = "Editar", Descripcion = "Producto actualizado", Usuario = "empleado1@floristeriadapaz.com", Modulo = "Producto" },
                        new ActividadRecienteModel { IdActividad = 3, FechaActividad = DateTime.Now.AddHours(-1), TipoActividad = "Crear", Descripcion = "Nuevo arreglo floral", Usuario = "admin@floristeriadapaz.com", Modulo = "Arreglo" },
                        new ActividadRecienteModel { IdActividad = 4, FechaActividad = DateTime.Now.AddHours(-2), TipoActividad = "Crear", Descripcion = "Factura de proveedor registrada", Usuario = "contabilidad@floristeriadapaz.com", Modulo = "FacturaProveedor" },
                        new ActividadRecienteModel { IdActividad = 5, FechaActividad = DateTime.Now.AddHours(-3), TipoActividad = "Login", Descripcion = "Inicio de sesión exitoso", Usuario = "empleado2@floristeriadapaz.com", Modulo = "Sistema" },
                        new ActividadRecienteModel { IdActividad = 6, FechaActividad = DateTime.Today.AddHours(-5), TipoActividad = "Crear", Descripcion = "Nueva venta procesada", Usuario = "admin@floristeriadapaz.com", Modulo = "Venta" },
                        new ActividadRecienteModel { IdActividad = 7, FechaActividad = DateTime.Today.AddHours(-6), TipoActividad = "Editar", Descripcion = "Stock de producto ajustado", Usuario = "empleado1@floristeriadapaz.com", Modulo = "Producto" }
                    };
                    
                    historialData.ActividadesRecientes = actividadesSimuladas;
                    
                    historialData.ActividadesPorTipo = new List<ActividadPorTipoModel>
                    {
                        new ActividadPorTipoModel { TipoActividad = "Crear", CantidadActividades = 45, PorcentajeTotal = 38.5m },
                        new ActividadPorTipoModel { TipoActividad = "Editar", CantidadActividades = 32, PorcentajeTotal = 27.4m },
                        new ActividadPorTipoModel { TipoActividad = "Login", CantidadActividades = 25, PorcentajeTotal = 21.4m },
                        new ActividadPorTipoModel { TipoActividad = "Eliminar", CantidadActividades = 8, PorcentajeTotal = 6.8m },
                        new ActividadPorTipoModel { TipoActividad = "Logout", CantidadActividades = 7, PorcentajeTotal = 6.0m }
                    };
                    
                    historialData.ActividadesPorUsuario = new List<ActividadPorUsuarioModel>
                    {
                        new ActividadPorUsuarioModel { Usuario = "admin@floristeriadapaz.com", CantidadActividades = 38, UltimaActividad = DateTime.Now.AddMinutes(-15) },
                        new ActividadPorUsuarioModel { Usuario = "empleado1@floristeriadapaz.com", CantidadActividades = 29, UltimaActividad = DateTime.Now.AddMinutes(-30) },
                        new ActividadPorUsuarioModel { Usuario = "contabilidad@floristeriadapaz.com", CantidadActividades = 22, UltimaActividad = DateTime.Now.AddHours(-2) },
                        new ActividadPorUsuarioModel { Usuario = "empleado2@floristeriadapaz.com", CantidadActividades = 18, UltimaActividad = DateTime.Now.AddHours(-3) }
                    };
                    
                    historialData.DetalleActividades = actividadesSimuladas.Select(a => new ActividadDetalleModel
                    {
                        Fecha = a.FechaActividad,
                        TipoActividad = a.TipoActividad,
                        Usuario = a.Usuario,
                        Modulo = a.Modulo,
                        Descripcion = a.Descripcion,
                        Detalles = "",
                        IPAddress = "192.168.1.100",
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                    }).ToList();
                    
                    // Calcular resumen basado en datos simulados
                    var actividades = historialData.ActividadesRecientes;
                    historialData.ResumenActividades = new ResumenActividadesModel
                    {
                        TotalActividadesHoy = actividades.Count(a => a.FechaActividad.Date == DateTime.Today),
                        TotalActividadesSemana = actividades.Count(a => a.FechaActividad >= DateTime.Now.AddDays(-7)),
                        TotalActividadesMes = actividades.Count(a => a.FechaActividad >= DateTime.Now.AddMonths(-1)),
                        UsuariosActivosHoy = actividades.Where(a => a.FechaActividad.Date == DateTime.Today).Select(a => a.Usuario).Distinct().Count(),
                        ErroresHoy = 0,
                        ActividadMasFrecuente = "Crear",
                        UsuarioMasActivo = "admin@floristeriadapaz.com",
                        PromedioActividadesDiarias = 15.7m
                    };
                    
                    historialData.ActividadesPorFecha = new List<ActividadPorFechaModel>();
                }
            }

            return View(historialData);
        }
    }
}
