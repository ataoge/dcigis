using System.Collections.Generic;

namespace DCI.GIS.MapServer.Configuration
{
    public class WmtsServiceConfig : ServiceConfig
    {
        private readonly IDictionary<string, string> innerDicts = new Dictionary<string, string>();
        
        public int ZoomOffset {get; set;} = 0;


        public IDictionary<string, string> Extensions {
            get { return this.innerDicts;}
        }

        public string this[string key]
        {
            get { return innerDicts[key]; }
            set { innerDicts[key] = value; }
        }
    }
}