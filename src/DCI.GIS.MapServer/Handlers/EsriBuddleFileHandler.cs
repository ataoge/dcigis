using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Ataoge.GisCore.Wmts;
using DCI.GIS.MapServer.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;

namespace DCI.GIS.MapServer.Handlers
{
    public class EsriBuddleFileHandler : IServiceHandler
    {
        public EsriBuddleFileHandler(string basePath)
        {
            _basePath = basePath;
        }
        private readonly string _basePath;

        private readonly IDictionary<string, BuddleSetting> _buddleSettings = new  ConcurrentDictionary<string, BuddleSetting>();

        public void Init(ServiceConfig config)
        {
            
        }

        public async Task<bool> Handle(HttpContext context, string serviceName, string format)
        {
            if (!_buddleSettings.ContainsKey(serviceName))
            {
                var newSetting = new BuddleSetting();
                var path = Path.Combine(_basePath, serviceName);
                if (Directory.Exists(path)) //是目录
                {
                    
                    newSetting.IsTpk = false;
                    bool found = false;
                    var baseDir = new DirectoryInfo(path);
                    foreach (var dir in baseDir.GetDirectories())
                    {
                        switch (dir.Name)
                        {
                            case "VectorTileServer":
                            case "p12":
                                found = true;
                                newSetting.IsVector = true;
                                newSetting.TilePath = Path.Combine(path, dir.Name, "tile");
                                break;
                            case "图层":
                            case "Layers":
                                found = true;
                                if (!File.Exists(Path.Combine(path, "conf.properties")))
                                    newSetting.Version = 1;
                                newSetting.TilePath = Path.Combine(path, dir.Name, "_alllayers");
                                break;
                            case "v101":
                                found = true;
                                var layersDir = dir.GetDirectories().First();
                                newSetting.TilePath = Path.Combine(path, dir.Name, layersDir.Name, "_alllayers");
                                break;
                            default:
                                continue;
                        }
                        if (found)
                            break;
                    }
                    if (!found)
                        return false;
                    
                }
                else
                {
                    if (string.Equals("pbf", format, StringComparison.InvariantCultureIgnoreCase))
                    {
                        path += ".vtpk";
                        newSetting.IsVector = true;
                    }
                    else
                    {
                        path += ".tpk";
                    }

                    if (!File.Exists(path))
                    {
                        return false;
                    }

                    newSetting.TilePath = path;
                    newSetting.IsTpk = true;
                }

                _buddleSettings.TryAdd(serviceName, newSetting);
            }

            var buddleSetting = _buddleSettings[serviceName];

            var zoomOffset = context.GetIntParam("ZoomOffset");
            var zoom = Convert.ToInt32(context.GetRouteValue("z")) + zoomOffset;
            var tx = Convert.ToInt32(context.GetRouteValue("x"));
            var ty = Convert.ToInt32(context.GetRouteValue("y"));

            byte[] bytes = null;
            if (buddleSetting.IsTpk) 
                bytes = GetTileFromTpk(buddleSetting.TilePath, zoom, tx, ty);
            else
                bytes = GetTileFromDirectory(buddleSetting.TilePath, zoom, tx, ty);

            if (bytes != null)
            {
                var response = context.Response;
                response.ContentType = GetContentType(format);
                response.StatusCode = StatusCodes.Status200OK;
                response.ContentLength = bytes.Length;
                response.Headers.Add("Content-Encoding", "gzip");

                var outputStream = response.Body;
                using (var inputStream = new MemoryStream(bytes))
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

            return true;
        }

        private string GetContentType(string format)
        {
            switch (format)
            {
                case "pbf":
                    return "application/x-protobuf";
                case "png":
                    return "image/png";
                case "jpg":
                case "jpeg":
                    return "image/jpeg";
                default:
                    return "application/octet-stream";
            }
        }

        private byte[] GetTileFromTpk(string filePath, int zoom, int tx, int ty, int tileSize = 256)
        {
            return ArcGISBundleFileHelper.GetTileImage_VTPK(zoom, tx, ty, filePath);
        }

        private byte[] GetTileFromDirectory(string basePath, int zoom, int tx, int ty)
        {
            //return ArcGISBundleFileHelper.GetTileImage_v2(zoom, tx, ty, basePath);
            int packetSize = 128;
            int startCol = (ty / packetSize) * packetSize;
            int startRow = (tx / packetSize) * packetSize;
            string layerPath = string.Format("L{0:D2}", zoom);
            string fileName = string.Format("R{0:x4}C{1:x4}", startRow, startCol);
            string bundleFileName = Path.Combine(basePath, layerPath, fileName + ".bundle");
            if (string.IsNullOrEmpty(bundleFileName) || !File.Exists(bundleFileName))
            {
                return null;
            }
            
            int index = packetSize * (tx - startRow) + (ty - startCol);

            FileStream fs = new FileStream(bundleFileName, FileMode.Open, FileAccess.Read);
            fs.Seek(64 + 8 * index, SeekOrigin.Begin);
                        
            //获取位置索引并计算切片位置偏移量
            byte[] indexBytes = new byte[8];
            fs.Read(indexBytes, 0, 8); 
            var indexOffset = BitConverter.ToInt64(indexBytes, 0);  
            var offset = (indexOffset << 24) >> 24;
            var size = indexOffset >> 40;
            if (size == 0)
                return null; 
          
            //获取切片长度索引并计算切片长度
            long startOffset = offset - 4; 
            fs.Seek(startOffset, SeekOrigin.Begin);
            byte[] lengthBytes = new byte[4];
            fs.Read(lengthBytes, 0, 4);
            int length = BitConverter.ToInt32(lengthBytes, 0);

            //根据切片位置和切片长度获取切片
            byte[] tileBytes = new byte[length];
            int bytesRead = fs.Read(tileBytes, 0, tileBytes.Length);
            if(bytesRead > 0){
                return tileBytes;
            }
            return null;
        }

        public Task HandleCapabilities(HttpContext context)
        {
            throw new NotImplementedException();
        }
    }
}