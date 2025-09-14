
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using DinkToPdf;
using DinkToPdf.Contracts;
using DaPazWebApp.Helpers;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(); // Ya está registrado una vez, puedes eliminar la duplicación.
builder.Services.AddHttpClient(); // <-- Agrega esto para IHttpClientFactory

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";  // O la ruta de tu acción login GET
        options.AccessDeniedPath = "/Login/AccessDenied"; // Opcional, para acceso denegado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddSession();

// Configurar DinkToPdf para generación de PDFs en Azure
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Registrar PdfHelper como servicio
builder.Services.AddScoped<DaPazWebApp.Helpers.PdfHelper>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10 MB
});

var cultureInfo = new CultureInfo("es-CR");
cultureInfo.NumberFormat.NumberDecimalSeparator = ".";
cultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";

CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

// Configurar el AuditoriaHelper con la configuración
AuditoriaHelper.SetConfiguration(app.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Habilitar el middleware de sesión ANTES de autenticación/autorización.
app.UseAuthentication();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Configuración de Rotativa para Azure y desarrollo
IWebHostEnvironment env = app.Environment;
try
{
    if (env.IsProduction())
    {
        // En Azure App Service - usar configuración sin path específico
        // Azure App Service no permite ejecutar archivos .exe personalizados
        // Usar la configuración básica de Rotativa
        Rotativa.AspNetCore.RotativaConfiguration.Setup(env.ContentRootPath);
        
        // Configurar variables de entorno necesarias para Azure
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        Environment.SetEnvironmentVariable("ROTATIVA_PATH", env.ContentRootPath);
    }
    else
    {
        // En desarrollo, usar la configuración original
        Rotativa.AspNetCore.RotativaConfiguration.Setup(env.WebRootPath, "../Rotativa/Windows");
    }
}
catch (Exception ex)
{
    // Log el error pero no impedir que la aplicación inicie
    Console.WriteLine($"Warning: Rotativa configuration failed: {ex.Message}");
}

app.Run();
