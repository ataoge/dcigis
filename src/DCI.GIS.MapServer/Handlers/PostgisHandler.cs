using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ataoge.Data;
using Ataoge.GisCore.Utilities;
using Ataoge.GisCore.Wmts;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;

namespace DCI.GIS.MapServer.Handlers
{
    public class PostgisHandler : IServiceHandler
    {
        public PostgisHandler(Database database, ICacheManager cacheManager)
        {
            this.Database = database;
            _cacheManager = cacheManager;
        }

        private ICacheManager _cacheManager;
        public Database Database {get;}

        private int _defaultEpsg = 3857;
        private string _defaultFid;

        public void Init(ServiceConfig config)
        {
            // Ataoge.GisCore.Utilities.CommonGisTools.GetTileExtent
            if (config["Epsg"] != null)
                _defaultEpsg = int.Parse(config["Epsg"]);

            if (config["Fid"] != null)
                _defaultFid = config["Fid"];    
        }

        private static Dictionary<string, object> sequenceLockNames = new Dictionary<string, object>();
        public object GetLockObject(String name)
        {
            object lockObject = null;
            if (sequenceLockNames.ContainsKey(name))
            {
                lockObject = sequenceLockNames[name];
            }
            else
            {
                lock (sequenceLockNames)
                {
                    if (sequenceLockNames.ContainsKey(name))
                    {
                        lockObject = sequenceLockNames[name];
                    }
                    else
                    {
                        sequenceLockNames[name] = lockObject = new Object();
                    }
                }

            }
            return lockObject;
        } 

        public void RemoveLockObject(string name)
        {
            if (sequenceLockNames.ContainsKey(name))
            {
                lock (sequenceLockNames)
                {
                    sequenceLockNames.Remove(name);
                }
            }
        }

        public Task HandleCapabilities(HttpContext context)
        {
            throw new NotImplementedException();
        }
        
        public async Task<bool> Handle(HttpContext context, string serviceName, string format)
        {
            if (serviceName.IndexOf(".") > 0)
            {
                serviceName = serviceName.Split(".")[1];
            }

            if (format == "json")
            {
                var properties = context.GetStringParam("properties");
                if (properties == null)
                {
                    properties = _defaultFid;
                }
                else
                {
                    properties = _defaultFid + "," +properties;
                }
                var sqlQuery = string.Format("SELECT {0}, shape FROM {1} WHERE objectid_1 = 1", properties, serviceName);
                var sqlFeature = $@"SELECT jsonb_build_object(
                                     'type', 'Feature', 
                                     'id', {_defaultFid}, 
                                     'geometry', ST_AsGeoJSON(shape)::jsonb,
                                     'properties', to_jsonb(tt) - '{_defaultFid}' - 'shape'
                                    ) AS feature FROM ({sqlQuery}) tt";
                var sqlFeatures = $@"SELECT jsonb_build_object(
                                     'type', 'FeatureCollection',
                                     'Features', jsonb_agg(features.feature)
                                    ) FROM ({sqlFeature}) features";
                string result = "";
                using (var conn = Database.CreateConnection())
                {

                    var command = Database.Factory.CreateCommand();
                    command.Connection = conn;
                    command.CommandText = sqlFeatures;

                    result = (await command.ExecuteScalarAsync(context.RequestAborted)).ToString();
                }

                if (result!=null)
                {


                    var response = context.Response;

                    response.StatusCode = StatusCodes.Status200OK;
                    //response.ContentLength = result.Length;
                    
                    response.ContentType = "application/json;charset=utf-8";
                    await response.WriteAsync(result, context.RequestAborted);
                    
                }

            }
            else 
            {
                var zoom = Convert.ToInt32(context.GetRouteValue("z"));
                var tx = Convert.ToInt32(context.GetRouteValue("y"));
                var ty = Convert.ToInt32(context.GetRouteValue("x"));
                var tileExtent = GetTileExtent(zoom, tx, ty, -4823200.0, 7002100.0,  19473.372280077896);

                var fid = string.Format("t.{0}", _defaultFid);
                var sb = new StringBuilder();
                var properties = context.GetStringParam("properties");
                if (properties != null)
                {
                    foreach (var property in properties.Split(","))
                    {
                        sb.Append(",");
                        sb.Append( string.Format("t.{0}", property));
                    }
                }
                
                var sql = $"select st_asmvt(tile, '{serviceName}', 4096, 'geom') from (select {fid} as \"id\" {sb.ToString()}, st_asmvtgeom(t.shape, box2d(st_makeenvelope({tileExtent}, {_defaultEpsg})),4096, 64, true) as geom from {serviceName} t) as tile where tile.geom is not null";
                var key = serviceName  + "_" + Ataoge.GisCore.Wmts.ArcGISBundleFileHelper.TileXYToQuadKey(tx, ty, zoom);
                byte[] result = _cacheManager.GetFromCache(key);
                if (result == null)
                {
                    lock (GetLockObject(key))
                    {
                        result = _cacheManager.GetFromCache(key);
                        if (result == null)
                        {
                            using (var conn = Database.CreateConnection())
                            {

                                var command =Database.Factory.CreateCommand();
                                command.Connection = conn;
                                command.CommandText = sql;
                            
                                result = (byte[])command.ExecuteScalar();
                                if (result != null)
                                    _cacheManager.AddToCache(key, result);
                            
                            }
                            RemoveLockObject(key);
                        }
                    }
                }

                if (result!=null)
                {


                    var response = context.Response;

                    response.StatusCode = StatusCodes.Status200OK;
                    response.ContentLength = result.Length;

                    response.ContentType = "application/x-protobuf";

                    var outputStream = response.Body;
                    using (var inputStream = new MemoryStream(result))
                    {
                        try
                        {
                            await StreamCopyOperation.CopyToAsync(inputStream, outputStream, count: null, bufferSize: 64 * 1024, cancel: context.RequestAborted);
                        }
                        catch (OperationCanceledException)
                        {
                            context.Abort();
                        }

                    }

                }
            }

           return true;
        }

        string GetTileExtent(int z, int x, int y, double ox = -180, double oy = 90, double? initResolution = null)
        {
            double tileSize = initResolution.HasValue ? 512 : 360;
            double res = initResolution.HasValue ?  initResolution.Value / Math.Pow(2, z) : 1 /Math.Pow(2, z);
            double minx = res * x * tileSize + ox; // x * res * 360 - 180
            double maxx = res * (x + 1) * tileSize + ox;
            double miny, maxy;
            if (initResolution.HasValue)
            {
                maxy = oy - res * y * tileSize;
                miny = oy - res * (y + 1) * tileSize;
            }
            else
            {
                // var n = Math.PI - (2.0 * Math.PI * y) / Math.Pow(2, z);  Math.Atan(Math.Sinh(n)) * 180 / Math.PI;
                maxy = Math.Atan(Math.Sinh(Math.PI - (2.0 * Math.PI * (y+1)) * res)) * 180 / Math.PI;
                miny = Math.Atan(Math.Sinh(Math.PI - (2.0 * Math.PI * y) * res)) * 180 / Math.PI;
            }

            return $"{minx}, {miny}, {maxx}, {maxy}";
        }
    }
}