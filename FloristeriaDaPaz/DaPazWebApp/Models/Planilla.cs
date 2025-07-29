namespace DaPazWebApp.Models
{
    public class Planilla
    {
        public int IdPlanilla { get; set; }

        public int IdEmpleado { get; set; }

        public DateTime FechaPlanilla { get; set; }

        public decimal SalarioBruto { get; set; }

        public decimal Deducciones { get; set; }

        public decimal SalarioNeto { get; set; }

     

        public EmpleadoModel Empleado { get; set; }

        public List<DetalleHoras> DetallesHoras { get; set; } = new List<DetalleHoras>();



    }
}
