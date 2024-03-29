#define CORS_ENABLED
#define LOGGING_ENABLED
#define LEVEL_ENABLE

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
        public static string LocalIP = "http://192.168.1.13:5000/";
    }
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public static Microcontroller kitchenNodeAmica = new Microcontroller() { IPAddress = "192.168.1.129", Id = 0, Room = "Kitchen", UdpPort = 4210 };
        public static Microcontroller kitchenWemos = new Microcontroller() { IPAddress = "192.168.1.130", Id = 1, Room = "Kitchen", UdpPort = 4211 };
        public static Microcontroller kitchenSwitches = new Microcontroller() { IPAddress = "192.168.1.131", Id = 2, Room = "Kitchen", UdpPort = 4212 };
        public static Microcontroller livingRoomLightSwitch = new Microcontroller() { IPAddress = "192.168.1.140", Id = 0, Room = "Living Room", UdpPort = 6881 };
        public static Microcontroller livingRoomLeftLightController = new Microcontroller() { IPAddress = "192.168.1.141", Id = 1, Room = "Living Room", UdpPort = 6882 };
        public static Microcontroller livingRoomRightLightController = new Microcontroller() { IPAddress = "192.168.1.142", Id = 2, Room = "Living Room", UdpPort = 6883 };
        // public static Microcontroller kitchenNodeMcu = new Microcontroller() { IPAddress = "192.168.1.26", Id = 1, Room = "Kitchen", UdpPort = 4211 };
        public static List<Microcontroller> mcus = new List<Microcontroller>() { kitchenNodeAmica, kitchenWemos, kitchenSwitches, livingRoomLightSwitch, livingRoomLeftLightController, livingRoomRightLightController };

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

            services.AddSingleton<ILightsRepo, LightsRepo>();
            services.AddSingleton<IKitchenRepo, KitchenRepo>();
            services.AddSingleton<List<Microcontroller>>(mcus);
            services.AddSingleton<Daemon, Kitchen>();
            services.AddSingleton<Daemon, Lights>();
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
                endpoints.MapHub<LightsHub>("/lightHub");
            });
        }
    }
}
