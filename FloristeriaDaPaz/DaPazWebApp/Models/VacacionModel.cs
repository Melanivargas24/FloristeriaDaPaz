using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class Vacacion
    {
        public int IdVacacion { get; set; }

        [Required(ErrorMessage = "El empleado es requerido")]
        public int IdEmpleado { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es requerida")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Inicio")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "La fecha de fin es requerida")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Fin")]
        public DateTime FechaFin { get; set; }
        
        // Validación personalizada para el rango de días
        public bool EsRangoValido => DiasVacacion <= 12 && DiasVacacion > 0;

        // Propiedades de navegación
        public EmpleadoModel Empleado { get; set; }

        // Propiedades calculadas
        public int DiasVacacion => (FechaFin - FechaInicio).Days + 1;

        // Nombre completo del empleado
        public string NombreCompleto => Empleado != null ? $"{Empleado.nombre} {Empleado.apellido}" : "Sin empleado";

        // Propiedades para la vista
        public string EstadoVacacion
        {
            get
            {
                if (FechaInicio > DateTime.Now)
                    return "Programadas";
                else if (FechaFin < DateTime.Now)
                    return "Completadas";
                else
                    return "En curso";
            }
        }

        public string ClaseEstado
        {
            get
            {
                return EstadoVacacion switch
                {
                    "Programadas" => "bg-warning",
                    "Completadas" => "bg-success",
                    "En curso" => "bg-primary",
                    _ => "bg-secondary"
                };
            }
        }
    }
}