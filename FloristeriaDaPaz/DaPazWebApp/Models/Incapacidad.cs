using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class Incapacidad
    {
        [Key]
        public int IdIncapacidad { get; set; }

        [Required]
        [StringLength(50)]
        public string NumeroIncapacidad { get; set; }

        [Required]
        [StringLength(200)]
        public string MotivoIncapacidad { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaInicio { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaFin { get; set; }

        [NotMapped]
        public int CantidadDias => (FechaFin - FechaInicio).Days;

        [Required]
        [StringLength(100)]
        public string CentroMedicoEmisor { get; set; }

        [Required]
        [StringLength(50)]
        public string EntidadEmisora { get; set; } 

        [Required]
        public int IdEmpleado { get; set; }

        [ForeignKey("IdEmpleado")]
        public EmpleadoModel Empleado { get; set; }
    }
}

