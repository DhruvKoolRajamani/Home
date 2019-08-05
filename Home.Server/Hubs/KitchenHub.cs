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
    public class KitchenHub : Hub
    {
        //Initialize Repositories for Dependency Injection
        private readonly IKitchenRepo _kitchenRepo;
        //private readonly IVentMotorRepo _ventMotorRepo;
        private Kitchen _kitchen;
        private Tank _UpperTank;
        private Tank _LowerTank;
        private Vent _Vent;
        private DHT11 _DHT11;
        protected ILogger _logger;

        //Hub Constructor to store data obtained from constructor call in the repositories
        public KitchenHub(IKitchenRepo kitchenRepo, IEnumerable<Daemon> daemons, ILogger<KitchenHub> logger) //, IVentMotorRepo ventMotorRepo)
        {
            _logger = logger;
            _kitchenRepo = kitchenRepo;
            _kitchen = (Kitchen)daemons.FirstOrDefault(d => d.CurrentName == nameof(Kitchen));
            _UpperTank = kitchenRepo.UpperTank;
            _LowerTank = kitchenRepo.LowerTank;
            _Vent = kitchenRepo.ChimneyVent;
            _DHT11 = kitchenRepo.Dht11;
        }

        public async Task GetPumpStates()
        {
            await Clients.Caller.SendAsync("UpperTankPumpStatus", _kitchenRepo.UpperTank.State);
            await Clients.Caller.SendAsync("LowerTankPumpStatus", _kitchenRepo.LowerTank.State);
        }

        public async Task GetVentState()
        {
            await Clients.Caller.SendAsync("VentStatus", _kitchenRepo.ChimneyVent.State, _kitchenRepo.ChimneyVent.Speed, _kitchenRepo.ChimneyVent.CalibrationState);
        }

        public async Task Notification(string message)
        {
            await Clients.Others.SendAsync("TankStatusChanged", message);
        }

        public async Task SetVentState(bool state, int speed, bool calState)
        {
            try
            {
                _kitchen.SetVentStatus(_kitchenRepo.ChimneyVent.Id, state, speed);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
            }
            await Clients.Caller.SendAsync("VentStatus", _kitchenRepo.ChimneyVent.State, _kitchenRepo.ChimneyVent.Speed, _kitchenRepo.ChimneyVent.CalibrationState);
        }

        public async Task SetUpperTankPumpState(bool upperTankPumpState)
        {
            _kitchenRepo.UpperTank.State = upperTankPumpState;
            _kitchenRepo.UpperTank.RaiseTankStatusChangedEvent(upperTankPumpState);
            _kitchen.SetTankStatus(_kitchenRepo.UpperTank.Id, upperTankPumpState);

            _logger.LogInformation($"Upper Tank Pump State: {_UpperTank.State}");

            await Clients.All.SendAsync("UpperTankPumpStatus", _UpperTank.State);
        }

        public async Task SetLowerTankPumpState(bool lowerTankPumpState)
        {
            _kitchenRepo.LowerTank.State = lowerTankPumpState;
            _kitchenRepo.LowerTank.RaiseTankStatusChangedEvent(lowerTankPumpState);
            _kitchen.SetTankStatus(_kitchenRepo.LowerTank.Id, lowerTankPumpState);

            _logger.LogInformation($"Lower Tank Pump State: {_LowerTank.State}");

            await Clients.All.SendAsync("LowerTankPumpStatus", _LowerTank.State);
        }

        public async Task SetUpperTankLevel(float uppertank)
        {
            try
            {
                _UpperTank.Depth = uppertank;

                _logger.LogInformation($"Upper Tank Pump Depth: {_UpperTank.Depth}");

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

                _logger.LogInformation($"Lower Tank Pump Depth: {_LowerTank.Depth}");

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
    }
}