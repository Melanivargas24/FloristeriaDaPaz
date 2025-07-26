namespace DaPazWebApp.Models
{
    public class DashboardViewModel
    {
        public List<VentaDiariaModel> VentasDiarias { get; set; } = new List<VentaDiariaModel>();
        public List<VentaSemanalModel> VentasSemanales { get; set; } = new List<VentaSemanalModel>();
        public List<VentaMensualModel> VentasMensuales { get; set; } = new List<VentaMensualModel>();
        public EstadisticasGeneralesModel EstadisticasGenerales { get; set; } = new EstadisticasGeneralesModel();
        public List<TopProductoModel> TopProductos { get; set; } = new List<TopProductoModel>();
        public List<ProductoBajoStockModel> ProductosBajoStock { get; set; } = new List<ProductoBajoStockModel>();
        public ResumenInventarioModel ResumenInventario { get; set; } = new ResumenInventarioModel();
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
}
