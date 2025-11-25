using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class DetalleHoras
    {
        public int IdDetalle { get; set; }

        public int IdPlanilla { get; set; }

        [Required(ErrorMessage = "La fecha es requerida")]
        public DateTime Fecha { get; set; }

        [Range(0, 12, ErrorMessage = "Las horas regulares deben estar entre 0 y 12")]
        [Display(Name = "Horas Regulares")]
        public decimal HorasRegulares { get; set; }

        [Range(0, 4, ErrorMessage = "Las horas extra no pueden exceder 4 horas por día")]
        [Display(Name = "Horas Extra")]
        public decimal HorasExtra { get; set; }

        [Range(1.0, 3.0, ErrorMessage = "El porcentaje debe estar entre 1.0 y 3.0")]
        public decimal Porcentaje { get; set; } = 1.5M;

        public Planilla? Planilla { get; set; } 
    }

}
