﻿namespace DaPazWebApp.Models
{
    public class UsersModel
    {
        public int? idUsuario { get; set; }
        public string?  nombre { get; set; }
        public string?  apellido { get; set; }
        public string?  correo { get; set; }
        public string?  telefono { get; set; }
        public string?  contrasena { get; set; }
        public string?  estado { get; set; }
        public int?  idRol { get; set; }
        public int? idDireccion { get; set; }

    }
}
