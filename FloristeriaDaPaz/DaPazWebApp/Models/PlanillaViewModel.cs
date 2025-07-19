namespace DaPazWebApp.Models
{
    public class PlanillaViewModel
    {
        public int IdPlanilla { get; set; }
        public DateTime FechaPlanilla { get; set; }
        public decimal SalarioBruto { get; set; }
        public decimal Deducciones { get; set; }
        public decimal SalarioNeto { get; set; }

        public string NombreDeduccion { get; set; }
        public decimal MontoDeduccion { get; set; }

        public string NombreEmpleado { get; set; }
        public string ApellidoEmpleado { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string Cargo { get; set; }
    }

}
