using System.Collections.Generic;

namespace DaPazWebApp.Models
{
    public class FacturaProveedorDetalleViewModel
    {
        public FacturaProveedorModel Factura { get; set; }
        public List<DetalleFacturaProveedorModel> Detalles { get; set; }
    }
}
