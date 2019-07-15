using System;
using System.Threading.Tasks;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;

namespace DCI.GIS.MapServer.Handlers
{
    public class TpkHandler : IServiceHandler
    {
        public TpkHandler(string filePath)
        {
            _filePath = filePath;
        }

        private readonly string _filePath;

        public void Init(ServiceConfig config)
        {
            
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