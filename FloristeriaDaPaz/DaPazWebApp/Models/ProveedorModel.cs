using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class ProveedorModel
    {
        public int? IdProveedor { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
        public string? nombreProveedor { get; set; }
        [Required(ErrorMessage = "El teléfono del proveedor es obligatorio.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "El teléfono debe contener solo números")]
        [MaxLength(12, ErrorMessage = "El teléfono no puede tener más de 12 dígitos")]
        public string? telefonoProveedor { get; set; }
        [Required(ErrorMessage = "El correo del proveedor es obligatorio.")]
        public string? correoProveedor { get; set; }
        [Required(ErrorMessage = "La dirección del proveedor es obligatoria.")]
        public string? direccionProveedor { get; set; }
        public string? estado { get; set; }
    }
}
