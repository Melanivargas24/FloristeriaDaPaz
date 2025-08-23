using DaPazWebApp.Helpers;
using DaPazWebApp.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DaPazWebApp.Controllers
{
    public class CarritoController : Controller

    {
        private readonly IConfiguration _configuration;
        public CarritoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarCantidad(int IdProducto, int Cantidad, string Tipo)
        {
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(x => x.IdProducto == IdProducto && x.Tipo == Tipo);
            if (item != null && Cantidad > 0)
            {
                item.Cantidad = Cantidad;
                HttpContext.Session.SetObjectAsJson("Carrito", carrito);
            }
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarDelCarrito(int IdProducto, string Tipo)
        {
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var item = carrito.FirstOrDefault(x => x.IdProducto == IdProducto && x.Tipo == Tipo);
            if (item != null)
            {
                carrito.Remove(item);
                HttpContext.Session.SetObjectAsJson("Carrito", carrito);
            }
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AgregarAlCarrito(int idProducto, int cantidad, string tipo = "producto")
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null || idUsuario == 0)
            {
                return RedirectToAction("Login", "Login");
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                string nombre = string.Empty;
                string imagen = string.Empty;
                decimal precio = 0;
                int id = idProducto;

                if (tipo == "arreglo")
                {
                    var arreglo = connection.QuerySingleOrDefault<Arreglo>(
                        "SP_ObtenerArregloPorId",
                        new { IdArreglo = idProducto },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    if (arreglo == null)
                        return NotFound();
                    nombre = arreglo.nombreArreglo;
                    imagen = arreglo.imagen ?? string.Empty;
                    precio = arreglo.precio;
                    id = arreglo.idArreglo;
                }
                else
                {
                    var producto = connection.QuerySingleOrDefault<Producto>(
                        "SP_ObtenerProductoPorId",
                        new { IdProducto = idProducto },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    if (producto == null)
                        return NotFound();
                    nombre = producto.NombreProducto ?? string.Empty;
                    imagen = producto.Imagen ?? string.Empty;
                    precio = producto.Precio ?? 0;
                    id = producto.IdProducto;
                }

                var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
                var item = carrito.FirstOrDefault(x => x.IdProducto == id && x.NombreProducto == nombre && x.Tipo == tipo);
                
                if (item != null)
                {
                    item.Cantidad += cantidad;
                    // Recalcular promoción con nueva cantidad
                    if (tipo == "producto")
                    {
                        var (precioOrig, precioDesc, descAplicado, idPromo) = PromocionHelper.CalcularPrecioConPromocion(id, item.Cantidad, _configuration);
                        item.PrecioOriginal = precioOrig;
                        item.PrecioConDescuento = precioDesc;
                        item.DescuentoAplicado = descAplicado;
                        item.IdPromocion = idPromo;
                        
                        if (idPromo.HasValue)
                        {
                            var promocion = PromocionHelper.ObtenerPromocionActiva(id, _configuration);
                            item.PorcentajeDescuento = promocion?.descuentoPorcentaje;
                            item.NombrePromocion = promocion?.nombrePromocion;
                        }
                    }
                    else if (tipo == "arreglo")
                    {
                        // Recalcular promoción del arreglo
                        var promocionArreglo = PromocionHelper.CalcularPromocionArreglo(id, _configuration);
                        if (promocionArreglo.tienePromocion)
                        {
                            item.PrecioOriginal = promocionArreglo.precioOriginal;
                            item.PrecioConDescuento = promocionArreglo.precioConDescuento;
                            item.DescuentoAplicado = promocionArreglo.descuentoTotal * item.Cantidad;
                            item.NombrePromocion = promocionArreglo.promocionesAplicadas;
                            item.PorcentajeDescuento = (double?)Math.Round(
                                (promocionArreglo.descuentoTotal / promocionArreglo.precioOriginal) * 100, 2);
                            item.IdPromocion = -1; // Indicador de promoción de arreglo
                        }
                        else
                        {
                            item.PrecioOriginal = item.Precio;
                            item.PrecioConDescuento = item.Precio;
                            item.DescuentoAplicado = 0;
                            item.IdPromocion = null;
                        }
                    }
                }
                else
                {
                    var nuevoItem = new CarritoItem
                    {
                        IdProducto = id,
                        NombreProducto = nombre,
                        Imagen = imagen,
                        Precio = precio,
                        Cantidad = cantidad,
                        Tipo = tipo,
                        PrecioOriginal = precio
                    };

                    // Aplicar promoción si es producto individual
                    if (tipo == "producto")
                    {
                        var (precioOrig, precioDesc, descAplicado, idPromo) = PromocionHelper.CalcularPrecioConPromocion(id, cantidad, _configuration);
                        nuevoItem.PrecioOriginal = precioOrig;
                        nuevoItem.PrecioConDescuento = precioDesc;
                        nuevoItem.DescuentoAplicado = descAplicado;
                        nuevoItem.IdPromocion = idPromo;
                        
                        if (idPromo.HasValue)
                        {
                            var promocion = PromocionHelper.ObtenerPromocionActiva(id, _configuration);
                            nuevoItem.PorcentajeDescuento = promocion?.descuentoPorcentaje;
                            nuevoItem.NombrePromocion = promocion?.nombrePromocion;
                        }
                    }
                    else
                    {
                        // Para arreglos, calcular promociones basadas en productos
                        var promocionArreglo = PromocionHelper.CalcularPromocionArreglo(id, _configuration);
                        if (promocionArreglo.tienePromocion)
                        {
                            nuevoItem.PrecioOriginal = promocionArreglo.precioOriginal;
                            nuevoItem.PrecioConDescuento = promocionArreglo.precioConDescuento;
                            nuevoItem.DescuentoAplicado = promocionArreglo.descuentoTotal * cantidad;
                            nuevoItem.NombrePromocion = promocionArreglo.promocionesAplicadas;
                            nuevoItem.PorcentajeDescuento = (double?)Math.Round(
                                (promocionArreglo.descuentoTotal / promocionArreglo.precioOriginal) * 100, 2);
                            // Para arreglos, no tenemos IdPromocion específico, así que usamos -1 para indicar que es promoción de arreglo
                            nuevoItem.IdPromocion = -1;
                        }
                        else
                        {
                            nuevoItem.PrecioOriginal = precio;
                            nuevoItem.PrecioConDescuento = precio;
                            nuevoItem.DescuentoAplicado = 0;
                        }
                    }

                    carrito.Add(nuevoItem);
                }
                HttpContext.Session.SetObjectAsJson("Carrito", carrito);
            }
            TempData["CarritoMensaje"] = tipo == "arreglo" ? "Arreglo agregado exitosamente" : "Producto agregado exitosamente";
            if (tipo == "arreglo")
                return RedirectToAction("DetallesAV", "Arreglo", new { id = idProducto });
            else
                return RedirectToAction("DetallesPV", "Producto", new { id = idProducto });
        }

        [HttpGet]
        public IActionResult Cart()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null || idUsuario == 0)
            {
                return RedirectToAction("Login", "Login");
            }

            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var viewModel = new CarritoViewModel { Items = carrito };
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null || idUsuario == 0)
            {
                return RedirectToAction("Login", "Login");
            }
            UsuarioConsultaModel? usuario;
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                usuario = connection.QuerySingleOrDefault<UsuarioConsultaModel>(
                    "SP_ObtenerUsuarioPorId",
                    new { idUsuario },
                    commandType: System.Data.CommandType.StoredProcedure
                );
            }
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var viewModel = new CarritoViewModel { Items = carrito };
            ViewBag.Usuario = usuario;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmarPedido(string tipoEntrega, string metodoPago)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null || idUsuario == 0)
            {
                return RedirectToAction("Login", "Login");
            }

            // Validación backend para evitar error 400
            if (string.IsNullOrEmpty(tipoEntrega) || string.IsNullOrEmpty(metodoPago))
            {
                ModelState.AddModelError("", "Debe seleccionar tipo de entrega y método de pago.");
                var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
                var viewModel = new CarritoViewModel { Items = carrito };
                UsuarioConsultaModel? usuario;
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    usuario = connection.QuerySingleOrDefault<UsuarioConsultaModel>(
                        "SP_ObtenerUsuarioPorId",
                        new { idUsuario },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                }
                ViewBag.Usuario = usuario;
                return View("Checkout", viewModel);
            }

            var carritoOk = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            int idFactura = 0;
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // 1. Insertar la factura y obtener el id generado
                idFactura = connection.QuerySingle<int>(
                    "SP_InsertarFactura",
                    new
                    {
                        fechaFactura = DateTime.Now,
                        totalFactura = carritoOk.Sum(x => x.PrecioEfectivo * x.Cantidad),
                        idUsuario = idUsuario
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                // 2. Insertar cada venta asociada a la factura
                foreach (var item in carritoOk)
                {
                    // Insertar la venta y obtener el idVenta generado
                    int idVenta = connection.QuerySingle<int>(
                        "SP_InsertarVenta",
                        new
                        {
                            fechaVenta = DateTime.Now,
                            cantidad = item.Cantidad,
                            total = item.PrecioEfectivo * item.Cantidad,
                            idUsuario = idUsuario,
                            idProducto = (item.Tipo == "producto") ? item.IdProducto : (int?)null,
                            idArreglo = (item.Tipo == "arreglo") ? item.IdProducto : (int?)null,
                            tipoEntrega = tipoEntrega,
                            metodoPago = metodoPago,
                            idFactura = idFactura,
                            idPromocion = (item.IdPromocion > 0) ? item.IdPromocion : (int?)null,
                            precioOriginal = item.PrecioOriginal,
                            descuentoAplicado = item.DescuentoAplicado
                        },
                        commandType: System.Data.CommandType.StoredProcedure
                    );

                    // Si es un producto individual, rebajar stock
                    if (item.Tipo == "producto")
                    {
                        connection.Execute(
                            "SP_ActualizarStockProducto",
                            new { idProducto = item.IdProducto, cantidadVendida = item.Cantidad },
                            commandType: System.Data.CommandType.StoredProcedure
                        );
                    }

                    // Si es un arreglo, guardar los productos que lo componen en VentaProducto y rebajar stock
                    if (item.Tipo == "arreglo")
                    {
                        var productosArreglo = connection.Query<ArregloProductoModel>(
                            "SP_ObtenerProductosPorArreglo",
                            new { idArreglo = item.IdProducto },
                            commandType: System.Data.CommandType.StoredProcedure
                        );

                        foreach (var prod in productosArreglo)
                        {
                            int cantidadTotal = prod.cantidadProducto * item.Cantidad;
                            try
                            {
                                connection.Execute(
                                    "SP_InsertarVentaProducto",
                                    new { idVenta = idVenta, idProducto = prod.idProducto, cantidad = cantidadTotal },
                                    commandType: System.Data.CommandType.StoredProcedure
                                );
                                // Rebajar stock del producto del arreglo
                                connection.Execute(
                                    "SP_ActualizarStockProducto",
                                    new { idProducto = prod.idProducto, cantidadVendida = cantidadTotal },
                                    commandType: System.Data.CommandType.StoredProcedure
                                );
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error al insertar en VentaProducto: {ex.Message}");
                                throw;
                            }
                        }
                    }
                }
            }

            // Registrar actividad en el historial
            var usuarioSesion = HttpContext.Session.GetString("Usuario") ?? "Sistema";
            var totalFactura = carritoOk.Sum(x => x.PrecioEfectivo * x.Cantidad);
            var totalDescuentos = carritoOk.Sum(x => x.DescuentoAplicado);
            AuditoriaHelper.RegistrarActividad(
                tipoActividad: "Crear",
                modulo: "Venta",
                descripcion: $"Nueva venta procesada - Factura #{idFactura}",
                usuario: usuarioSesion,
                detalles: $"Total: ₡{totalFactura:N0}, Descuentos: ₡{totalDescuentos:N0}, Items: {carritoOk.Count}, Entrega: {tipoEntrega}, Pago: {metodoPago}"
            );

            // Limpiar el carrito después de confirmar
            HttpContext.Session.Remove("Carrito");
            return RedirectToAction("Factura", new { id = idFactura });
        }

        [HttpGet]
        public IActionResult Factura(int id)
        {
            // Obtener datos de la factura y ventas asociadas
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var factura = connection.QuerySingleOrDefault<Factura>(
                    "SP_ObtenerFacturaPorId",
                    new { idFactura = id },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                var ventas = connection.Query<Venta>(
                    "SP_ObtenerVentasPorFactura",
                    new { idFactura = id },
                    commandType: System.Data.CommandType.StoredProcedure
                ).ToList();
                var viewModel = new DaPazWebApp.Models.FacturaViewModel
                {
                    Factura = factura,
                    Ventas = ventas
                };
                return View(viewModel);
            }
        }

        [HttpGet]
        public IActionResult HistorialUsuario()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null || idUsuario == 0)
            {
                return RedirectToAction("Login", "Login");
            }
            var viewModel = new HistorialCompraViewModel();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Obtener todas las facturas del usuario
                var facturas = connection.Query<Factura>(
                    "SP_ObtenerFacturasPorUsuario",
                    new { idUsuario },
                    commandType: System.Data.CommandType.StoredProcedure
                ).ToList();
                viewModel.Facturas = facturas;
                // Obtener ventas por cada factura
                foreach (var factura in facturas)
                {
                    var ventas = connection.Query<Venta>(
                        "SP_ObtenerVentasPorFactura",
                        new { idFactura = factura.idFactura },
                        commandType: System.Data.CommandType.StoredProcedure
                    ).ToList();
                    viewModel.VentasPorFactura[factura.idFactura] = ventas;
                }
            }
            return View(viewModel);
        }
        
        [HttpGet]
        public IActionResult HistorialGlobal()
        {
            var idRol = HttpContext.Session.GetInt32("IdRol");
            if (idRol != 1 && idRol != 3)
            {
                return RedirectToAction("Login", "Login");
            }
            var viewModel = new HistorialCompraViewModel();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Obtener todas las facturas del sistema
                var facturas = connection.Query<Factura>(
                    "SP_ObtenerTodasLasFacturas",
                    commandType: System.Data.CommandType.StoredProcedure
                ).ToList();
                viewModel.Facturas = facturas;
                // Obtener ventas por cada factura
                foreach (var factura in facturas)
                {
                    var ventas = connection.Query<Venta>(
                        "SP_ObtenerVentasPorFactura",
                        new { idFactura = factura.idFactura },
                        commandType: System.Data.CommandType.StoredProcedure
                    ).ToList();
                    viewModel.VentasPorFactura[factura.idFactura] = ventas;
                }
            }
            return View(viewModel);
        }
    }
}