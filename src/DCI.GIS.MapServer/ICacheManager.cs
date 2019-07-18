namespace DCI.GIS.MapServer
{
    public interface ICacheManager
    {
        void AddToCache(string key, byte[] value);

        byte[] GetFromCache(string key);
        
    }
}