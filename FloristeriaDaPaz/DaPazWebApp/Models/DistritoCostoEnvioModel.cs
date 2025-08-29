using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class DistritoCostoEnvioModel
    {
        public int idDistrito { get; set; }
        
        public string nombreDistrito { get; set; } = "";
        
        public string nombreCanton { get; set; } = "";
        
        public string nombreProvincia { get; set; } = "";
        
        [Required(ErrorMessage = "El costo de envío es obligatorio")]
        [Range(0, double.MaxValue, ErrorMessage = "El costo de envío debe ser mayor o igual a 0")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal? costoEnvio { get; set; }

        // Propiedad calculada para mostrar la ubicación completa
        public string UbicacionCompleta => $"{nombreProvincia}, {nombreCanton}, {nombreDistrito}";
    }
}
