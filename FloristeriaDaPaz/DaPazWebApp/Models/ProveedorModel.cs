using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class ProveedorModel
    {
        public int? IdProveedor { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
        public string? nombreProveedor { get; set; }
        [Required(ErrorMessage = "El teléfono del proveedor es obligatorio.")]
        public string? telefonoProveedor { get; set; }
        [Required(ErrorMessage = "El correo del proveedor es obligatorio.")]
        public string? correoProveedor { get; set; }
        [Required(ErrorMessage = "La dirección del proveedor es obligatoria.")]
        public string? direccionProveedor { get; set; }
        public string? estado { get; set; }
        public int? IdDistrito { get; set; }
        public int? IdCanton { get; set; }
        public int? IdProvincia { get; set; }
    }
}
