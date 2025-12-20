using DoAnLTWHQT.App_Start;
using System;
using System.Web.Http;
using System.Web.Http.Cors;

namespace DoAnLTWHQT
{
    public class WebApiConfig
    {

        public static void Register(HttpConfiguration config)
        {
            // ========== QUAN TRỌNG: Enable CORS ==========
            var cors = new EnableCorsAttribute(
                origins: "http://localhost:5173",  // URL của Vue dev server
                headers: "*",
                methods: "*"
            );
            cors.SupportsCredentials = true;  // Cho phép gửi cookies
            config.EnableCors(cors);

            // Attribute routing
            config.MapHttpAttributeRoutes();

            // Convention-based routing
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Đảm bảo trả về JSON
            config.Formatters.JsonFormatter.SupportedMediaTypes
                .Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/html"));

            // Xử lý OPTIONS request (preflight)
            config.MessageHandlers.Add(new PreflightRequestsHandler());

        }

    }
}