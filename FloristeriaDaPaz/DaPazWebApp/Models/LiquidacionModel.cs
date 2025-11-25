using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class Liquidacion
    {
        public int IdLiquidacion { get; set; }

        [Required(ErrorMessage = "El empleado es requerido")]
        [Display(Name = "Empleado")]
        public int IdEmpleado { get; set; }

        [Required(ErrorMessage = "La fecha de liquidación es requerida")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Liquidación")]
        public DateTime FechaLiquidacion { get; set; }

        [Required(ErrorMessage = "El monto de liquidación es requerido")]
        [Range(0.01, 999999999.99, ErrorMessage = "El monto debe ser mayor a 0 y menor a 1,000,000,000")]
        [Display(Name = "Monto de Liquidación")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontoLiquidacion { get; set; }

        [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        [Display(Name = "Motivo (Opcional)")]
        public string? Motivo { get; set; }

        // Propiedades de navegación
        public EmpleadoModel? Empleado { get; set; }

        // Propiedades calculadas para la vista
        public int AnosServicio
        {
            get
            {
                if (Empleado?.fechaIngreso != null)
                {
                    return FechaLiquidacion.Year - Empleado.fechaIngreso.Year;
                }
                return 0;
            }
        }

        public string MontoFormateado => $"₡{MontoLiquidacion:N2}";
        public string SalarioBaseFormateado => $"₡{(Empleado?.salario ?? 0):N2}";
        public string NombreCompleto => Empleado != null ? $"{Empleado.nombre} {Empleado.apellido}" : "Sin empleado";
    }
}