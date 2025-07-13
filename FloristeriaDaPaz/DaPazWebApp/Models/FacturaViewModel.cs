using System.Collections.Generic;

namespace DaPazWebApp.Models
{
    public class FacturaViewModel
    {
        public Factura Factura { get; set; }
        public List<Venta> Ventas { get; set; }
    }
}
