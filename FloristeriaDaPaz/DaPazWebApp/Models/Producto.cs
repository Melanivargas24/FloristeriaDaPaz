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

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public int? Precio { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public int? Stock { get; set; }

        public string? Imagen { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public string? Estado { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public int? IdCategoriaProducto { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio.")]
        public int? IdProveedor { get; set; }


        public string? NombreCategoriaProducto { get; set; }
        public string? NombreProveedor { get; set; }
    }
}
