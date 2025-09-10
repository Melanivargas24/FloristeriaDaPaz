using System.Collections.Generic;

namespace DaPazWebApp.Models
{
    public class FacturaViewModel
    {
        public Factura Factura { get; set; }
        public List<Venta> Ventas { get; set; }
        public decimal CostoEnvio { get; set; }
        public string? NombreDistrito { get; set; }
        public bool TieneEnvioADomicilio { get; set; }
        public decimal SubtotalProductos { get; set; }
    }
}
