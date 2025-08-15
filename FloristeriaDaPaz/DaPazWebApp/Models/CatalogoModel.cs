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
    }

}