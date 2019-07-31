using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Home.Server.Daemons;
using Microsoft.AspNetCore.SignalR.Client;

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
        public KitchenRepo()
        {
            UpperTank.OnDepthChanged += DepthChanged;
            LowerTank.OnDepthChanged += DepthChanged;
        }

        private void DepthChanged(object sender, DepthChangedEventArgs e)
        {
            
        }

        private Tank upperTank = new Tank() { Id = 1, Name = "Upper Tank", State = false, Depth = 0.0f };
        private Tank lowerTank = new Tank() { Id = 2, Name = "Lower Tank", State = false, Depth = 0.0f };
        private Vent chimneyVent = new Vent() { Id = 0, Name = "Chimney Vent", State = false, Speed = 0, CalibrationState = false };
        private DHT11 dht11 = new DHT11() { Id = 0, Name = "DHT Sensor", State = false, Temp = 0.0f, Humidity = 0.0f };

        public Tank UpperTank { get => upperTank; set => upperTank = value; }
        public Tank LowerTank { get => lowerTank; set => lowerTank = value; }
        public Vent ChimneyVent { get => chimneyVent; set => chimneyVent = value; }
        public DHT11 Dht11 { get => dht11; set => dht11 = value; }
    }
}