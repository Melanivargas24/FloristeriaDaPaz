using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DaPazWebApp.Models
{
    public class Producto
    {
        public int IdProducto { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public string? NombreProducto { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public string? Descripcion { get; set; }

        public int Precio { get; set; }

        public int Stock { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public string? Imagen { get; set; }

        public string? Estado { get; set; }

        public int IdCategoriaProducto { get; set; }

        public int IdProveedor { get; set; }
    }
}
