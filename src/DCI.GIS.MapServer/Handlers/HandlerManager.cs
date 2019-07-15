using System.Collections.Generic;
using DCI.GIS.MapServer.Configuration;

namespace DCI.GIS.MapServer.Handlers
{
    public class HandlerManager : IHandlerManager
    {
        public HandlerManager(MapServerConfig config)
        {
            _config = config;

            _defaultHandler = new EsriBuddleFileHandler(config.DefalutFileBasePath);

            Init();
        }



        private readonly MapServerConfig _config;
        private readonly IDictionary<string, IServiceHandler> handlerCaches = new Dictionary<string, IServiceHandler>();
        private readonly EsriBuddleFileHandler _defaultHandler;
        
        public void Init()
        {
            foreach (var serviceConfig in _config.Services)
            {
                var handler = CreateHandlerByType(serviceConfig.Type);
                handler.Init(serviceConfig);
                handlerCaches.Add(serviceConfig.Name, handler);
            }
        }

        private IServiceHandler CreateHandlerByType(string type)
        {
            switch(type)
            {
                case "Proxy":
                    return new WmtsProxyHandler();
                default:
                    
                    break;
            }
            return null;
        }

        public IServiceHandler GetHandler(string name)
        {
            if (handlerCaches.ContainsKey(name))
            {
                return handlerCaches[name];
            }
            return _defaultHandler;
        }
    }
}