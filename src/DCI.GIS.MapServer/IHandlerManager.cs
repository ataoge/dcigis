namespace DCI.GIS.MapServer
{
    public interface IHandlerManager
    {
        IServiceHandler GetHandler(string name);
    }
}