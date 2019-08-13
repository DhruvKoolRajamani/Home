using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Home.Server.Daemons;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Gpio;
using Devices;

namespace Home.Server.Repositories
{
    public interface IKitchenRepo
    {
        Tank UpperTank { get; set; }
        Tank LowerTank { get; set; }
        Vent ChimneyVent { get; set; }
        DHT11 Dht11 { get; set; }
    }

    public class KitchenRepo : IKitchenRepo
    {
        private static ILogger _logger;
        public KitchenRepo(ILogger<KitchenRepo> logger)
        {
            _logger = logger;
        }
        //public Pin staticPin = new Pin(_logger);

        private Tank upperTank = new Tank() { Id = 0x01, Name = "Upper Tank", State = false, Depth = 0.0f };
        private Tank lowerTank = new Tank() { Id = 0x02, Name = "Lower Tank", State = false, Depth = 0.0f };
        private Vent chimneyVent = new Vent() { Id = 0x00, Name = "Chimney Vent", State = false, Speed = 0, CalibrationState = false };
        private DHT11 dht11 = new DHT11() { Id = 0x00, Name = "DHT Sensor", State = false, Temp = 0.0f, Humidity = 0.0f };

        public Tank UpperTank { get => upperTank; set => upperTank = value; }
        public Tank LowerTank { get => lowerTank; set => lowerTank = value; }
        public Vent ChimneyVent { get => chimneyVent; set => chimneyVent = value; }
        public DHT11 Dht11 { get => dht11; set => dht11 = value; }
    }
}