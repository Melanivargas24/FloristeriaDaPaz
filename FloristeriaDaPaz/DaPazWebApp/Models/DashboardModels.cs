namespace DaPazWebApp.Models
{
    // Modelo para filtros de fecha
    public class FiltroFechasModel
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool AplicarFiltro => FechaInicio.HasValue && FechaFin.HasValue;
        
        public string FechaInicioFormateada => FechaInicio?.ToString("yyyy-MM-dd") ?? "";
        public string FechaFinFormateada => FechaFin?.ToString("yyyy-MM-dd") ?? "";
    }

    public class DashboardViewModel
    {
        public List<VentaDiariaModel> VentasDiarias { get; set; } = new List<VentaDiariaModel>();
        public List<VentaSemanalModel> VentasSemanales { get; set; } = new List<VentaSemanalModel>();
        public List<VentaMensualModel> VentasMensuales { get; set; } = new List<VentaMensualModel>();
        public EstadisticasGeneralesModel EstadisticasGenerales { get; set; } = new EstadisticasGeneralesModel();
        public List<TopProductoModel> TopProductos { get; set; } = new List<TopProductoModel>();
        public List<ProductoBajoStockModel> ProductosBajoStock { get; set; } = new List<ProductoBajoStockModel>();
        public ResumenInventarioModel ResumenInventario { get; set; } = new ResumenInventarioModel();
        public FiltroFechasModel FiltroFechas { get; set; } = new FiltroFechasModel();
    }

    public class VentaDiariaModel
    {
        public DateTime Fecha { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
    }

    public class VentaSemanalModel
    {
        public int Semana { get; set; }
        public int Año { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        public string PeriodoFormateado => $"Semana {Semana} - {Año}";
    }

    public class VentaMensualModel
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        public string MesNombre { get; set; } = "";
        public string PeriodoFormateado => $"{MesNombre} {Año}";
    }

    public class EstadisticasGeneralesModel
    {
        public decimal VentasHoy { get; set; }
        public decimal VentasSemana { get; set; }
        public decimal VentasMes { get; set; }
        public decimal VentasAño { get; set; }
        public int TotalClientes { get; set; }
        public int TotalProductos { get; set; }
        public int VentasHoyCount { get; set; }
        public decimal PromedioVentaDiaria { get; set; }
    }

    public class TopProductoModel
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public int CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
    }

    public class ProductoBajoStockModel
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = "";
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
        public string Categoria { get; set; } = "";
        public decimal Precio { get; set; }
        public string NivelAlerta => StockActual == 0 ? "Sin Stock" : "Bajo Stock";
        public decimal ValorStock => StockActual * Precio;
    }

    public class ResumenInventarioModel
    {
        public int TotalProductos { get; set; }
        public int ProductosSinStock { get; set; }
        public int ProductosBajoStock { get; set; }
        public int ProductosConStock { get; set; }
        public decimal ValorTotalInventario { get; set; }
    }

    public class InventarioViewModel
    {
        public ResumenInventarioModel ResumenInventario { get; set; } = new ResumenInventarioModel();
        public List<ProductoBajoStockModel> ProductosBajoStock { get; set; } = new List<ProductoBajoStockModel>();
        public List<ProductoBajoStockModel> ProductosSinStock { get; set; } = new List<ProductoBajoStockModel>();
        public List<InventarioPorCategoriaModel> ProductosPorCategoria { get; set; } = new List<InventarioPorCategoriaModel>();
    }

    public class InventarioPorCategoriaModel
    {
        public string Categoria { get; set; } = "";
        public int TotalProductos { get; set; }
        public int TotalStock { get; set; }
        public decimal ValorCategoria { get; set; }
    }

    // Modelos para el Dashboard de Balance Financiero
    public class BalanceFinancieroViewModel
    {
        public ResumenFinancieroModel ResumenFinanciero { get; set; } = new ResumenFinancieroModel();
        public List<MovimientoFinancieroModel> MovimientosRecientes { get; set; } = new List<MovimientoFinancieroModel>();
        public List<IngresoMensualModel> IngresosPorMes { get; set; } = new List<IngresoMensualModel>();
        public List<EgresoMensualModel> EgresosPorMes { get; set; } = new List<EgresoMensualModel>();
        public List<FlujoCajaModel> FlujoCaja { get; set; } = new List<FlujoCajaModel>();
        public AnalisisFinancieroModel AnalisisFinanciero { get; set; } = new AnalisisFinancieroModel();
        public FiltroFechasModel FiltroFechas { get; set; } = new FiltroFechasModel();
    }

    public class ResumenFinancieroModel
    {
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal BalanceNeto { get; set; }
        public decimal IngresosHoy { get; set; }
        public decimal EgresosHoy { get; set; }
        public decimal IngresosSemana { get; set; }
        public decimal EgresosSemana { get; set; }
        public decimal IngresosMes { get; set; }
        public decimal EgresosMes { get; set; }
        public decimal MargenBruto { get; set; }
        public decimal PorcentajeMargen { get; set; }
        public string EstadoFinanciero => BalanceNeto >= 0 ? "Positivo" : "Negativo";
    }

    public class MovimientoFinancieroModel
    {
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = ""; // "Ingreso" o "Egreso"
        public string Concepto { get; set; } = "";
        public string Detalle { get; set; } = "";
        public decimal Monto { get; set; }
        public string Categoria { get; set; } = "";
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm");
        public string MontoFormateado => $"₡{Monto:N2}";
        public string CssClass => TipoMovimiento == "Ingreso" ? "text-success" : "text-danger";
    }

    public class IngresoMensualModel
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal OtrosIngresos { get; set; }
        public decimal TotalIngresos { get; set; }
        public string MesNombre { get; set; } = "";
        public string PeriodoFormateado => $"{MesNombre} {Año}";
    }

    public class EgresoMensualModel
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public decimal ComprasProveedores { get; set; }
        public decimal SalariosPlanillas { get; set; }
        public decimal Liquidaciones { get; set; }
        public decimal OtrosEgresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public string MesNombre { get; set; } = "";
        public string PeriodoFormateado => $"{MesNombre} {Año}";
    }

    public class FlujoCajaModel
    {
        public DateTime Fecha { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal FlujoNeto { get; set; }
        public decimal SaldoAcumulado { get; set; }
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
    }

    public class AnalisisFinancieroModel
    {
        public decimal PromedioIngresosDiarios { get; set; }
        public decimal PromedioEgresosDiarios { get; set; }
        public decimal TendenciaIngresos { get; set; } // % cambio vs mes anterior
        public decimal TendenciaEgresos { get; set; } // % cambio vs mes anterior
        public string TendenciaIngresosTexto => TendenciaIngresos >= 0 ? $"+{TendenciaIngresos:F1}%" : $"{TendenciaIngresos:F1}%";
        public string TendenciaEgresosTexto => TendenciaEgresos >= 0 ? $"+{TendenciaEgresos:F1}%" : $"{TendenciaEgresos:F1}%";
        public decimal RatioIngresosEgresos { get; set; }
        public int DiasOperativos { get; set; }
        public decimal PuntoEquilibrio { get; set; }
    }

    // Modelos para el Dashboard de Historial de Actividades
    public class HistorialActividadesViewModel
    {
        public List<ActividadRecienteModel> ActividadesRecientes { get; set; } = new List<ActividadRecienteModel>();
        public List<ActividadPorTipoModel> ActividadesPorTipo { get; set; } = new List<ActividadPorTipoModel>();
        public List<ActividadPorUsuarioModel> ActividadesPorUsuario { get; set; } = new List<ActividadPorUsuarioModel>();
        public List<ActividadPorFechaModel> ActividadesPorFecha { get; set; } = new List<ActividadPorFechaModel>();
        public ResumenActividadesModel ResumenActividades { get; set; } = new ResumenActividadesModel();
        public List<ActividadDetalleModel> DetalleActividades { get; set; } = new List<ActividadDetalleModel>();
        public FiltroFechasModel FiltroFechas { get; set; } = new FiltroFechasModel();
    }

    public class ActividadRecienteModel
    {
        public int IdActividad { get; set; }
        public DateTime FechaActividad { get; set; }
        public string TipoActividad { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Modulo { get; set; } = "";
        public string Detalles { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public string FechaFormateada => FechaActividad.ToString("dd/MM/yyyy HH:mm:ss");
        public string TiempoTranscurrido
        {
            get
            {
                var diferencia = DateTime.Now - FechaActividad;
                if (diferencia.TotalMinutes < 1)
                    return "Hace menos de 1 minuto";
                if (diferencia.TotalMinutes < 60)
                    return $"Hace {(int)diferencia.TotalMinutes} minutos";
                if (diferencia.TotalHours < 24)
                    return $"Hace {(int)diferencia.TotalHours} horas";
                return $"Hace {(int)diferencia.TotalDays} días";
            }
        }
        public string CssClass
        {
            get
            {
                return TipoActividad switch
                {
                    "Crear" => "text-success",
                    "Editar" => "text-warning",
                    "Eliminar" => "text-danger",
                    "Login" => "text-info",
                    "Logout" => "text-secondary",
                    "Error" => "text-danger",
                    _ => "text-primary"
                };
            }
        }
        public string IconoActividad
        {
            get
            {
                return TipoActividad switch
                {
                    "Crear" => "fas fa-plus-circle",
                    "Editar" => "fas fa-edit",
                    "Eliminar" => "fas fa-trash",
                    "Login" => "fas fa-sign-in-alt",
                    "Logout" => "fas fa-sign-out-alt",
                    "Error" => "fas fa-exclamation-triangle",
                    "Venta" => "fas fa-shopping-cart",
                    "Compra" => "fas fa-shopping-bag",
                    "Planilla" => "fas fa-money-check-alt",
                    _ => "fas fa-info-circle"
                };
            }
        }
    }

    public class ActividadPorTipoModel
    {
        public string TipoActividad { get; set; } = "";
        public int CantidadActividades { get; set; }
        public decimal PorcentajeTotal { get; set; }
    }

    public class ActividadPorUsuarioModel
    {
        public string Usuario { get; set; } = "";
        public int CantidadActividades { get; set; }
        public DateTime UltimaActividad { get; set; }
        public string UltimaActividadFormateada => UltimaActividad.ToString("dd/MM/yyyy HH:mm");
    }

    public class ActividadPorFechaModel
    {
        public DateTime Fecha { get; set; }
        public int CantidadActividades { get; set; }
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
    }

    public class ResumenActividadesModel
    {
        public int TotalActividadesHoy { get; set; }
        public int TotalActividadesSemana { get; set; }
        public int TotalActividadesMes { get; set; }
        public int UsuariosActivosHoy { get; set; }
        public int ErroresHoy { get; set; }
        public decimal PromedioActividadesDiarias { get; set; }
        public string ActividadMasFrecuente { get; set; } = "";
        public string UsuarioMasActivo { get; set; } = "";
    }

    public class ActividadDetalleModel
    {
        public DateTime Fecha { get; set; }
        public string TipoActividad { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Modulo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Detalles { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm:ss");
    }
}
