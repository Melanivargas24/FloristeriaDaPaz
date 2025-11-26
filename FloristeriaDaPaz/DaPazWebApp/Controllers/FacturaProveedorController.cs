using System.Data;
using DaPazWebApp.Helpers;
using Microsoft.Data.SqlClient;
using DaPazWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Dapper;
using System.Data.SqlClient;

namespace DaPazWebApp.Controllers
{
    public class FacturaProveedorController : Controller
    {
        // GET: FacturaProveedor
        private readonly IConfiguration _configuration;

        public FacturaProveedorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var facturas = new List<FacturaProveedorModel>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                facturas = connection.Query<FacturaProveedorModel>(
                    "SP_ObtenerFacturasProveedor",
                    commandType: CommandType.StoredProcedure
                ).AsList();
            }
            return View(facturas);
        }

        // GET: FacturaProveedor/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores", commandType: CommandType.StoredProcedure)
                    .Where(p => p.estado == "Activo")
                    .ToList();
                var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();
                ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");
                ViewBag.Productos = productos;
            }
            return View(new FacturaProveedorModel{
                FechaFactura = DateTime.Now,
            });
        }

        // POST: FacturaProveedor/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(FacturaProveedorModel model)
        {
            // Validar que el proveedor esté activo
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var proveedor = connection.QueryFirstOrDefault<ProveedorModel>(
                    "SELECT * FROM Proveedor WHERE IdProveedor = @IdProveedor",
                    new { IdProveedor = model.IdProveedor },
                    commandType: CommandType.Text
                );
                
                if (proveedor == null || proveedor.estado != "Activo")
                {
                    ModelState.AddModelError("IdProveedor", "El proveedor seleccionado no está activo o no existe");
                }
            }

            // Validaciones adicionales para precios negativos
            if (model.Detalles != null)
            {
                for (int i = 0; i < model.Detalles.Count; i++)
                {
                    if (model.Detalles[i].PrecioUnitario <= 0)
                    {
                        ModelState.AddModelError($"Detalles[{i}].PrecioUnitario", "El precio unitario debe ser mayor a 0");
                    }
                    if (model.Detalles[i].PrecioVenta <= 0)
                    {
                        ModelState.AddModelError($"Detalles[{i}].PrecioVenta", "El precio de venta debe ser mayor a 0");
                    }
                }
            }

            // No forzar cultura, usar la que tenga el sistema o usuario
            if (!ModelState.IsValid || model.Detalles == null || model.Detalles.Count == 0)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
                {
                    var proveedores = connection.Query<ProveedorModel>("SP_ObtenerProveedores", commandType: CommandType.StoredProcedure)
                        .Where(p => p.estado == "Activo")
                        .ToList();
                    var productos = connection.Query<Producto>("SP_ObtenerProductos", commandType: CommandType.StoredProcedure).ToList();
                    ViewBag.Proveedores = new SelectList(proveedores, "IdProveedor", "nombreProveedor");
                    ViewBag.Productos = productos;
                }

                // Mostrar errores exactos en consola
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("Error en ModelState: " + error.ErrorMessage);
                }

                // Validar valores numéricos
                if (!decimal.TryParse(model.TotalFactura.ToString(), out _))
                {
                    ModelState.AddModelError("TotalFactura", "El valor de TotalFactura no es válido. Debe ser un número decimal.");
                }
                if (model.Detalles != null)
                {
                    for (int i = 0; i < model.Detalles.Count; i++)
                    {
                        var detalle = model.Detalles[i];
                        if (!decimal.TryParse(detalle.Subtotal.ToString(), out _))
                        {
                            ModelState.AddModelError($"Detalles[{i}].Subtotal", $"El valor de Subtotal en la fila {i + 1} no es válido. Debe ser un número decimal.");
                        }
                    }
                }

                if (model.Detalles == null || model.Detalles.Count == 0)
                    ModelState.AddModelError("", "Debe agregar al menos un producto a la factura.");

                return View(model);
            }

            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                try {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IdProveedor", model.IdProveedor);
                    parameters.Add("@FechaFactura", model.FechaFactura);
                    parameters.Add("@TotalFactura", model.TotalFactura);
                    parameters.Add("@IdFacturaProveedor", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);
                    connection.Execute("SP_InsertarFacturaProveedor", parameters, commandType: CommandType.StoredProcedure);
                    int idFacturaProveedor = parameters.Get<int>("@IdFacturaProveedor");
                    Console.WriteLine($"Factura insertada: Id={idFacturaProveedor}");

                    foreach (var detalle in model.Detalles)
                    {
                        Console.WriteLine($"Insertando detalle: Producto={detalle.IdProducto}, Cantidad={detalle.Cantidad}, PrecioCompra={detalle.PrecioUnitario}, PrecioVenta={detalle.PrecioVenta}, Subtotal={detalle.Subtotal}");
                        var detalleParams = new DynamicParameters();
                        detalleParams.Add("@IdFacturaProveedor", idFacturaProveedor);
                        detalleParams.Add("@IdProducto", detalle.IdProducto);
                        detalleParams.Add("@Cantidad", detalle.Cantidad);
                        detalleParams.Add("@PrecioUnitario", detalle.PrecioUnitario);
                        detalleParams.Add("@PrecioVenta", detalle.PrecioVenta);
                        detalleParams.Add("@Subtotal", detalle.Subtotal);
                        connection.Execute("SP_InsertarDetalleFacturaProveedor", detalleParams, commandType: CommandType.StoredProcedure);
                    }

                    // Registrar actividad en el historial
                    var usuario = HttpContext.Session.GetString("Usuario") ?? "Sistema";
                    AuditoriaHelper.RegistrarActividad(
                        tipoActividad: "Crear",
                        modulo: "FacturaProveedor",
                        descripcion: $"Nueva factura de proveedor registrada",
                        usuario: usuario,
                        detalles: $"ID: {idFacturaProveedor}, Total: ₡{model.TotalFactura:N0}, Productos: {model.Detalles.Count}"
                    );
                } catch (Exception ex) {
                    Console.WriteLine($"ERROR al guardar factura: {ex.Message}");
                    ModelState.AddModelError("", "Error al guardar la factura: " + ex.Message);
                    return View(model);
                }
            }
            return RedirectToAction("Index");
        }

        // GET: FacturaProveedor/Detalle/{id}
        public IActionResult Detalle(int id)
        {
            FacturaProveedorModel? factura = null;
            List<DetalleFacturaProveedorModel> detalles = new List<DetalleFacturaProveedorModel>();
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                // Obtener la cabecera de la factura usando SP
                factura = connection.QueryFirstOrDefault<FacturaProveedorModel>(
                    "SP_ObtenerCabeceraFacturaProveedor",
                    new { IdFacturaProveedor = id },
                    commandType: CommandType.StoredProcedure);
                if (factura != null)
                {
                    // Obtener los detalles
                    detalles = connection.Query<DetalleFacturaProveedorModel>("SP_ObtenerDetalleFacturaProveedor", new { IdFacturaProveedor = id }, commandType: System.Data.CommandType.StoredProcedure).AsList();
                }
            }
            var viewModel = new FacturaProveedorDetalleViewModel
            {
                Factura = factura ?? new FacturaProveedorModel(),
                Detalles = detalles
            };
            return View(viewModel);
        }

        // GET: FacturaProveedor/ObtenerProductosPorProveedor
        [HttpGet]
        public JsonResult ObtenerProductosPorProveedor(int idProveedor)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("BDConnection")))
            {
                var productos = connection.Query<Producto>(
                    "SP_ObtenerProductosPorProveedor",
                    new { IdProveedor = idProveedor },
                    commandType: CommandType.StoredProcedure
                ).ToList();
                return Json(productos);
            }
        }
    }
}
