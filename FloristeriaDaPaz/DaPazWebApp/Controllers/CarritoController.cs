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
                // Validar stock para productos
                if (Tipo == "producto")
                {
                    using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                    {
                        var producto = connection.QuerySingleOrDefault<Producto>(
                            "SP_ObtenerProductoPorId",
                            new { IdProducto = IdProducto },
                            commandType: System.Data.CommandType.StoredProcedure
                        );
                        
                        if (producto != null && Cantidad > (producto.Stock ?? 0))
                        {
                            TempData["ErrorMensaje"] = $"⚠️ Stock insuficiente. Solo quedan {producto.Stock ?? 0} unidades disponibles de '{item.NombreProducto}'. Reduce la cantidad en el carrito.";
                            return RedirectToAction("Cart");
                        }
                    }
                }
                else if (Tipo == "arreglo")
                {
                    // Validar stock de productos del arreglo
                    using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                    {
                        var productosArreglo = connection.Query(
                            "SELECT p.IdProducto, p.NombreProducto, p.Stock, ap.cantidadProducto as cantidad FROM ArregloProducto ap " +
                            "INNER JOIN Producto p ON ap.idProducto = p.IdProducto " +
                            "WHERE ap.idArreglo = @IdArreglo",
                            new { IdArreglo = IdProducto }
                        ).ToList();
                        
                        foreach (var prodArreglo in productosArreglo)
                        {
                            int cantidadNecesaria = prodArreglo.cantidad * Cantidad;
                            if (prodArreglo.Stock < cantidadNecesaria)
                            {
                                TempData["ErrorMensaje"] = $"⚠️ Stock insuficiente para el arreglo '{item.NombreProducto}'. El producto '{prodArreglo.NombreProducto}' requiere {cantidadNecesaria} unidades pero solo quedan {prodArreglo.Stock} disponibles.";
                                return RedirectToAction("Cart");
                            }
                        }
                    }
                }
                
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
                int stockDisponible = 0;

                if (tipo == "arreglo")
                {
                    var arreglo = connection.QuerySingleOrDefault<Arreglo>(
                        "SP_ObtenerArregloPorId",
                        new { IdArreglo = idProducto },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    if (arreglo == null)
                        return NotFound();
                        
                    // Validar stock de productos del arreglo
                    var productosArreglo = connection.Query(
                        "SELECT p.IdProducto, p.NombreProducto, p.Stock, ap.cantidadProducto as cantidad FROM ArregloProducto ap " +
                        "INNER JOIN Producto p ON ap.idProducto = p.IdProducto " +
                        "WHERE ap.idArreglo = @IdArreglo",
                        new { IdArreglo = idProducto }
                    ).ToList();
                    
                    foreach (var prodArreglo in productosArreglo)
                    {
                        int cantidadNecesaria = prodArreglo.cantidad * cantidad;
                        if (prodArreglo.Stock < cantidadNecesaria)
                        {
                            TempData["ErrorMensaje"] = $"⚠️ Stock insuficiente para el arreglo. El producto '{prodArreglo.NombreProducto}' requiere {cantidadNecesaria} unidades pero solo quedan {prodArreglo.Stock} disponibles. Reduce la cantidad solicitada.";
                            return RedirectToAction("DetallesAV", "Arreglo", new { id = idProducto });
                        }
                    }
                    
                    nombre = arreglo.nombreArreglo;
                    imagen = arreglo.imagen ?? string.Empty;
                    precio = arreglo.precio;
                    id = arreglo.idArreglo;
                    stockDisponible = int.MaxValue; // Para arreglos, ya validamos los componentes
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
                    stockDisponible = producto.Stock ?? 0;
                    
                    // Validar stock disponible para productos
                    var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
                    var itemExistente = carrito.FirstOrDefault(x => x.IdProducto == id && x.Tipo == tipo);
                    int cantidadEnCarrito = itemExistente?.Cantidad ?? 0;
                    int cantidadTotal = cantidadEnCarrito + cantidad;
                    
                    if (cantidadTotal > stockDisponible)
                    {
                        if (cantidadEnCarrito > 0)
                        {
                            TempData["ErrorMensaje"] = $"⚠️ Stock insuficiente. Solo quedan {stockDisponible} unidades disponibles de '{nombre}' y ya tienes {cantidadEnCarrito} en tu carrito. Solo puedes agregar {stockDisponible - cantidadEnCarrito} unidades más.";
                        }
                        else
                        {
                            TempData["ErrorMensaje"] = $"⚠️ Stock insuficiente. Solo quedan {stockDisponible} unidades disponibles de '{nombre}'. Reduce la cantidad solicitada.";
                        }
                        return RedirectToAction("DetallesPV", "Producto", new { id = idProducto });
                    }
                }

                var carritoActual = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
                var item = carritoActual.FirstOrDefault(x => x.IdProducto == id && x.NombreProducto == nombre && x.Tipo == tipo);
                
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

                    carritoActual.Add(nuevoItem);
                }
                HttpContext.Session.SetObjectAsJson("Carrito", carritoActual);
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
                // Usar el SP que incluye información de ubicación
                usuario = connection.QuerySingleOrDefault<UsuarioConsultaModel>(
                    "SP_ObtenerUsuarioConUbicacion",
                    new { idUsuario },
                    commandType: System.Data.CommandType.StoredProcedure
                );
            }
            
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            var viewModel = new CarritoViewModel { Items = carrito };
            
            // Si el usuario tiene distrito asignado, obtener el costo de envío
            if (usuario?.idDistrito.HasValue == true)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var costoEnvio = connection.QueryFirstOrDefault<decimal?>(
                        "SELECT costoEnvio FROM dbo.Distrito WHERE idDistrito = @idDistrito",
                        new { idDistrito = usuario.idDistrito },
                        commandType: System.Data.CommandType.Text
                    );
                    
                    viewModel.CostoEnvio = costoEnvio ?? 0;
                    viewModel.NombreDistrito = usuario.nombreDistrito ?? "";
                    
                    // Log de debugging
                    Console.WriteLine($"DEBUG - Usuario tiene distrito: {usuario.idDistrito}");
                    Console.WriteLine($"DEBUG - Costo de envío consultado: {costoEnvio}");
                    Console.WriteLine($"DEBUG - Costo asignado al modelo: {viewModel.CostoEnvio}");
                }
            }
            else
            {
                Console.WriteLine($"DEBUG - Usuario NO tiene distrito configurado");
                viewModel.CostoEnvio = 0;
            }
            
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

            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            
            // VALIDACIÓN DE STOCK ANTES DE CONFIRMAR PEDIDO
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                foreach (var item in carrito)
                {
                    if (item.Tipo == "producto")
                    {
                        var producto = connection.QuerySingleOrDefault<Producto>(
                            "SP_ObtenerProductoPorId",
                            new { IdProducto = item.IdProducto },
                            commandType: System.Data.CommandType.StoredProcedure
                        );
                        
                        if (producto == null || item.Cantidad > (producto.Stock ?? 0))
                        {
                            TempData["ErrorMensaje"] = $"Stock insuficiente para '{item.NombreProducto}'. Solo hay {producto?.Stock ?? 0} unidades disponibles.";
                            return RedirectToAction("Cart");
                        }
                    }
                    else if (item.Tipo == "arreglo")
                    {
                        var productosArreglo = connection.Query(
                            "SELECT p.IdProducto, p.NombreProducto, p.Stock, ap.cantidadProducto as cantidad FROM ArregloProducto ap " +
                            "INNER JOIN Producto p ON ap.idProducto = p.IdProducto " +
                            "WHERE ap.idArreglo = @IdArreglo",
                            new { IdArreglo = item.IdProducto }
                        ).ToList();
                        
                        foreach (var prodArreglo in productosArreglo)
                        {
                            int cantidadNecesaria = prodArreglo.cantidad * item.Cantidad;
                            if (prodArreglo.Stock < cantidadNecesaria)
                            {
                                TempData["ErrorMensaje"] = $"Stock insuficiente para el arreglo '{item.NombreProducto}'. El producto '{prodArreglo.NombreProducto}' requiere {cantidadNecesaria} unidades pero solo hay {prodArreglo.Stock} disponibles.";
                                return RedirectToAction("Cart");
                            }
                        }
                    }
                }
            }

            // Validación backend para evitar error 400
            if (string.IsNullOrEmpty(tipoEntrega) || string.IsNullOrEmpty(metodoPago))
            {
                ModelState.AddModelError("", "Debe seleccionar tipo de entrega y método de pago.");
                var viewModel = new CarritoViewModel { Items = carrito };
                
                UsuarioConsultaModel? usuario;
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    usuario = connection.QuerySingleOrDefault<UsuarioConsultaModel>(
                        "SP_ObtenerUsuarioConUbicacion",
                        new { idUsuario },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    
                    // Obtener costo de envío si tiene distrito
                    if (usuario?.idDistrito.HasValue == true)
                    {
                        var costoEnvioTemp = connection.QueryFirstOrDefault<decimal?>(
                            "SELECT costoEnvio FROM dbo.Distrito WHERE idDistrito = @idDistrito",
                            new { idDistrito = usuario.idDistrito },
                            commandType: System.Data.CommandType.Text
                        );
                        
                        viewModel.CostoEnvio = costoEnvioTemp ?? 0;
                        viewModel.NombreDistrito = usuario.nombreDistrito ?? "";
                    }
                }
                ViewBag.Usuario = usuario;
                return View("Checkout", viewModel);
            }

            // Validación para requerir ubicación si selecciona envío a domicilio
            if (tipoEntrega?.ToLower() == "domicilio")
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var usuario = connection.QuerySingleOrDefault<UsuarioConsultaModel>(
                        "SP_ObtenerUsuarioConUbicacion",
                        new { idUsuario },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    
                    if (usuario?.idDistrito == null)
                    {
                        ModelState.AddModelError("", "Debe configurar su ubicación completa (provincia, cantón y distrito) para solicitar envío a domicilio.");
                        var viewModel = new CarritoViewModel { Items = carrito };
                        ViewBag.Usuario = usuario;
                        return View("Checkout", viewModel);
                    }
                }
            }

            var carritoOk = HttpContext.Session.GetObjectFromJson<List<CarritoItem>>("Carrito") ?? new List<CarritoItem>();
            
            // Calcular el costo de envío si es entrega a domicilio
            decimal costoEnvio = 0;
            if (tipoEntrega?.ToLower() == "domicilio")
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    // Obtener el distrito del usuario
                    var usuario = connection.QuerySingleOrDefault(
                        "SELECT idDistrito FROM dbo.Usuario WHERE idUsuario = @idUsuario",
                        new { idUsuario },
                        commandType: System.Data.CommandType.Text
                    );
                    
                    if (usuario?.idDistrito != null)
                    {
                        costoEnvio = connection.QueryFirstOrDefault<decimal?>(
                            "SELECT costoEnvio FROM dbo.Distrito WHERE idDistrito = @idDistrito",
                            new { idDistrito = usuario.idDistrito },
                            commandType: System.Data.CommandType.Text
                        ) ?? 0;
                    }
                }
            }
            
            decimal subtotalProductos = carritoOk.Sum(x => x.PrecioEfectivo * x.Cantidad);
            decimal totalConEnvio = subtotalProductos + costoEnvio;
            
            int idFactura = 0;
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // DEBUG: Ver valores antes de insertar
                System.Diagnostics.Debug.WriteLine($"DEBUG INSERTAR - Subtotal productos: {subtotalProductos}");
                System.Diagnostics.Debug.WriteLine($"DEBUG INSERTAR - Costo envío: {costoEnvio}");
                System.Diagnostics.Debug.WriteLine($"DEBUG INSERTAR - Total con envío: {totalConEnvio}");
                System.Diagnostics.Debug.WriteLine($"DEBUG INSERTAR - Tipo entrega: {tipoEntrega}");
                
                // 1. Insertar la factura con el total que incluye costo de envío
                idFactura = connection.QuerySingle<int>(
                    "SP_InsertarFactura",
                    new
                    {
                        fechaFactura = DateTime.Now,
                        totalFactura = totalConEnvio,
                        idUsuario = idUsuario,
                        costoEnvio = costoEnvio
                    },
                    commandType: System.Data.CommandType.StoredProcedure
                );
                // 2. Insertar cada venta asociada a la factura
                foreach (var item in carritoOk)
                {
                    // Insertar la venta sin costo de envío (ahora está en Factura)
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
                
                // Verificar si hay envío a domicilio
                bool tieneEnvioADomicilio = ventas.Any(v => v.tipoEntrega?.ToLower() == "domicilio");
                
                // Obtener costo de envío y distrito desde la factura
                decimal costoEnvio = factura?.costoEnvio ?? 0;
                string? nombreDistrito = null;
                
                // DEBUG: Ver qué valores tenemos
                System.Diagnostics.Debug.WriteLine($"DEBUG FACTURA - ID: {factura?.idFactura}");
                System.Diagnostics.Debug.WriteLine($"DEBUG FACTURA - Total: {factura?.totalFactura}");
                System.Diagnostics.Debug.WriteLine($"DEBUG FACTURA - Costo Envío desde BD: {factura?.costoEnvio}");
                System.Diagnostics.Debug.WriteLine($"DEBUG FACTURA - Costo Envío calculado: {costoEnvio}");
                
                // Calcular subtotal de productos (total factura menos costo de envío)
                decimal subtotalProductos = (factura?.totalFactura ?? 0) - costoEnvio;
                
                // Si hay costo de envío, obtener el nombre del distrito
                if (costoEnvio > 0 && factura != null)
                {
                    var usuario = connection.QuerySingleOrDefault<UsuarioConsultaModel>(
                        "SP_ObtenerUsuarioConUbicacion",
                        new { idUsuario = factura.idUsuario },
                        commandType: System.Data.CommandType.StoredProcedure
                    );
                    
                    nombreDistrito = usuario?.nombreDistrito;
                }
                
                var viewModel = new DaPazWebApp.Models.FacturaViewModel
                {
                    Factura = factura,
                    Ventas = ventas,
                    CostoEnvio = costoEnvio,
                    NombreDistrito = nombreDistrito,
                    TieneEnvioADomicilio = tieneEnvioADomicilio,
                    SubtotalProductos = subtotalProductos
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