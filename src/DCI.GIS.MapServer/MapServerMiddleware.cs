using System.Collections.Generic;
using System.Threading.Tasks;
using DCI.GIS.MapServer;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DCI.GIS
{
    public class MapServerMiddleware : IMiddleware
    {
        //private readonly RequestDelegate _next;

        public MapServerMiddleware(/* IOptions<MapServerConfig> optionAccessor, */
                                      IHandlerManager manager,
                                      ILogger<MapServerMiddleware> logger)
        {
            //_next = next;
            //_options = optionAccessor.Value;
            _logger = logger;
            _manager = manager;
            
        }

        //private readonly MapServerConfig _options;
        private readonly ILogger _logger;
        private readonly IHandlerManager _manager;

        
        private void InitHandlers()
        {
            
             /*            var aa= System.Uri.IsWellFormedUriString(service.Url, System.UriKind.Absolute);
                         var bb= System.Uri.CheckSchemeName("file");
                        if (System.IO.Directory.Exists(service.Url))
                        {

                        }
             */
           
        }
        //public async Task InvokeAsync(HttpContext context)
        //{
        
            //await _next(context);
        //}

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var url = context.Request.Path;
           
            _logger.LogInformation("Handleb Url {url}", url);
            var serviceName = context.GetRouteValue("serviceName").ToString();
            var handler = _manager.GetHandler(serviceName);
            
            if (url.StartsWithSegments("/wmts/" + serviceName + "/tile", System.StringComparison.CurrentCultureIgnoreCase))
            {
                var format = context.GetRouteValue("format").ToString();
                
                if (handler != null)
                {
                    if (await handler.Handle(context, serviceName, format))
                        return;
                }
            }

            await next(context);
        }
    }
}