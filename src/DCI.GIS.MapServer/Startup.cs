using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Ataoge.Data;
using DCI.GIS.MapServer.Configuration;
using DCI.GIS.MapServer.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DCI.GIS.MapServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        readonly string MapServerAllowSpecificOrigins = "_MapServerAllowSpecificOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            services.AddSingleton<Database>(new Database(connectionString,Npgsql.NpgsqlFactory.Instance));

            //services.Configure<MapServerConfig>(Configuration.GetSection("MapServer"));
            services.AddTransient<MapServerMiddleware>();
            services.AddRouting();

            var config = Configuration.GetSection("MapServer");//.Get<MapServerConfig>();
            services.Configure<MapServerConfig>(config);
            services.AddSingleton<IHandlerManager, HandlerManager>();

            // 跨域访问
            services.AddCors(options =>
            {
                options.AddPolicy(MapServerAllowSpecificOrigins, build => {
                    //build.WithOrigins("http://localhost:5000");
                    build.AllowAnyOrigin();
                });
            });

            // 分布式缓存
            services.AddDistributedMemoryCache();
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            app.UseCors(MapServerAllowSpecificOrigins);

            app.UseRouter(routeBuilder => {
                routeBuilder.MapMiddlewareGet("/Wmts/{serviceName}/tile/{z:int}/{x:int}/{y:int}.{format?}", appBuilder => appBuilder.UseMiddleware<MapServerMiddleware>());
                routeBuilder.MapMiddlewareGet("/Wmts/{serviceName}/{**path}", appBuilder => appBuilder.UseMiddleware<MapServerMiddleware>());
            });
            
            //app.Map("/Wmts", appBuilder => appBuilder.UseMiddleware<MapServerMiddleware>());

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
