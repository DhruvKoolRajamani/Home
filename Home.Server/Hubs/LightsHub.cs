using System;
using System.Collections.Generic;
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
    public class LightsHub : Hub
    {
        //Initialize Repositories for Dependency Injection
        private readonly ILightsRepo _lightsRepo;
        //private readonly IVentMotorRepo _ventMotorRepo;
        private Lights _lights;
        protected ILogger _logger;

        //Hub Constructor to store data obtained from constructor call in the repositories
        public LightsHub(ILightsRepo lightsRepo, IEnumerable<Daemon> daemons, ILogger<LightsHub> logger) //, IVentMotorRepo ventMotorRepo)
        {
            _logger = logger;
            _lightsRepo = lightsRepo;

            try
            {
                _lights = (Lights)daemons.FirstOrDefault(d => d.CurrentName == nameof(Lights));
                // _lights = (Lights)daemons;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Conversion error?");
            }
        }

        public async Task GetLightsStates()
        {
            _lights.SetLightState(_lightsRepo.LivingRoom[0], _lightsRepo.LivingRoom[0].State); 
            await Clients.All.SendAsync("LightStates", _lightsRepo.LivingRoom, _lightsRepo.Kitchen);
        }

        public async Task SetLightsState(string swName, int swId, bool swState)
        {
            List<Switch> AllSwitches = new List<Switch>();
            AllSwitches.AddRange(_lightsRepo.LivingRoom);
            AllSwitches.AddRange(_lightsRepo.Kitchen);

            var sender = _lightsRepo.IdentifySwitchById(swId);

            try
            {
                // _logger.LogInformation($"object: {sender.ToString()} of type {sender.GetType()}\n");
                sender.State = swState;
                _logger.LogInformation($"Received Switch with state {sender.State}\n");
                _lights.SetLightState(sender, sender.State);
                // await Clients.All.SendAsync("LightStates", _lightsRepo.LivingRoom, _lightsRepo.Kitchen);
                // await Clients.All.SendAsync("LightStates", new List<object>() {sender, sender}, new List<object>() {sender});
            }
            catch (Exception ex)
            {
                var ln = DevicesExtensions.LineNumber(ex);
                _logger.LogWarning($"Line {ln}\n{ex.Message}");
            }
        }
    }
}