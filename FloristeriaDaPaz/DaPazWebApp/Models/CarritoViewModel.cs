using System.Collections.Generic;

namespace DaPazWebApp.Models
{
    public class CarritoItem
    {
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; }
        public string Imagen { get; set; }
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string Tipo { get; set; } // "producto" o "arreglo"
        
        // Campos para promociones
        public decimal PrecioOriginal { get; set; }
        public decimal PrecioConDescuento { get; set; }
        public int? IdPromocion { get; set; }
        public double? PorcentajeDescuento { get; set; }
        public string? NombrePromocion { get; set; }
        public decimal DescuentoAplicado { get; set; }
        
        // Propiedad calculada para mostrar el precio efectivo
        public decimal PrecioEfectivo => TienePromocion ? PrecioConDescuento : Precio;
        public bool TienePromocion => (IdPromocion.HasValue && PorcentajeDescuento > 0) || 
                                     (!string.IsNullOrEmpty(NombrePromocion) && PrecioConDescuento < Precio);
    }

    public class CarritoViewModel
    {
        public List<CarritoItem> Items { get; set; } = new List<CarritoItem>();
        public decimal Total => Items.Sum(x => x.PrecioEfectivo * x.Cantidad);
        public decimal TotalDescuentos => Items.Sum(x => x.DescuentoAplicado);
        public decimal TotalSinDescuentos => Items.Sum(x => x.PrecioOriginal * x.Cantidad);
    }
}
