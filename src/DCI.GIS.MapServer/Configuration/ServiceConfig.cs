namespace DCI.GIS.MapServer.Configuration
{
    public class ServiceConfig
    {
        public string Name {get; set;}

        public string Type {get; set;}

        public string Url {get; set;}

        public virtual string this[string key]
        {
            get { return null;}
            set {}
        }   
    }
}