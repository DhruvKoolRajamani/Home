#define CORS_ENABLED
#define LOGGING_ENABLED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Home.Server.Hubs;
using Home.Server.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Home.Server.Daemons;
using Devices;
using Gpio;

// LOGGING_HEADERS
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace Home.Server
{
    public static class ConnectionExtension
    {
        public static string LocalIP = "http://192.168.1.18:5000/";
    }
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public Kitchen KitchenDaemon { get; set; }
        public static Microcontroller kitchenArduino = new Microcontroller() { IPAddress = "192.168.1.25", Id = 0, Room = "Kitchen", UdpPort = 4210 };
        // public static Microcontroller kitchenNodeMcu = new Microcontroller() { IPAddress = "192.168.1.26", Id = 1, Room = "Kitchen", UdpPort = 4211 };
        public static List<Microcontroller> kitchenMcus = new List<Microcontroller>() { kitchenArduino/*, kitchenNodeMcu*/ };

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

            // services.AddSingleton<IHostedService, Kitchen>();
            services.AddSingleton<IKitchenRepo, KitchenRepo>();
            services.AddSingleton<List<Microcontroller>>(kitchenMcus);
            services.AddHostedService<Kitchen>();
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

            // var kitchen = services.BuildServiceProvider().GetService<Kitchen>();
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
                endpoints.MapHub<KitchenHub>("/kitchen");
            });
        }
    }
}
