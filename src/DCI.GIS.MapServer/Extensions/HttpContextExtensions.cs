namespace Microsoft.AspNetCore.Http
{
    internal static class HttpContextExtensions
    {
        public static int GetIntParam(this HttpContext context, string name, int defaultValue = 0)
        {
            if (context.Request.Query.ContainsKey(name))
            {
                int returnValue;
                if (int.TryParse(context.Request.Query[name], out returnValue))
                    return returnValue;
            }
            return defaultValue;
        }

        public static string GetStringParam(this HttpContext context, string name, string defaultValue = null)
        {
            if (context.Request.Query.ContainsKey(name))
            {
                return context.Request.Query[name];
            }
            return defaultValue;
        }
    }
}