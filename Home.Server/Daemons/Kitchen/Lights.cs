using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Net;
using Devices;
using System.Collections.Generic;
using System.Linq;
using Home.Server.Repositories;
using Microsoft.AspNetCore.SignalR;
using Home.Server.Hubs;
using System.Globalization;

namespace Home.Server.Daemons
{
    public class Lights : Daemon
    {
        public new string currentName => nameof(Lights);
        private readonly ILightsRepo _LightRepo;
        private IHubContext<LightsHub> _LightsHub;
        public List<Switch> LivingRoom { get; set; }
        public override string CurrentName { get => currentName; }

        public Lights(List<Microcontroller> micro, ILightsRepo repo, IHubContext<LightsHub> hub, ILogger<Daemon> logger) : base(micro.FindAll(m => m.Room == "Living Room"), logger)
        {
            _LightRepo = repo;
            _LightsHub = hub;
        }

        public void SetLightState(Switch sw, bool state)
        {
            int st = (state) ? 1 : 0;
            string sId = "sw" + sw.Id.ToString().PadLeft(2, '0');
        }

        protected override bool ProcessMessage(string message, string AckState = "*")
        {
            if (AckState != "A")
            {
                var msg = message.Split('^');
                string ackState = msg[0];
                string sID = msg[1];
                bool state;
                string sDType = msg[1].Substring(0, 2);
                string sDId = msg[1].Substring(2, 2);
                int iD = int.Parse(sDId, NumberStyles.HexNumber);
                bool parseStatus = false;

                switch (sDType)
                {
                    case "sw":
                        var data = 0;
                        parseStatus = bool.TryParse(msg[2], out state);
                        if (parseStatus)
                        {
                            parseStatus = int.TryParse(msg[3], out data);
                        }
                        break;
                    default:
                        break;
                }
                return true;
            }
            return true;
        }
    }
}
