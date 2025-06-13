using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class EditarUsuarioModel
    {
        public int? idUsuario { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string? nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string? apellido { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Debe ingresar un correo válido")]
        public string? correo { get; set; }

        public string? telefono { get; set; }
        public string? contrasenaActual { get; set; }
        public string? nuevaContrasena { get; set; }
    }
}