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
    }

    public class CarritoViewModel
    {
        public List<CarritoItem> Items { get; set; } = new List<CarritoItem>();
        public decimal Total => Items.Sum(x => x.Precio * x.Cantidad);
    }
}
