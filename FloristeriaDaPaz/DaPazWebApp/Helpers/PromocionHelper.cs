using DaPazWebApp.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DaPazWebApp.Helpers
{
    public static class PromocionHelper
    {
        public static Promociones? ObtenerPromocionActiva(int idProducto, IConfiguration configuration)
        {
            try
            {
                using var connection = new SqlConnection(configuration.GetConnectionString("BDConnection"));
                
                // Usar procedimiento almacenado en lugar de consulta directa
                var promocion = connection.QuerySingleOrDefault<Promociones>(
                    "SP_ObtenerPromocionActivaPorProducto",
                    new { idProducto = idProducto },
                    commandType: CommandType.StoredProcedure);
                
                // Verificar si la promoción ha vencido y actualizarla automáticamente
                if (promocion != null && promocion.estado == "Activa" && 
                    promocion.fechaFin.HasValue && promocion.fechaFin.Value.Date < DateTime.Now.Date)
                {
                    // Actualizar estado a inactiva
                    connection.Execute("SP_ModificarPromocion", new
                    {
                        promocion.idPromocion,
                        promocion.nombrePromocion,
                        promocion.descuentoPorcentaje,
                        promocion.fechaInicio,
                        promocion.fechaFin,
                        promocion.idProducto,
                        estado = "Inactiva"
                    }, commandType: CommandType.StoredProcedure);
                    
                    // Devolver null ya que la promoción ya no está activa
                    return null;
                }
                
                return promocion;
            }
            catch (Exception ex)
            {
                // Log error si es necesario
                System.Diagnostics.Debug.WriteLine($"Error obteniendo promoción: {ex.Message}");
                return null;
            }
        }

        public static List<Promociones> ObtenerPromocionesActivas(IConfiguration configuration)
        {
            try
            {
                using var connection = new SqlConnection(configuration.GetConnectionString("BDConnection"));
                var promociones = connection.Query<Promociones>(
                    "SP_ObtenerPromocionesActivas",
                    commandType: CommandType.StoredProcedure
                ).ToList();
                
                // Verificar y actualizar promociones vencidas
                var fechaActual = DateTime.Now.Date;
                var promocionesParaActualizar = promociones.Where(p => 
                    p.estado == "Activa" && 
                    p.fechaFin.HasValue && 
                    p.fechaFin.Value.Date < fechaActual).ToList();
                
                // Actualizar promociones vencidas en la base de datos
                foreach (var promo in promocionesParaActualizar)
                {
                    connection.Execute("SP_ModificarPromocion", new
                    {
                        promo.idPromocion,
                        promo.nombrePromocion,
                        promo.descuentoPorcentaje,
                        promo.fechaInicio,
                        promo.fechaFin,
                        promo.idProducto,
                        estado = "Inactiva"
                    }, commandType: CommandType.StoredProcedure);
                }
                
                // Retornar solo las promociones que siguen activas (no vencidas)
                return promociones.Where(p => 
                    p.estado == "Activa" && 
                    (!p.fechaFin.HasValue || p.fechaFin.Value.Date >= fechaActual)).ToList();
            }
            catch (Exception ex)
            {
                // Log error si es necesario
                System.Diagnostics.Debug.WriteLine($"Error obteniendo promociones activas: {ex.Message}");
                return new List<Promociones>();
            }
        }

        public static (decimal precioOriginal, decimal precioConDescuento, decimal descuentoAplicado, int? idPromocion) 
            CalcularPrecioConPromocion(int idProducto, int cantidad, IConfiguration configuration)
        {
            try
            {
                using var connection = new SqlConnection(configuration.GetConnectionString("BDConnection"));
                
                var parameters = new DynamicParameters();
                parameters.Add("@idProducto", idProducto);
                parameters.Add("@cantidad", cantidad);
                parameters.Add("@precioOriginal", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 10, scale: 2);
                parameters.Add("@precioConDescuento", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 10, scale: 2);
                parameters.Add("@descuentoAplicado", dbType: DbType.Decimal, direction: ParameterDirection.Output, precision: 10, scale: 2);
                parameters.Add("@idPromocion", dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("SP_CalcularPrecioConPromocion", parameters, commandType: CommandType.StoredProcedure);

                var precioOriginal = parameters.Get<decimal>("@precioOriginal");
                var precioConDescuento = parameters.Get<decimal>("@precioConDescuento");
                var descuentoAplicado = parameters.Get<decimal>("@descuentoAplicado");
                var idPromocion = parameters.Get<int?>("@idPromocion");

                return (precioOriginal, precioConDescuento, descuentoAplicado, idPromocion);
            }
            catch (Exception ex)
            {
                // Log error si es necesario
                System.Diagnostics.Debug.WriteLine($"Error calculando precio con promoción: {ex.Message}");
                return (0, 0, 0, null);
            }
        }

        public static decimal CalcularDescuentoPorcentaje(decimal precioOriginal, decimal precioConDescuento)
        {
            if (precioOriginal <= 0) return 0;
            return Math.Round(((precioOriginal - precioConDescuento) / precioOriginal) * 100, 2);
        }

        public static (bool tienePromocion, decimal precioOriginal, decimal precioConDescuento, 
                      decimal descuentoTotal, string promocionesAplicadas) 
            CalcularPromocionArreglo(int idArreglo, IConfiguration configuration)
        {
            try
            {
                using var connection = new SqlConnection(configuration.GetConnectionString("BDConnection"));
                
                var resultado = connection.QuerySingleOrDefault(
                    "SP_CalcularPromocionArreglo",
                    new { idArreglo = idArreglo },
                    commandType: CommandType.StoredProcedure);

                if (resultado != null)
                {
                    return (
                        tienePromocion: resultado.tienePromocion == 1,
                        precioOriginal: resultado.precioOriginalArreglo,
                        precioConDescuento: resultado.precioConDescuento,
                        descuentoTotal: resultado.descuentoTotal,
                        promocionesAplicadas: resultado.promocionesAplicadas ?? string.Empty
                    );
                }
                
                return (false, 0, 0, 0, string.Empty);
            }
            catch (Exception ex)
            {
                // Log error si es necesario
                System.Diagnostics.Debug.WriteLine($"Error calculando promoción de arreglo: {ex.Message}");
                return (false, 0, 0, 0, string.Empty);
            }
        }
    }
}