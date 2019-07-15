using System.Threading.Tasks;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;

namespace DCI.GIS.MapServer.Handlers
{
    public class WmtsProxyHandler : IServiceHandler
    {
        public WmtsProxyHandler()
        {

        }

        public void Init(ServiceConfig config)
        {
            
        }

        public Task<bool> Handle(HttpContext context, string serviceName, string format)
        {
            throw new System.NotImplementedException();
        }

        public Task HandleCapabilities(HttpContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}