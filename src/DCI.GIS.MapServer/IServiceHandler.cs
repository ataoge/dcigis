using System.Threading.Tasks;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;

namespace DCI.GIS.MapServer
{
    public interface IServiceHandler
    {
        void Init(ServiceConfig config);

        Task HandleCapabilities(HttpContext context);
        
        Task<bool> Handle(HttpContext context, string serviceName, string format);
    }

    
}