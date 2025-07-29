namespace DaPazWebApp.Models
{
    public class PlanillaViewModel
    {
        public PlanillaViewModel()
        {
            DetalleHoras = new List<DetalleHorasViewModel>();
        }

        public int IdPlanilla { get; set; }
        public DateTime FechaPlanilla { get; set; }
        public decimal SalarioBruto { get; set; }
        public decimal Deducciones { get; set; }
        public decimal SalarioNeto { get; set; }

        // Totales calculados
        public decimal TotalSemana { get; set; }
        public decimal SalarioNetoCalculado { get; set; }

        // Empleado
        public string NombreEmpleado { get; set; }
        public string ApellidoEmpleado { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string Cargo { get; set; }
        public decimal SalarioEmpleado { get; set; } // salario por hora

        public string NombreDeduccion { get; set; }

        // Detalle de horas por día
        public List<DetalleHorasViewModel> DetalleHoras { get; set; }
      
    }

   


}