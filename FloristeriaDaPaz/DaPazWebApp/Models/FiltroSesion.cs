using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace DaPazWebApp.Models
{
    public class FiltroSesion : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var tokenSesion = context.HttpContext.Session.GetString("TokenSesion");
            var idUsuario = context.HttpContext.Session.GetInt32("IdUsuario");

            // Si no hay token de sesión o ID de usuario, redirigir al login
            if (string.IsNullOrEmpty(tokenSesion) || idUsuario == null || idUsuario == 0)
            {
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToRouteResult(new { controller = "Login", action = "Login" });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}