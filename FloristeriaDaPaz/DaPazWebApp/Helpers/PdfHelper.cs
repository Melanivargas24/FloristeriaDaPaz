using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;

namespace DaPazWebApp.Helpers
{
    public class PdfHelper
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _environment;
        private readonly IConverter _converter;

        public PdfHelper(IServiceProvider serviceProvider, IWebHostEnvironment environment, IConverter converter)
        {
            _serviceProvider = serviceProvider;
            _environment = environment;
            _converter = converter;
        }

        public async Task<byte[]> GeneratePdfFromView(Controller controller, string viewName, object model)
        {
            try
            {
                // Primero intentar con Rotativa (solo en desarrollo)
                var rotativaResult = await TryRotativa(controller, viewName, model);
                if (rotativaResult != null)
                    return rotativaResult;

                // Si Rotativa falla, usar DinkToPdf como alternativa
                return await GenerateWithDinkToPdf(controller, viewName, model);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generando PDF: {ex.Message}", ex);
            }
        }

        private async Task<byte[]?> TryRotativa(Controller controller, string viewName, object model)
        {
            try
            {
                // Solo usar Rotativa en desarrollo
                if (_environment.IsDevelopment())
                {
                    var viewAsPdf = new Rotativa.AspNetCore.ViewAsPdf(viewName, model)
                    {
                        PageSize = Rotativa.AspNetCore.Options.Size.A4,
                        PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
                    };
                    return await viewAsPdf.BuildFile(controller.ControllerContext);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task<byte[]> GenerateWithDinkToPdf(Controller controller, string viewName, object model)
        {
            var html = await RenderViewToString(controller, viewName, model);
            
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects = {
                    new ObjectSettings()
                    {
                        PagesCount = true,
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return _converter.Convert(doc);
        }

        private async Task<string> RenderViewToString(Controller controller, string viewName, object model)
        {
            var serviceProvider = _serviceProvider;
            var viewEngine = serviceProvider.GetService<ICompositeViewEngine>();
            var tempDataProvider = serviceProvider.GetService<ITempDataProvider>();

            using var writer = new StringWriter();
            var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);

            if (!viewResult.Success)
            {
                throw new ArgumentNullException($"View {viewName} not found");
            }

            var viewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
            {
                Model = model
            };

            var tempData = new TempDataDictionary(controller.HttpContext, tempDataProvider);

            var viewContext = new ViewContext(
                controller.ControllerContext,
                viewResult.View,
                viewData,
                tempData,
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}