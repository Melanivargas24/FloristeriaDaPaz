namespace DaPazWebApp.Models
{
    public class DetallePlanillaCompletoViewModel
    {
        // Datos de la Planilla
        public int IdPlanilla { get; set; }
        public DateTime FechaPlanilla { get; set; }
        public decimal SalarioBruto { get; set; }
        public decimal Deducciones { get; set; }
        public decimal SalarioNeto { get; set; }

        // Totales adicionales
        public decimal TotalDeducciones { get; set; }
        public decimal TotalSemana { get; set; }
        public decimal SalarioNetoCalculado { get; set; }

        // Info del empleado
        public decimal SalarioEmpleado { get; set; }
        public DateTime FechaIngreso { get; set; }
        public int IdUsuario { get; set; }

        // Info del usuario
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Correo { get; set; }
        public string Telefono { get; set; }

        // Horas trabajadas
        public decimal TotalHorasRegulares { get; set; }
        public decimal TotalHorasExtra { get; set; }
        public decimal PorcentajePromedio { get; set; }

        public List<DetalleHorasViewModel> DetallesHoras { get; set; } = new List<DetalleHorasViewModel>();
        public List<DeduccionViewModel> DeduccionesDetalle { get; set; } = new List<DeduccionViewModel>();



    }
}