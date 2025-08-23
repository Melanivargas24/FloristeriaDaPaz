using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class CatalogoModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public string Imagen { get; set; }
        public string Categoria { get; set; }
        public string Tipo { get; set; } // "Producto" o "Arreglo"
        
        // Propiedades para promociones
        public decimal PrecioOriginal { get; set; }
        public decimal PrecioConDescuento { get; set; }
        public bool TienePromocion { get; set; }
        public double? PorcentajeDescuento { get; set; }
        public string? NombrePromocion { get; set; }
        public int? IdPromocion { get; set; }
        
        // Propiedad calculada para mostrar el precio efectivo
        public decimal PrecioEfectivo => TienePromocion ? PrecioConDescuento : Precio;
    }

}