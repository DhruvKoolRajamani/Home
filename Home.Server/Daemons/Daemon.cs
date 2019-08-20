using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Net;
using Devices;
using static Devices.DevicesExtensions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Home.Server.Hubs;
using Home.Server.Repositories;

namespace Home.Server.Daemons
{
    
    public  class Daemon : BackgroundService
    {
        public string currentName = "";
        protected ILogger _logger;
        public ILogger GetLogger{get =>_logger;}

        public Daemon(ILogger<Daemon> logger)
        {
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(200);
        }

       
    }
}