namespace DCI.GIS.MapServer
{
    public interface IHandlerManager: ICacheManager
    {
        IServiceHandler GetHandler(string name);
    }
}