using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class CategoriaArregloModel
    {
        public int? idCategoriaArreglo { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría de arreglo es obligatorio.")]
        public string? nombreCategoriaArreglo { get; set; }
    }
}