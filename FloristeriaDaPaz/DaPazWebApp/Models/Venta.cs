namespace DaPazWebApp.Models
{
    public class Venta
    {
        public int idVenta { get; set; }
        public DateTime fechaVenta { get; set; }
        public int cantidad { get; set; }
        public decimal total { get; set; }
        public int idUsuario { get; set; }
        public int? idProducto { get; set; }
        public int? idArreglo { get; set; }
        public string tipoEntrega { get; set; }
        public string metodoPago { get; set; }
        // Puedes agregar más propiedades si tu SP retorna más columnas
    }
}