using DaPazWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace DaPazWebApp.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ChatbotController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<JsonResult> EnviarMensaje([FromBody] MensajeRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.mensaje))
            {
                return Json(new { respuesta = "Por favor ingrese un mensaje válido." });
            }

            string respuestaBot = await ObtenerRespuestaGemini(request.mensaje);

            return Json(new { respuesta = respuestaBot });
        }

        private async Task<string> ObtenerRespuestaGemini(string mensajeUsuario)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

            var client = _httpClientFactory.CreateClient();

            var payload = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text = mensajeUsuario }
                }
            }
        }
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return $"Error al contactar la API: {response.StatusCode}";
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            using var jsonDoc = JsonDocument.Parse(responseContent);
            try
            {
                var botRespuesta = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return botRespuesta ?? "No se recibió respuesta del bot.";
            }
            catch
            {
                return "Respuesta inesperada de la API.";
            }
        }

    }
}

