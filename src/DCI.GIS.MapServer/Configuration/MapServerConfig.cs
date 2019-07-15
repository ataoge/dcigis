using System.IO;

namespace DCI.GIS.MapServer.Configuration
{
    public class MapServerConfig
    {
        public string DefalutFileBasePath {get; set;} = Directory.GetCurrentDirectory();
        public string DefalutUrlBasePath {get; set;} 
        public WmtsServiceConfig[] Services {get; set;}
    }
}