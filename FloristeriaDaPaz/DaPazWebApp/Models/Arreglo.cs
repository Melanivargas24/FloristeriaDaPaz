namespace DaPazWebApp.Models
{
    public class Arreglo
    {
       public int idArreglo {  get; set; }
       
        public string nombreArreglo { get; set; }

        public string descripcion { get; set; }

        public int precio { get; set; }

        public int stock { get; set; }

        public string? imagen { get; set; } 

        public string estado { get; set; }

        public int idCategoriaArreglo { get; set; }

        public string? nombreCategoriaArreglo { get; set; }



    }
}
