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
        public string NombreProducto { get; set; } // Para mostrar en historial
        public int? idPromocion { get; set; }
        public decimal? precioOriginal { get; set; }
        public decimal? descuentoAplicado { get; set; }
    }
}