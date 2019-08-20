#define CORS_ENABLED
#define LOGGING_ENABLED
#define LEVEL_ENABLE

using System.Collections.Generic;
using Home.Server.Hubs;
using Home.Server.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Home.Server.Daemons;
using Devices;

// LOGGING_HEADERS
using Microsoft.Extensions.Logging;

namespace Home.Server
{
    public static class ConnectionExtension
    {
        public static string LocalIP =   "http://192.168.1.7:5000/";
    }
    public class Startup
    {
            
        
        public Startup(IConfiguration configuration)
        {
            
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

            services.AddSingleton<IKitchenRepo, KitchenRepo>();
            services.AddSingleton<iCommunication, Wire>();
            
            //Do we need this?
            services.AddHostedService<Daemon>();
            services.AddSignalR().AddJsonProtocol(options => options.PayloadSerializerOptions.WriteIndented = true);

#if CORS_ENABLED
            services.AddCors(options => options.AddPolicy("CorsPolicy",
            builder =>
            {
                builder.AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithOrigins(ConnectionExtension.LocalIP)
                       .AllowCredentials();
            }));
#endif

#if LOGGING_ENABLED
            services.AddLogging(builder =>
            {
                builder.AddConsole().AddDebug();
                DebugLoggerFactoryExtensions.AddDebug(builder).AddConsole();
            });
#endif

            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<KitchenHub>("/kitchenHub");
                endpoints.MapHub<HomeHub>("/HomeHub");

            });
        }
    }
}
