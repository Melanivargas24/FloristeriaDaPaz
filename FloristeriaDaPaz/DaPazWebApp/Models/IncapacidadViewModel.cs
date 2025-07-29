namespace DaPazWebApp.Models
{
    public class IncapacidadViewModel
    {
        public string NumeroIncapacidad { get; set; }
        public string MotivoIncapacidad { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int CantidadDias { get; set; }
        public string CentroMedicoEmisor { get; set; }
        public string EntidadEmisora { get; set; }

 
        public string NombreEmpleado { get; set; }
        public string ApellidoEmpleado { get; set; }
    }
}
