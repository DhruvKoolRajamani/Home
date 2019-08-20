using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

// SIGNALR_HEADERS
using Home.Server.Repositories;
using Microsoft.AspNetCore.SignalR;

// LOGGING_HEADERS
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

using Devices;
using static Devices.DevicesExtensions;
using Home.Server.Daemons;
using Home.Server;
using Microsoft.Extensions.Hosting;

namespace Home.Server.Hubs
{
    public interface IKitchenHub
    {
        Task GetPumpStates();
        Task GetVentStates();
        Task SendAsync(params object[] o );
        Task ProcessMessage(string[] message);

    }
    
    public class KitchenHub : Hub<IKitchenHub>
    {
        //Initialize Repositories for Dependency Injection
        private readonly IKitchenRepo _kitchenRepo;
        private Tank _UpperTank;
        private Tank _LowerTank;
        private Vent _Vent;
        private DHT11 _DHT11;
        protected ILogger _logger;
        private List<Microcontroller> mcus = null;
        IHostedService ihs;
        //Hub Constructor to store data obtained from constructor call in the repositories
        public KitchenHub(IKitchenRepo kitchenRepo, ILogger<KitchenHub> logger, IHostedService _ihs) 
        {
            _logger = logger;
            _kitchenRepo = kitchenRepo;
            //REMOVE: _kitchen = (Kitchen)daemons.FirstOrDefault(d => d.CurrentName == nameof(Kitchen));

            _UpperTank = kitchenRepo.UpperTank;
            _LowerTank = kitchenRepo.LowerTank;
            _Vent = kitchenRepo.ChimneyVent;
            _DHT11 = kitchenRepo.Dht11;
            ihs = _ihs;
            FillMicroList();
        }
        public void FillMicroList()
        {
            mcus = new List<Microcontroller>()
            {
              //new Microcontroller() {IPAddress = "192.168.1.129", Id = 0, Room = "kitchen", UdpPort = 11466 }
              new Kitchen(){IPAddress = "192.168.1.129", Id = 0, Room = "kitchen", UdpPort = 11466 }
            //, new Microcontroller() { IPAddress = "192.168.1.130", Id = 1, Room = "kitchen", UdpPort = 11466}
            , new Microcontroller() { IPAddress = "192.168.1.131", Id = 2, Room = "kitchen", UdpPort = 11466 }
            , new Microcontroller() { IPAddress = "192.168.1.140", Id = 0, Room = "living room", UdpPort = 11466}
            , new Microcontroller() { IPAddress = "192.168.1.141", Id = 1, Room = "living room", UdpPort = 11466 }
            , new Microcontroller() { IPAddress = "192.168.1.142", Id = 2, Room = "living room", UdpPort = 11466 }
            };

        }
        public async Task GetPumpStates()
        {
            await Clients.Caller.SendAsync("UpperTankPumpStatus", _kitchenRepo.UpperTank.State);
            await Clients.Caller.SendAsync("LowerTankPumpStatus", _kitchenRepo.LowerTank.State);
        }

        public async Task Notification(string message)
        {
            await Clients.Others.SendAsync("TankStatusChanged", message);
        }

        public async Task SetUpperTankPumpState(bool upperTankPumpState)
        {
            _kitchenRepo.UpperTank.State = upperTankPumpState;
            _kitchenRepo.UpperTank.RaiseTankStatusChangedEvent(upperTankPumpState);
            
            

            await Clients.All.SendAsync("UpperTankPumpStatus", _UpperTank.State);
        }

        public async Task SetLowerTankPumpState(bool lowerTankPumpState)
        {
            _kitchenRepo.LowerTank.State = lowerTankPumpState;
            _kitchenRepo.LowerTank.RaiseTankStatusChangedEvent(lowerTankPumpState);
            await Clients.All.SendAsync("LowerTankPumpStatus", _LowerTank.State);
        }

        public async Task SetUpperTankLevel(float uppertank)
        {
            

            
            try
            {
                _UpperTank.Depth = uppertank;
                await Clients.All.SendAsync("Levels", _UpperTank.Depth, _LowerTank.Depth);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception in {this} at {LineNumber(ex)} with {ex.Message} of type {ex}");
            }
        }
        public async Task SetLowerTankLevel(float lowertank)
        {
            try
            {
                _LowerTank.Depth = lowertank;

            //    _logger.LogInformation($"Lower Tank Pump Depth: {_LowerTank.Depth}");

                await Clients.All.SendAsync("Levels", _UpperTank.Depth, _LowerTank.Depth);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception in {this} at {LineNumber(ex)} with {ex.Message} of type {ex}");
            }
        }
        public async Task GetTankLevels()
        {
            await Clients.All.SendAsync("Levels", _UpperTank.Depth, _LowerTank.Depth);
        }
        public async Task SendWeatherData()
        {
            await Clients.All.SendAsync("WeatherData", _kitchenRepo.Dht11.Temp, _kitchenRepo.Dht11.Humidity);
        }

        public async Task ProcessMessage(string[] msgPack)
        {
            foreach (string s in msgPack)
                Console.WriteLine(s);
            await Task.Delay(10);

        }
    }

    public interface IHome
    {
        Task SendMessage(string sMsg);
        Task SendAsync(params object[] o);
        
    }
    public class HomeHub : Hub<IHome>
    {
        public async Task SendMessage(string sMsg)
        {
            await Clients.All.SendAsync("ReceiveMsg", sMsg);
        }
    }
}