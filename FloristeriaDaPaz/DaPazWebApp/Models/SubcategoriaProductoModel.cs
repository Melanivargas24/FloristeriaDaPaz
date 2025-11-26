using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class SubcategoriaProductoModel
    {
        public int idSubcategoriaProducto { get; set; }

        [Required(ErrorMessage = "El nombre de la subcategoría es obligatorio.")]
        [Display(Name = "Nombre de Subcategoría")]
        public string nombreSubcategoriaProducto { get; set; }
        
        [Required(ErrorMessage = "Debe seleccionar una categoría.")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría válida.")]
        [Display(Name = "Categoría")]
        public int idCategoriaProducto { get; set; }
        public string nombreCategoriaProducto { get; set; }
    }
}
