using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class EmpleadoModel
    {
        public int idEmpleado { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? nombre { get; set; }
        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string? apellido { get; set; }
        public string? correo { get; set; }
        public string? Cargo { get; set; }
        public decimal salario { get; set; }
        [RegularExpression(@"^\d+$", ErrorMessage = "El teléfono debe contener solo números")]
        [MaxLength(12, ErrorMessage = "El teléfono no puede tener más de 12 dígitos")]
        public string? telefono { get; set; }
        public DateTime fechaIngreso { get; set; }
        public DateTime? fechaSalida { get; set; }
        public int idUsuario { get; set; }
    }
}