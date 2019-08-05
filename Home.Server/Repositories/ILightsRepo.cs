using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Home.Server.Daemons;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Gpio;
using Devices;
using System.Linq;

namespace Home.Server.Repositories
{
    public interface ILightsRepo
    {
        List<Switch> LivingRoom { get; set; }
        List<Switch> Kitchen { get; set; }

        Switch IdentifySwitchById(int id);
    }

    public class LightsRepo : ILightsRepo
    {
        private static ILogger _logger;
        private List<Switch> livingRoom = new List<Switch>() { new Switch() { Name = "Living Room", Id = 0x00, State = false }, new Switch() { Name = "Living Room", Id = 0x01, State = false } };
        private List<Switch> kitchen = new List<Switch>() { new Switch() { Name = "Kitchen", Id = 0x02, State = false }, new Switch() { Name = "Kitchen", Id = 0x03, State = false } };

        public LightsRepo(ILogger<LightsRepo> logger)
        {
            _logger = logger;
        }

        public List<Switch> LivingRoom { get => livingRoom; set => livingRoom = value; }
        public List<Switch> Kitchen { get => kitchen; set => kitchen = value; }

        public Switch IdentifySwitchById(int id)
        {
            switch (id)
            {
                case 0x00:
                    return LivingRoom[0];
                case 0x01:
                    return LivingRoom[1];
                case 0x02:
                    return Kitchen[0];
                case 0x03:
                    return Kitchen[1];
                default:
                    return null;
            }
        }
    }
}