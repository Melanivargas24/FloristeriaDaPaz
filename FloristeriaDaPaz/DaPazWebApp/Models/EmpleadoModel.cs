using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class EmpleadoModel
    {
        public int idEmpleado { get; set; }
        public string? nombre { get; set; }
        public string? apellido { get; set; }
        public string? correo { get; set; }
        [Required(ErrorMessage = "El cargo es obligatorio")]
        public string? Cargo { get; set; }
        [Required(ErrorMessage = "El salario es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El salario debe ser mayor a 0")]
        public decimal salario { get; set; }
        [RegularExpression(@"^\d+$", ErrorMessage = "El teléfono debe contener solo números")]
        [MaxLength(12, ErrorMessage = "El teléfono no puede tener más de 12 dígitos")]
        public string? telefono { get; set; }
        [Required(ErrorMessage = "La fecha de ingreso es obligatoria")]
        public DateTime fechaIngreso { get; set; }
        public DateTime? fechaSalida { get; set; }
        [Required(ErrorMessage = "Debe seleccionar un usuario")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un usuario válido")]
        public int idUsuario { get; set; }
    }
}