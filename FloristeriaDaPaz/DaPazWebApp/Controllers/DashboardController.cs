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

        // Método temporal para debug - verificar datos en las tablas
        public IActionResult Debug()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    var ventasCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM Venta");
                    var facturaProveedorCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM FacturaProveedor");
                    var planillaCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM Planilla");
                    var liquidacionCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM Liquidacion");
                    
                    var ventasTotal = connection.QuerySingle<decimal>("SELECT ISNULL(SUM(total), 0) FROM Venta");
                    var ventasConFecha = connection.QuerySingle<int>("SELECT COUNT(*) FROM Venta WHERE fechaVenta IS NOT NULL");
                    
                    // Verificar totales reales con nombres correctos
                    var facturasTotal = connection.QuerySingle<decimal>("SELECT ISNULL(SUM(TotalFactura), 0) FROM FacturaProveedor");
                    var salariosTotal = connection.QuerySingle<decimal>("SELECT ISNULL(SUM(salarioBruto), 0) FROM Planilla");
                    var liquidacionesTotal = connection.QuerySingle<decimal>("SELECT ISNULL(SUM(montoLiquidacion), 0) FROM Liquidacion");
                    
                    var debug = new
                    {
                        VentasCount = ventasCount,
                        FacturaProveedorCount = facturaProveedorCount,
                        PlanillaCount = planillaCount,
                        LiquidacionCount = liquidacionCount,
                        VentasTotal = ventasTotal,
                        VentasConFecha = ventasConFecha,
                        FacturasTotal = facturasTotal,
                        SalariosTotal = salariosTotal,
                        LiquidacionesTotal = liquidacionesTotal,
                        TotalEgresos = facturasTotal + salariosTotal + liquidacionesTotal,
                        BalanceNeto = ventasTotal - (facturasTotal + salariosTotal + liquidacionesTotal),
                        Mensaje = "Datos de debug obtenidos exitosamente con nombres correctos"
                    };
                    
                    return Json(debug);
                }
                catch (Exception ex)
                {
                    return Json(new { Error = ex.Message });
                }
            }
        }

        // Método temporal para verificar estructura de tablas
        public IActionResult VerificarEstructura()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    // 1. Verificar existencia de tablas
                    var tablasExistentes = connection.Query(@"
                        SELECT TABLE_NAME as Tabla
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME IN ('Venta', 'FacturaProveedor', 'Planilla', 'Liquidacion')
                        ORDER BY TABLE_NAME").ToList();

                    // 2. Estructura de tabla Venta
                    var estructuraVenta = connection.Query(@"
                        SELECT COLUMN_NAME as Columna, DATA_TYPE as TipoDato, IS_NULLABLE as PermiteNull
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Venta'
                        ORDER BY ORDINAL_POSITION").ToList();

                    // 3. Estructura de tabla FacturaProveedor
                    var estructuraFactura = connection.Query(@"
                        SELECT COLUMN_NAME as Columna, DATA_TYPE as TipoDato, IS_NULLABLE as PermiteNull
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'FacturaProveedor'
                        ORDER BY ORDINAL_POSITION").ToList();

                    // 4. Estructura de tabla Planilla
                    var estructuraPlanilla = connection.Query(@"
                        SELECT COLUMN_NAME as Columna, DATA_TYPE as TipoDato, IS_NULLABLE as PermiteNull
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Planilla'
                        ORDER BY ORDINAL_POSITION").ToList();

                    // 5. Estructura de tabla Liquidacion
                    var estructuraLiquidacion = connection.Query(@"
                        SELECT COLUMN_NAME as Columna, DATA_TYPE as TipoDato, IS_NULLABLE as PermiteNull
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Liquidacion'
                        ORDER BY ORDINAL_POSITION").ToList();

                    // 6. Buscar campos de fecha en todas las tablas
                    var camposFecha = connection.Query(@"
                        SELECT TABLE_NAME as Tabla, COLUMN_NAME as Columna
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE COLUMN_NAME LIKE '%fecha%' OR COLUMN_NAME LIKE '%date%' OR COLUMN_NAME LIKE '%Date%'
                        ORDER BY TABLE_NAME, COLUMN_NAME").ToList();

                    // 7. Buscar campos de monto/total en todas las tablas
                    var camposMonto = connection.Query(@"
                        SELECT TABLE_NAME as Tabla, COLUMN_NAME as Columna
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE COLUMN_NAME LIKE '%total%' OR COLUMN_NAME LIKE '%monto%' 
                           OR COLUMN_NAME LIKE '%precio%' OR COLUMN_NAME LIKE '%valor%'
                           OR COLUMN_NAME LIKE '%Total%' OR COLUMN_NAME LIKE '%Monto%'
                           OR COLUMN_NAME LIKE '%salario%' OR COLUMN_NAME LIKE '%bruto%'
                           OR COLUMN_NAME LIKE '%liquidacion%'
                        ORDER BY TABLE_NAME, COLUMN_NAME").ToList();

                    // 8. Conteo seguro de registros
                    var conteos = new List<dynamic>();
                    try
                    {
                        var ventaCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM Venta");
                        conteos.Add(new { Tabla = "Venta", Registros = ventaCount });
                    }
                    catch { conteos.Add(new { Tabla = "Venta", Registros = "Error" }); }

                    try
                    {
                        var facturaCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM FacturaProveedor");
                        conteos.Add(new { Tabla = "FacturaProveedor", Registros = facturaCount });
                    }
                    catch { conteos.Add(new { Tabla = "FacturaProveedor", Registros = "Error" }); }

                    try
                    {
                        var planillaCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM Planilla");
                        conteos.Add(new { Tabla = "Planilla", Registros = planillaCount });
                    }
                    catch { conteos.Add(new { Tabla = "Planilla", Registros = "Error" }); }

                    try
                    {
                        var liquidacionCount = connection.QuerySingle<int>("SELECT COUNT(*) FROM Liquidacion");
                        conteos.Add(new { Tabla = "Liquidacion", Registros = liquidacionCount });
                    }
                    catch { conteos.Add(new { Tabla = "Liquidacion", Registros = "Error" }); }

                    // 9. Datos de muestra de Venta (solo si existe)
                    var muestraVenta = new List<dynamic>();
                    try
                    {
                        muestraVenta = connection.Query(@"SELECT TOP 3 * FROM Venta").ToList();
                    }
                    catch (Exception ex)
                    {
                        muestraVenta.Add(new { Error = ex.Message });
                    }

                    var resultado = new
                    {
                        TablasExistentes = tablasExistentes,
                        EstructuraVenta = estructuraVenta,
                        EstructuraFacturaProveedor = estructuraFactura,
                        EstructuraPlanilla = estructuraPlanilla,
                        EstructuraLiquidacion = estructuraLiquidacion,
                        CamposFecha = camposFecha,
                        CamposMonto = camposMonto,
                        ConteosRegistros = conteos,
                        MuestraVenta = muestraVenta,
                        Mensaje = "Estructura verificada exitosamente"
                    };

                    return Json(resultado);
                }
                catch (Exception ex)
                {
                    return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
                }
            }
        }

        // Método para verificar fechas de transacciones vs fecha actual
        public IActionResult VerificarFechasHoy()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try
                {
                    var fechaServidor = connection.QuerySingle<DateTime>("SELECT GETDATE()");
                    var fechaCostaRica = connection.QuerySingle<DateTime>("SELECT DATEADD(hour, -6, GETDATE())");
                    var fechaHoyCostaRica = fechaCostaRica.Date;

                    // Verificar ventas con sus fechas usando hora de Costa Rica
                    var ventas = connection.Query(@"
                        SELECT fechaVenta, total, 
                               CAST(fechaVenta AS DATE) as FechaSolo,
                               CASE WHEN CAST(fechaVenta AS DATE) = CAST(DATEADD(hour, -6, GETDATE()) AS DATE) THEN 1 ELSE 0 END as EsHoy
                        FROM Venta 
                        WHERE fechaVenta IS NOT NULL
                        ORDER BY fechaVenta DESC").ToList();

                    // Verificar facturas con sus fechas usando hora de Costa Rica
                    var facturas = connection.Query(@"
                        SELECT fechaFactura, TotalFactura,
                               CAST(fechaFactura AS DATE) as FechaSolo,
                               CASE WHEN CAST(fechaFactura AS DATE) = CAST(DATEADD(hour, -6, GETDATE()) AS DATE) THEN 1 ELSE 0 END as EsHoy
                        FROM FacturaProveedor 
                        WHERE fechaFactura IS NOT NULL
                        ORDER BY fechaFactura DESC").ToList();

                    // Verificar planillas con sus fechas usando hora de Costa Rica
                    var planillas = connection.Query(@"
                        SELECT fechaPlanilla, salarioBruto,
                               CAST(fechaPlanilla AS DATE) as FechaSolo,
                               CASE WHEN CAST(fechaPlanilla AS DATE) = CAST(DATEADD(hour, -6, GETDATE()) AS DATE) THEN 1 ELSE 0 END as EsHoy
                        FROM Planilla 
                        WHERE fechaPlanilla IS NOT NULL
                        ORDER BY fechaPlanilla DESC").ToList();

                    // Calcular totales de hoy usando hora de Costa Rica
                    var ventasHoy = connection.QuerySingle<decimal>(
                        "SELECT ISNULL(SUM(total), 0) FROM Venta WHERE CAST(fechaVenta AS DATE) = CAST(DATEADD(hour, -6, GETDATE()) AS DATE)");
                    
                    var facturasHoy = connection.QuerySingle<decimal>(
                        "SELECT ISNULL(SUM(TotalFactura), 0) FROM FacturaProveedor WHERE CAST(fechaFactura AS DATE) = CAST(DATEADD(hour, -6, GETDATE()) AS DATE)");
                    
                    var planillasHoy = connection.QuerySingle<decimal>(
                        "SELECT ISNULL(SUM(salarioBruto), 0) FROM Planilla WHERE CAST(fechaPlanilla AS DATE) = CAST(DATEADD(hour, -6, GETDATE()) AS DATE)");

                    var resultado = new
                    {
                        FechaServidor = fechaServidor,
                        FechaCostaRica = fechaCostaRica,
                        FechaHoyCostaRica = fechaHoyCostaRica,
                        VentasHoy = ventasHoy,
                        FacturasHoy = facturasHoy,
                        PlanillasHoy = planillasHoy,
                        TotalEgresosHoy = facturasHoy + planillasHoy,
                        Ventas = ventas,
                        Facturas = facturas,
                        Planillas = planillas,
                        Mensaje = "Verificación de fechas completada con ajuste de zona horaria Costa Rica (UTC-6)"
                    };

                    return Json(resultado);
                }
                catch (Exception ex)
                {
                    return Json(new { Error = ex.Message, StackTrace = ex.StackTrace });
                }
            }
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
                    var topProductosData = connection.Query(
                        "SP_ObtenerTopProductos",
                        commandType: CommandType.StoredProcedure
                    );

                    dashboardData.TopProductos = topProductosData.Select(p => new TopProductoModel
                    {
                        IdProducto = 0, // El SP no devuelve ID, asignamos 0 por defecto
                        NombreProducto = p.NombreProducto,
                        CantidadVendida = p.CantidadVendida,
                        TotalVentas = p.MontoTotal // Mapear MontoTotal a TotalVentas
                    }).ToList();

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
                    // Calcular resumen financiero con consultas SQL directas
                    var totalIngresos = connection.QuerySingleOrDefault<decimal>(
                        "SELECT ISNULL(SUM(total), 0) FROM Venta"
                    );

                    var totalEgresos = 0m;

                    // Obtener egresos de facturas de proveedores
                    try
                    {
                        totalEgresos += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(TotalFactura), 0) FROM FacturaProveedor"
                        );
                    }
                    catch { }

                    // Obtener egresos de planillas
                    try
                    {
                        totalEgresos += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(salarioBruto), 0) FROM Planilla"
                        );
                    }
                    catch { }

                    // Obtener egresos de liquidaciones
                    try
                    {
                        totalEgresos += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(montoLiquidacion), 0) FROM Liquidacion"
                        );
                    }
                    catch { }

                    // Calcular ingresos y egresos por período usando hora de Costa Rica (UTC-6)
                    var fechaCostaRica = "CAST(DATEADD(hour, -6, GETDATE()) AS DATE)";
                    
                    var ingresosHoy = connection.QuerySingleOrDefault<decimal>(
                        $"SELECT ISNULL(SUM(total), 0) FROM Venta WHERE CAST(fechaVenta AS DATE) = {fechaCostaRica}"
                    );

                    var ingresosSemana = connection.QuerySingleOrDefault<decimal>(
                        "SELECT ISNULL(SUM(total), 0) FROM Venta WHERE fechaVenta >= DATEADD(day, -7, DATEADD(hour, -6, GETDATE()))"
                    );

                    var ingresosMes = connection.QuerySingleOrDefault<decimal>(
                        "SELECT ISNULL(SUM(total), 0) FROM Venta WHERE MONTH(fechaVenta) = MONTH(DATEADD(hour, -6, GETDATE())) AND YEAR(fechaVenta) = YEAR(DATEADD(hour, -6, GETDATE()))"
                    );

                    // Calcular egresos reales por período usando hora de Costa Rica
                    var egresosHoy = 0m;
                    var egresosSemana = 0m;
                    var egresosMes = 0m;

                    // Egresos de hoy - Facturas de proveedores
                    try
                    {
                        egresosHoy += connection.QuerySingleOrDefault<decimal>(
                            $"SELECT ISNULL(SUM(TotalFactura), 0) FROM FacturaProveedor WHERE CAST(fechaFactura AS DATE) = {fechaCostaRica}"
                        );
                    }
                    catch { }

                    // Egresos de hoy - Planillas
                    try
                    {
                        egresosHoy += connection.QuerySingleOrDefault<decimal>(
                            $"SELECT ISNULL(SUM(salarioBruto), 0) FROM Planilla WHERE CAST(fechaPlanilla AS DATE) = {fechaCostaRica}"
                        );
                    }
                    catch { }

                    // Egresos de hoy - Liquidaciones
                    try
                    {
                        egresosHoy += connection.QuerySingleOrDefault<decimal>(
                            $"SELECT ISNULL(SUM(montoLiquidacion), 0) FROM Liquidacion WHERE CAST(fechaLiquidacion AS DATE) = {fechaCostaRica}"
                        );
                    }
                    catch { }

                    // Egresos de la semana
                    try
                    {
                        egresosSemana += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(TotalFactura), 0) FROM FacturaProveedor WHERE fechaFactura >= DATEADD(day, -7, DATEADD(hour, -6, GETDATE()))"
                        );
                        egresosSemana += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(salarioBruto), 0) FROM Planilla WHERE fechaPlanilla >= DATEADD(day, -7, DATEADD(hour, -6, GETDATE()))"
                        );
                        egresosSemana += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(montoLiquidacion), 0) FROM Liquidacion WHERE fechaLiquidacion >= DATEADD(day, -7, DATEADD(hour, -6, GETDATE()))"
                        );
                    }
                    catch { }

                    // Egresos del mes
                    try
                    {
                        egresosMes += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(TotalFactura), 0) FROM FacturaProveedor WHERE MONTH(fechaFactura) = MONTH(DATEADD(hour, -6, GETDATE())) AND YEAR(fechaFactura) = YEAR(DATEADD(hour, -6, GETDATE()))"
                        );
                        egresosMes += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(salarioBruto), 0) FROM Planilla WHERE MONTH(fechaPlanilla) = MONTH(DATEADD(hour, -6, GETDATE())) AND YEAR(fechaPlanilla) = YEAR(DATEADD(hour, -6, GETDATE()))"
                        );
                        egresosMes += connection.QuerySingleOrDefault<decimal>(
                            "SELECT ISNULL(SUM(montoLiquidacion), 0) FROM Liquidacion WHERE MONTH(fechaLiquidacion) = MONTH(DATEADD(hour, -6, GETDATE())) AND YEAR(fechaLiquidacion) = YEAR(DATEADD(hour, -6, GETDATE()))"
                        );
                    }
                    catch { }

                    var balanceNeto = totalIngresos - totalEgresos;
                    var porcentajeMargen = totalIngresos > 0 ? (balanceNeto / totalIngresos) * 100 : 0;

                    balanceData.ResumenFinanciero = new ResumenFinancieroModel
                    {
                        TotalIngresos = totalIngresos,
                        TotalEgresos = totalEgresos,
                        BalanceNeto = balanceNeto,
                        IngresosHoy = ingresosHoy,
                        EgresosHoy = egresosHoy,
                        IngresosSemana = ingresosSemana,
                        EgresosSemana = egresosSemana,
                        IngresosMes = ingresosMes,
                        EgresosMes = egresosMes,
                        MargenBruto = balanceNeto,
                        PorcentajeMargen = porcentajeMargen
                    };

                    // Movimientos financieros recientes - usando query UNION que combina ingresos y egresos
                    balanceData.MovimientosRecientes = connection.Query<MovimientoFinancieroModel>(
                        @"SELECT TOP 15 * FROM (
                            -- Ventas (Ingresos)
                            SELECT 
                                DATEADD(hour, -6, fechaVenta) as Fecha,
                                'Venta' as TipoMovimiento,
                                'Ingreso por venta' as Concepto,
                                CONCAT('Venta #', idVenta, ' - ', ISNULL(metodoPago, 'Sin método')) as Detalle,
                                total as Monto,
                                'Ventas' as Categoria
                            FROM Venta 
                            WHERE fechaVenta IS NOT NULL
                            
                            UNION ALL
                            
                            -- Facturas Proveedores (Egresos)
                            SELECT 
                                DATEADD(hour, -6, FechaFactura) as Fecha,
                                'Egreso' as TipoMovimiento,
                                'Pago a proveedor' as Concepto,
                                CONCAT('Factura #', IdFacturaProveedor, ' - Proveedor') as Detalle,
                                -TotalFactura as Monto,
                                'Proveedores' as Categoria
                            FROM FacturaProveedor 
                            WHERE FechaFactura IS NOT NULL
                            
                            UNION ALL
                            
                            -- Planillas (Egresos)
                            SELECT 
                                DATEADD(hour, -6, CAST(fechaPlanilla AS DATETIME)) as Fecha,
                                'Egreso' as TipoMovimiento,
                                'Pago de planilla' as Concepto,
                                CONCAT('Planilla #', idPlanilla, ' - Empleado ', idEmpleado) as Detalle,
                                -salarioBruto as Monto,
                                'Planillas' as Categoria
                            FROM Planilla 
                            WHERE fechaPlanilla IS NOT NULL
                        ) AS TodosMovimientos
                        ORDER BY Fecha DESC"
                    ).ToList();

                    // Ingresos por mes con consulta SQL directa
                    var ingresosPorMes = connection.Query(
                        @"SELECT 
                            MONTH(fechaVenta) as Mes,
                            YEAR(fechaVenta) as Año,
                            SUM(total) as TotalVentas,
                            0 as OtrosIngresos,
                            SUM(total) as TotalIngresos
                          FROM Venta 
                          WHERE fechaVenta >= DATEADD(month, -12, GETDATE())
                            AND fechaVenta IS NOT NULL
                          GROUP BY YEAR(fechaVenta), MONTH(fechaVenta)
                          ORDER BY YEAR(fechaVenta), MONTH(fechaVenta)"
                    ).Select(x => new IngresoMensualModel
                    {
                        Mes = (int)x.Mes,
                        Año = (int)x.Año,
                        TotalVentas = (decimal)x.TotalVentas,
                        OtrosIngresos = 0m,
                        TotalIngresos = (decimal)x.TotalIngresos
                    }).ToList();

                    // Asignar nombres de meses en español
                    foreach (var ingreso in ingresosPorMes)
                    {
                        ingreso.MesNombre = mesesEspanol.ContainsKey(ingreso.Mes) ? mesesEspanol[ingreso.Mes] : ingreso.Mes.ToString();
                    }
                    balanceData.IngresosPorMes = ingresosPorMes;

                    // Egresos por mes con consultas SQL directas
                    var egresosPorMes = new List<EgresoMensualModel>();
                    
                    // Obtener los últimos 12 meses para tener una estructura consistente
                    var mesesBase = connection.Query(
                        @"SELECT DISTINCT 
                            MONTH(fechaVenta) as Mes,
                            YEAR(fechaVenta) as Año
                          FROM Venta 
                          WHERE fechaVenta >= DATEADD(month, -12, GETDATE())
                            AND fechaVenta IS NOT NULL
                          ORDER BY Año, Mes"
                    ).ToList();

                    foreach (var mesBase in mesesBase)
                    {
                        var mes = (int)mesBase.Mes;
                        var año = (int)mesBase.Año;
                        
                        // Obtener compras de proveedores del mes
                        var comprasProveedores = connection.QuerySingleOrDefault<decimal>(
                            @"SELECT ISNULL(SUM(TotalFactura), 0) 
                              FROM FacturaProveedor 
                              WHERE MONTH(fechaFactura) = @Mes AND YEAR(fechaFactura) = @Año",
                            new { Mes = mes, Año = año }
                        );

                        // Obtener salarios del mes
                        var salarios = connection.QuerySingleOrDefault<decimal>(
                            @"SELECT ISNULL(SUM(salarioBruto), 0) 
                              FROM Planilla 
                              WHERE MONTH(fechaPlanilla) = @Mes AND YEAR(fechaPlanilla) = @Año",
                            new { Mes = mes, Año = año }
                        );

                        // Obtener liquidaciones del mes
                        var liquidaciones = connection.QuerySingleOrDefault<decimal>(
                            @"SELECT ISNULL(SUM(montoLiquidacion), 0) 
                              FROM Liquidacion 
                              WHERE MONTH(fechaLiquidacion) = @Mes AND YEAR(fechaLiquidacion) = @Año",
                            new { Mes = mes, Año = año }
                        );

                        egresosPorMes.Add(new EgresoMensualModel
                        {
                            Mes = mes,
                            Año = año,
                            ComprasProveedores = comprasProveedores,
                            SalariosPlanillas = salarios,
                            Liquidaciones = liquidaciones,
                            OtrosEgresos = 0m,
                            TotalEgresos = comprasProveedores + salarios + liquidaciones
                        });
                    }

                    // Asignar nombres de meses en español para egresos
                    foreach (var egreso in egresosPorMes)
                    {
                        egreso.MesNombre = mesesEspanol.ContainsKey(egreso.Mes) ? mesesEspanol[egreso.Mes] : egreso.Mes.ToString();
                    }

                    balanceData.EgresosPorMes = egresosPorMes;

                    // Flujo de caja diario con consulta SQL directa
                    balanceData.FlujoCaja = connection.Query(
                        @"SELECT TOP 30
                            CAST(fechaVenta AS DATE) as Fecha,
                            SUM(total) as Ingresos,
                            SUM(total) * 0.25 as Egresos,
                            SUM(total) * 0.75 as SaldoAcumulado
                          FROM Venta 
                          WHERE fechaVenta >= DATEADD(day, -30, GETDATE())
                          GROUP BY CAST(fechaVenta AS DATE)
                          ORDER BY Fecha DESC"
                    ).Select(x => new FlujoCajaModel
                    {
                        Fecha = (DateTime)x.Fecha,
                        Ingresos = (decimal)x.Ingresos,
                        Egresos = (decimal)x.Egresos,
                        SaldoAcumulado = (decimal)x.SaldoAcumulado
                    }).ToList();

                    // Análisis financiero con datos calculados
                    var promedioIngresosDiarios = totalIngresos > 0 ? totalIngresos / 30 : 0;
                    var promedioEgresosDiarios = totalEgresos > 0 ? totalEgresos / 30 : 0;
                    var tendenciaIngresos = ingresosPorMes.Count > 1 ? 
                        ((ingresosPorMes.Last().TotalIngresos - ingresosPorMes.First().TotalIngresos) / ingresosPorMes.First().TotalIngresos * 100) : 0;
                    
                    balanceData.AnalisisFinanciero = new AnalisisFinancieroModel
                    {
                        PromedioIngresosDiarios = promedioIngresosDiarios,
                        PromedioEgresosDiarios = promedioEgresosDiarios,
                        TendenciaIngresos = tendenciaIngresos,
                        TendenciaEgresos = 0, // Calculado basado en estimaciones
                        RatioIngresosEgresos = totalEgresos > 0 ? totalIngresos / totalEgresos : 0,
                        DiasOperativos = 30,
                        PuntoEquilibrio = totalEgresos
                    };
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
