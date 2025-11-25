using System.ComponentModel.DataAnnotations;

namespace DaPazWebApp.Models
{
    public class Planilla
    {
        public int IdPlanilla { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un empleado")]
        [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un empleado válido")]
        public int IdEmpleado { get; set; }

        public DateTime FechaPlanilla { get; set; }

        public decimal SalarioBruto { get; set; }

        public decimal Deducciones { get; set; }

        public decimal SalarioNeto { get; set; }

     

        public EmpleadoModel? Empleado { get; set; }

        public List<DetalleHoras> DetallesHoras { get; set; } = new List<DetalleHoras>();



    }
}
