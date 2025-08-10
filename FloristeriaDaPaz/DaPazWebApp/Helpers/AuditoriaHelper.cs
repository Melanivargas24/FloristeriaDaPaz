using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace DaPazWebApp.Helpers
{
    public static class AuditoriaHelper
    {
        private static IConfiguration? _configuration;

        public static void SetConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Registra una actividad simple en el sistema usando el stored procedure existente
        /// </summary>
        public static void RegistrarActividad(
            string tipoActividad,
            string modulo,
            string descripcion,
            string detalles = "",
            string usuario = "Sistema")
        {
            try
            {
                // Usar la misma cadena de conexión que usa la aplicación
                string? connectionString = _configuration?.GetConnectionString("BDConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    // Fallback a la cadena de conexión de Azure si no está configurada
                    connectionString = "Server=dapaz--server.database.windows.net;Database=dapazdb-development;User Id=dapaz-dev;Password=dp_2025_dv2;TrustServerCertificate=True";
                }
                
                using (var connection = new SqlConnection(connectionString))
                {
                    // Usar el stored procedure existente SP_RegistrarActividad
                    var parametros = new
                    {
                        TipoActividad = tipoActividad,
                        Modulo = modulo,
                        Descripcion = descripcion,
                        Detalles = detalles ?? "",
                        Usuario = usuario ?? "Sistema",
                        IPAddress = "127.0.0.1",
                        UserAgent = (string?)null,
                        RegistroAfectadoId = (string?)null,
                        ValoresAnteriores = (string?)null,
                        ValoresNuevos = (string?)null,
                        Exitoso = true,
                        MensajeError = (string?)null
                    };
                    
                    connection.QuerySingle<int>(
                        "SP_RegistrarActividad", 
                        parametros, 
                        commandType: CommandType.StoredProcedure);
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Actividad registrada: {tipoActividad} - {modulo} - {descripcion}");
                    Console.WriteLine($"✅ Actividad registrada: {tipoActividad} - {modulo} - {descripcion}");
                }
            }
            catch (Exception ex)
            {
                // En caso de error en el logging, no afectar la operación principal
                System.Diagnostics.Debug.WriteLine($"❌ Error al registrar actividad: {ex.Message}");
                Console.WriteLine($"❌ Error al registrar actividad: {ex.Message}");
            }
        }
    }
}
