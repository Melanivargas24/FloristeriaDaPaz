namespace DaPazWebApp.Models
{
    public class Factura
    {
        public int idFactura { get; set; }
        public DateTime fechaFactura { get; set; }
        public decimal totalFactura { get; set; }
        public int idUsuario { get; set; } // Para historial global
    }
}