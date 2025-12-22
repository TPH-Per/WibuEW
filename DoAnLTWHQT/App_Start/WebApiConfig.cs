using DoAnLTWHQT.App_Start;
using System;
using System.Web.Http;

namespace DoAnLTWHQT
{
    public class WebApiConfig
    {

        public static void Register(HttpConfiguration config)
        {
            // ========== CORS được xử lý trong Global.asax.cs ==========
            // Không enable CORS ở đây để tránh duplicate headers
            // CORS headers được add trong Application_BeginRequest

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

            // Xử lý OPTIONS request (preflight) - cũng đã xử lý trong Global.asax.cs
            // config.MessageHandlers.Add(new PreflightRequestsHandler());

        }

    }
}
