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

namespace Home.Server.Hubs
{
    public class KitchenHub : Hub
    {
        //Initialize Repositories for Dependency Injection
        private readonly IKitchenRepo _kitchenRepo;
        //private readonly IVentMotorRepo _ventMotorRepo;

        //Hub Constructor to store data obtained from constructor call in the repositories
        public KitchenHub(IKitchenRepo kitchenRepo) //, IVentMotorRepo ventMotorRepo)
        {
            _kitchenRepo = kitchenRepo;
            //_ventMotorRepo = ventMotorRepo;
        }

        public async Task GetPumpStates()
        {
            await Clients.Caller.SendAsync("UpperTankPumpStatus", _kitchenRepo.UpperTankPumpState);
            await Clients.Caller.SendAsync("LowerTankPumpStatus", _kitchenRepo.LowerTankPumpState);
        }

        public async Task Notification(string message)
        {
            await Clients.Others.SendAsync("onNotification", message);
        }

        public async Task SetUpperTankPumpState(bool upperTankPumpState)
        {
            _kitchenRepo.UpperTankPumpState = upperTankPumpState;

            Debug.WriteLine($"Upper Tank Pump State: {_kitchenRepo.UpperTankPumpState}");

            await Clients.All.SendAsync("UpperTankPumpStatus", _kitchenRepo.UpperTankPumpState);
        }

        public async Task SetLowerTankPumpState(bool lowerTankPumpState)
        {
            _kitchenRepo.LowerTankPumpState = lowerTankPumpState;

            Debug.WriteLine($"Lower Tank Pump State: {_kitchenRepo.LowerTankPumpState}");

            await Clients.All.SendAsync("LowerTankPumpStatus", _kitchenRepo.LowerTankPumpState);
        }

        public async Task SetUpperTankLevel(float uppertank)
        {
            _kitchenRepo.UpperTankDepth = uppertank;

            Debug.WriteLine($"Upper Tank Pump Depth: {_kitchenRepo.UpperTankDepth}");

            await Clients.All.SendAsync("Levels", _kitchenRepo.UpperTankDepth, _kitchenRepo.LowerTankDepth);
        }

        public async Task SetLowerTankLevel(float lowertank)
        {
            _kitchenRepo.LowerTankDepth = lowertank;

            Debug.WriteLine($"Lower Tank Pump Depth: {_kitchenRepo.LowerTankDepth}");

            await Clients.All.SendAsync("Levels", _kitchenRepo.UpperTankDepth, _kitchenRepo.LowerTankDepth);
        }

        public async Task GetTankLevels()
        {
            await Clients.All.SendAsync("Levels", _kitchenRepo.UpperTankDepth, _kitchenRepo.LowerTankDepth);
        }
    }
}