namespace DaPazWebApp.Models
{
    public class EmpleadoModel
    {
        public int idEmpleado { get; set; }
        public string? nombre { get; set; }
        public string? apellido { get; set; }
        public string? correo { get; set; }
        public string? Cargo { get; set; }
        public decimal salario { get; set; }
        public string? telefono { get; set; }
        public DateTime fechaIngreso { get; set; }
        public DateTime? fechaSalida { get; set; }
        public int idUsuario { get; set; }
    }
}