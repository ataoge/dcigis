using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace DCI.GIS.MapServer
{
    public class WmsProxyHandler
    {
        public WmsProxyHandler()
        {

        }

        public Task<bool> Handler(HttpContext context)
        {
            var query = QueryHelpers.ParseQuery(context.Request.QueryString.ToString());
            var items = query.SelectMany(x => x.Value, (col, value) =>
            {
                return new KeyValuePair<string, string>(col.Key, value);
            }).ToList();
            var bbox = query["bbox"].ToString();
            items.RemoveAll(x => x.Key == "bbox");
            int inSrid = 0;
            var vesrion = query["version"].ToString();
            switch (vesrion)
            {
                case var s when s.StartsWith("1.3") :
                    var crs = query["crs"].ToString();
                    break;
                default:
                    var srs = query["srs"].ToString().ToLower();
                    if (srs.StartsWith("epsg:"))
                    {
                        inSrid = int.Parse(srs.Substring(5));
                    }
                    break;
            }
            
            var qb = new QueryBuilder(items);
            qb.Add("bbox", bbox);
            
            return Task.FromResult(false);
        }            
    }
}