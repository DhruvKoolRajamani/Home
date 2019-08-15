using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using Devices;
using System.Collections.Generic;
using Home.Server.Repositories;
using Microsoft.AspNetCore.SignalR;
using Home.Server.Hubs;
using System.Globalization;
using System;

namespace Home.Server.Daemons
{
    public class Lights : Daemon
    {
        public new string currentName => nameof(Lights);
        private readonly ILightsRepo _lightsRepo;
        private IHubContext<LightsHub> _lightsHub;
        public List<Switch> LivingRoom { get; set; }
        public override string CurrentName { get => currentName; }

        public Lights(List<Microcontroller> micro, ILightsRepo repo, IHubContext<LightsHub> hub, ILogger<Daemon> logger) : base(micro.FindAll(m => m.Room == "Living Room"), logger)
        {
            _lightsRepo = repo;
            _lightsHub = hub;
        }

        public async void SetLightState(Switch sw, bool state)
        {
            _lightsRepo.IdentifySwitchById(sw.Id).State = state;
            int st = (state) ? 1 : 0;
            string sId = "rl" + Convert.ToString(sw.Id, 16).PadLeft(2, '0');
            string msg = $"*^{sId}^{st}^*^000|"; // Ack.id.state.length
            string chk = $"*^{sId}^{st}^*^{msg.Length - 1}|";
            if (sw.Id == 0)
                SendGroupMessage(chk, new Tuple<string, int>("Living Room", 0), new Tuple<string, int>("Living Room", 1));
            else if (sw.Id == 1)
                SendGroupMessage(chk, new Tuple<string, int>("Living Room", 0), new Tuple<string, int>("Living Room", 2));
            await _lightsHub.Clients.All.SendAsync("LightStates", _lightsRepo.LivingRoom, _lightsRepo.Kitchen);
        }

        public async void SetLightState(Switch sw)
        {
            _lightsRepo.IdentifySwitchById(sw.Id).State = !sw.State;
            int st = (sw.State) ? 1 : 0;
            string sId = "rl" + Convert.ToString(sw.Id, 16).PadLeft(2, '0');
            string msg = $"*^{sId}^{st}^*^000|"; // Ack.id.state.length
            string chk = $"*^{sId}^{st}^*^{msg.Length - 1}|";
            if (sw.Id == 0)
                SendGroupMessage(chk, new Tuple<string, int>("Living Room", 0), new Tuple<string, int>("Living Room", 1));
            else if (sw.Id == 1)
                SendGroupMessage(chk, new Tuple<string, int>("Living Room", 0), new Tuple<string, int>("Living Room", 2));
            await _lightsHub.Clients.All.SendAsync("LightStates", _lightsRepo.LivingRoom, _lightsRepo.Kitchen);
        }

        protected override bool ProcessMessage(string message, string AckState = "*")
        {
            if (AckState != "A")
            {
                var msg = message.Split('^');
                // foreach (var m in msg)
                string ackState = msg[0];
                try
                {
                    string sID = msg[1];
                    bool state;
                    string sDType = msg[1].Substring(0, 2);
                    string sDId = msg[1].Substring(2, 2);
                    int iD = int.Parse(sDId, NumberStyles.HexNumber);
                    bool parseStatus = false;
                    parseStatus = bool.TryParse(msg[2], out state);
                    string data = msg[3];

                    var col = data.IndexOf(":");
                    var cmd = data.Substring(0, col);

                    switch (sDType)
                    {
                        case "sw":
                            switch (cmd)
                            {
                                case "st":
                                    var tmpStr = data.Substring(col + 1, data.Length - (col + 1));
                                    if (tmpStr == "toggle")
                                        SetLightState(_lightsRepo.IdentifySwitchById(iD));
                                    break;
                                default:
                                    break;
                            }
                            // var st = int.Parse(msg[2]);
                            // state = (st == 1) ? true : false;
                            // _logger.LogInformation($"Switching: {state}");
                            break;
                        default:
                            break;
                    }
                    return true;
                }
                catch
                {

                }
            }
            return true;
        }
    }
}
