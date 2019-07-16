using System;
using System.Threading.Tasks;
using Ataoge.GisCore.Wmts;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;

namespace DCI.GIS.MapServer.Handlers
{
    public class PostgisHandler : IServiceHandler
    {
        public PostgisHandler()
        {
            
        }

        public void Init(ServiceConfig config)
        {
           // Ataoge.GisCore.Utilities.CommonGisTools.GetTileExtent
           
        }

        public Task HandleCapabilities(HttpContext context)
        {
            throw new NotImplementedException();
        }
        
        public  Task<bool> Handle(HttpContext context, string serviceName, string format)
        {
            throw new System.NotImplementedException();
        }
    }
}