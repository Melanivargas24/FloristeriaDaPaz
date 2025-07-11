namespace DaPazWebApp.Models
{
    public class DetalleHoras
    {
        public int IdDetalle { get; set; }

        public int IdPlanilla { get; set; }

        public DateTime Fecha { get; set; }

        public decimal HorasRegulares { get; set; }

        public decimal HorasExtra { get; set; }

        public decimal Porcentaje { get; set; } = 1.5M;

        public Planilla Planilla { get; set; } 
    }

}
