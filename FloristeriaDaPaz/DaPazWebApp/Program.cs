using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Registrar servicios de MVC y autenticaci�n
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";  // O la ruta de tu acci�n login GET
        options.AccessDeniedPath = "/Login/AccessDenied"; // Opcional, para acceso denegado
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddSession();  // Si usas sesi�n

var app = builder.Build();

// Middleware pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();  // �IMPORTANTE! Debe ir antes de UseAuthorization
app.UseAuthorization();

app.UseSession(); // Si usas sesiones

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
