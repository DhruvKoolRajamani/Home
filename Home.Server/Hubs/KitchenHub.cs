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

        //Hub Constructor to store data obtained from constructor call in the repositories
        public KitchenHub(IKitchenRepo kitchenRepo, IHostedService kitchen) //, IVentMotorRepo ventMotorRepo)
        {
            _kitchenRepo = kitchenRepo;
            _kitchen = (Kitchen)kitchen;
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
            await Clients.Others.SendAsync("onNotification", message);
        }

        public async Task SetVentState(bool state, int speed, bool calState)
        {
            _kitchen.SetVentStatus(_kitchenRepo.ChimneyVent.Id, state, speed);
            await Clients.Caller.SendAsync("VentStatus", _kitchenRepo.ChimneyVent.State, _kitchenRepo.ChimneyVent.Speed, _kitchenRepo.ChimneyVent.CalibrationState);
        }

        public async Task SetUpperTankPumpState(bool upperTankPumpState)
        {
            _UpperTank.State = upperTankPumpState;
            _kitchen.SetTankStatus(_kitchenRepo.UpperTank.Id, upperTankPumpState);

            Debug.WriteLine($"Upper Tank Pump State: {_UpperTank.State}");

            await Clients.All.SendAsync("UpperTankPumpStatus", _UpperTank.State);
        }

        public async Task SetLowerTankPumpState(bool lowerTankPumpState)
        {
            _LowerTank.State = lowerTankPumpState;
            _kitchen.SetTankStatus(_kitchenRepo.LowerTank.Id, lowerTankPumpState);

            Debug.WriteLine($"Lower Tank Pump State: {_LowerTank.State}");

            await Clients.All.SendAsync("LowerTankPumpStatus", _LowerTank.State);
        }

        public async Task SetUpperTankLevel(float uppertank)
        {
            try
            {
                _UpperTank.Depth = uppertank;

                Debug.WriteLine($"Upper Tank Pump Depth: {_UpperTank.Depth}");

                await Clients.All.SendAsync("Levels", _UpperTank.Depth, _LowerTank.Depth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in {this} at {LineNumber(ex)} with {ex.Message} of type {ex}");
            }
        }

        public async Task SetLowerTankLevel(float lowertank)
        {
            try
            {
                _LowerTank.Depth = lowertank;

                Debug.WriteLine($"Lower Tank Pump Depth: {_LowerTank.Depth}");

                await Clients.All.SendAsync("Levels", _UpperTank.Depth, _LowerTank.Depth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in {this} at {LineNumber(ex)} with {ex.Message} of type {ex}");
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