using System;
using System.Collections.Generic;

namespace DaPazWebApp.Models
{
    public class FacturaProveedorModel
    {
        public int IdFacturaProveedor { get; set; }
        public int IdProveedor { get; set; }
        public string? NombreProveedor { get; set; } // No requerido para el binding ni validación
        public DateTime FechaFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public List<DetalleFacturaProveedorModel> Detalles { get; set; } = new List<DetalleFacturaProveedorModel>();
    }

    public class DetalleFacturaProveedorModel
    {
        public int IdProducto { get; set; }
        public string? NombreProducto { get; set; } // No requerido para el binding ni validación
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; } // Precio de compra
        public decimal PrecioVenta { get; set; }    // Precio de venta
        public decimal Subtotal { get; set; }
    }
}
