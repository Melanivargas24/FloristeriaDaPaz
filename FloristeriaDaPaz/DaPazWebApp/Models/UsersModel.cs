using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace DaPazWebApp.Models
{
    public class UsersModel
    {
        public int? idUsuario { get; set; }
        public string? nombre { get; set; }
        public string? apellido { get; set; }
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo válido")]
        public string? correo { get; set; }
        [RegularExpression(@"^\d+$", ErrorMessage = "El teléfono debe contener solo números")]
        [MaxLength(12, ErrorMessage = "El teléfono no puede tener más de 12 dígitos")]
        public string?  telefono { get; set; }
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string?  contrasena { get; set; }
        public string? contrasenaActual { get; set; }
        public string? nuevaContrasena { get; set; }
        public string?  estado { get; set; }
        public int?  idRol { get; set; }
        public string? direccion { get; set; }
        public int? idDistrito { get; set; }
        public int? idCanton { get; set; }
        public int? idProvincia { get; set; }

    }
}
