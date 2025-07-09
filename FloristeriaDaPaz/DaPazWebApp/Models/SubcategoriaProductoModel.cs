using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class SubcategoriaProductoModel
    {
        public int idSubcategoriaProducto { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría de producto es obligatorio.")]
        public string? nombreSubcategoriaProducto { get; set; }
        public int idCategoriaProducto { get; set; }
        public string? nombreCategoriaProducto { get; set; }
    }
}
