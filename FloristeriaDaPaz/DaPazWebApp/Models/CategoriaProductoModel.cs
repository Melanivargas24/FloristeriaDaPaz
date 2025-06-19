using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class CategoriaProductoModel
    {
        public int idCategoriaProducto {  get; set; }

        [Required(ErrorMessage = "El nombre de la categoría de producto es obligatorio.")]
        public string? nombreCategoriaProducto { get; set; }
    }
}
