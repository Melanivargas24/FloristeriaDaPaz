namespace DaPazWebApp.Models
{
    public class Arreglo
    {
        public int idArreglo { get; set; }

        public string nombreArreglo { get; set; }

        public string descripcion { get; set; }

        public int precio { get; set; }

        public string? imagen { get; set; }

        public bool estado { get; set; }

        public int idCategoriaArreglo { get; set; }

        public string? nombreCategoriaArreglo { get; set; }

        // Relación con productos
        public List<ArregloProductoModel>? Productos { get; set; }

        // Propiedades para promociones
        public bool TienePromocion { get; set; } = false;
        public decimal PrecioOriginal { get; set; }
        public decimal PrecioConDescuento { get; set; }
        public string? NombrePromocion { get; set; }
        public double? PorcentajeDescuento { get; set; }

    }
        public class ArregloProductoModel
    {
        public int idProducto { get; set; }
        public string? nombreProducto { get; set; }
        public int cantidadProducto { get; set; }
    }
}
