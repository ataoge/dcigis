namespace DCI.GIS.MapServer.Handlers
{
    internal class BuddleSetting
    {
        public string TilePath {get; set;}

        public bool IsTpk {get; set;}

        public bool IsVector {get; set;} 

        public int Version {get; set;}  = 2;
    }
}