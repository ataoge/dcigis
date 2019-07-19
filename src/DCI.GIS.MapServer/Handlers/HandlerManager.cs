using System;
using System.Collections.Generic;
using Ataoge.Data;
using DCI.GIS.MapServer.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace DCI.GIS.MapServer.Handlers
{
    public class HandlerManager : IHandlerManager
    {
        public HandlerManager( IServiceProvider serviceProvider, IDistributedCache memoryCache, IOptions<MapServerConfig> configAccessor)
        {
            _serviceProvider = serviceProvider;
            _cache = memoryCache;
            _config = configAccessor.Value;
            
            var basePath = _config.DefalutFileBasePath;
            if (string.IsNullOrEmpty(basePath))
                basePath = System.IO.Directory.GetCurrentDirectory();
            _defaultHandler = new EsriBuddleFileHandler(basePath);

            Init();
        }

        private readonly IDistributedCache _cache;
        private readonly IServiceProvider _serviceProvider;

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
                    var clientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
                    return new WmtsProxyHandler(clientFactory);
                case "PostGIS":
                    var database = _serviceProvider.GetService<Database>();
                    return new PostgisHandler(database, this);
                default:
                    
                    break;
            }
            return null;
        }

        public void AddToCache(string key, byte[] bytes)
        {
            _cache.Set(key, bytes);
        }

        public byte[] GetFromCache(string key)
        {
            return _cache.Get(key);
        }

        public IServiceHandler GetHandler(string name)
        {
            name = name.Split(".")[0];
            if (handlerCaches.ContainsKey(name))
            {
                return handlerCaches[name];
            }
            return _defaultHandler;
        }
    }
}