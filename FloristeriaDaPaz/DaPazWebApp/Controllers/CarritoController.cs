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
                }
                else
                {
                    carrito.Add(new CarritoItem
                    {
                        IdProducto = id,
                        NombreProducto = nombre,
                        Imagen = imagen,
                        Precio = precio,
                        Cantidad = cantidad,
                        Tipo = tipo // Guardar el tipo explícitamente
                    });
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
                        totalFactura = carritoOk.Sum(x => x.Precio * x.Cantidad),
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
                            total = item.Precio * item.Cantidad,
                            idUsuario = idUsuario,
                            idProducto = (item.Tipo == "producto") ? item.IdProducto : (int?)null,
                            idArreglo = (item.Tipo == "arreglo") ? item.IdProducto : (int?)null,
                            tipoEntrega = tipoEntrega,
                            metodoPago = metodoPago,
                            idFactura = idFactura
                        },
                        commandType: System.Data.CommandType.StoredProcedure
                    );

                    // Si es un arreglo, guardar los productos que lo componen en VentaProducto
                    if (item.Tipo == "arreglo")
                    {
                        // Obtener los productos del arreglo
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
                                // Puedes poner un breakpoint aquí o loguear los valores
                                System.Diagnostics.Debug.WriteLine($"Insertando: idVenta={idVenta}, idProducto={prod.idProducto}, cantidad={cantidadTotal}");
                                connection.Execute(
                                    "SP_InsertarVentaProducto",
                                    new { idVenta = idVenta, idProducto = prod.idProducto, cantidad = cantidadTotal },
                                    commandType: System.Data.CommandType.StoredProcedure
                                );
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error al insertar en VentaProducto: {ex.Message}");
                                throw; // O puedes manejarlo como prefieras
                            }
                        }
                    }
                }
            }
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
    }

}
