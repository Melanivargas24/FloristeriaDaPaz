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
        public string?  telefono { get; set; }
        public string?  contrasena { get; set; }
        public string? contrasenaActual { get; set; }
        public string? nuevaContrasena { get; set; }
        public string?  estado { get; set; }
        public int?  idRol { get; set; }
        public string? direccion { get; set; }

    }
}
