namespace DaPazWebApp.Models
{
    public class UsuarioConsultaModel
    {
        public int idUsuario { get; set; }
        public string? nombre { get; set; }
        public string? apellido { get; set; }
        public string? correo { get; set; }
        public string? telefono { get; set; }
        public string? estado { get; set; }
        public string? nombreRol { get; set; }
        public string? direccion { get; set; }
    }
}