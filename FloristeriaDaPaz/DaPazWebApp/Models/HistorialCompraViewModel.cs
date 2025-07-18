using System.Collections.Generic;

namespace DaPazWebApp.Models
{
    public class HistorialCompraViewModel
    {
        public List<Factura> Facturas { get; set; } = new List<Factura>();
        public Dictionary<int, List<Venta>> VentasPorFactura { get; set; } = new Dictionary<int, List<Venta>>();
    }
}
