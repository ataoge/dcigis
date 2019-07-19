using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DCI.GIS.MapServer.Handlers
{
    public class WmtsProxyHandler : IServiceHandler
    {
        public WmtsProxyHandler(IHttpClientFactory clientFactory)
        {
            _httpClient = clientFactory.CreateClient();
        }

        private readonly HttpClient _httpClient;
        private string _domains;

        public void Init(ServiceConfig config)
        {
            var wmtsConfig = config as WmtsServiceConfig; 
            if (wmtsConfig != null)   
            {
                _defaultZoomOffset = wmtsConfig.ZoomOffset;
                _domains = wmtsConfig["Domains"];
            }
            
            _proxyUrl = config.Url;
        }

        private int _defaultZoomOffset;
        private string _proxyUrl;

        public async Task<bool> Handle(HttpContext context, string serviceName, string format)
        {
            var zoomOffset = context.GetIntParam("ZoomOffset");
            if (zoomOffset == 0)
                zoomOffset = _defaultZoomOffset;
            var zoom = Convert.ToInt32(context.GetRouteValue("z")) + zoomOffset;
            var tx = Convert.ToInt32(context.GetRouteValue("x"));
            var ty = Convert.ToInt32(context.GetRouteValue("y"));


            var uriString = string.Format(_proxyUrl, zoom, tx, ty);
            var requestMessage = new HttpRequestMessage();
            var requestMethod = context.Request.Method;
            requestMessage.RequestUri = new Uri(uriString);

   
            requestMessage.Method = new HttpMethod(context.Request.Method);
            using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                foreach (var header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
                context.Response.Headers.Remove("transfer-encoding");
                await responseMessage.Content.CopyToAsync(context.Response.Body);
            }
            return true;
        }

        public Task HandleCapabilities(HttpContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}