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
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Home.Server.Hubs;
using System.Globalization;

namespace Home.Server.Daemons
{
    public class Kitchen : Daemon
    {
        public new string currentName => nameof(Kitchen);
        public override string CurrentName { get => currentName; }
        private readonly IKitchenRepo _kitchenRepo;
        private IHubContext<KitchenHub> _kitchenHub;
        public Tank UpperTank { get; set; }
        public Tank LowerTank { get; set; }
        public Vent ChimneyVent { get; set; }
        private Timer _TimerKitchen { get; set; }

        public Kitchen(List<Microcontroller> micro, IKitchenRepo repo, IHubContext<KitchenHub> hub, ILogger<Daemon> logger) : base(micro.FindAll(m => m.Room == "Kitchen"), logger)
        {
            _kitchenRepo = repo;

            UpperTank = repo.UpperTank;
            LowerTank = repo.LowerTank;

            UpperTank._GpioLevelTrigger.EventTankStatusChanged += TankStatusChanged;
            LowerTank._GpioLevelTrigger.EventTankStatusChanged += TankStatusChanged;

            ChimneyVent = repo.ChimneyVent;
            _kitchenHub = hub;
        }

        private static DateTime curTime = DateTime.MinValue;
        private static int tmDelay = 0;

        private async void TankStatusChanged(object sender, TankStatusChangedEventArgs e)
        {
            var tank = (e.TankID == 1) ? UpperTank : LowerTank;
            tank.Depth = e.Depth;

            _logger.LogInformation($"\nTank {e.TankID} says {e.DebugMessage} at {DateTime.Now.ToString("hh:mm:ss:fff")}\n");
            _logger.LogInformation($"Tank {e.TankID} is {tank.Depth} full");

            if (e.Level == 3)
            {
                if (curTime == DateTime.MinValue)
                {
                    curTime = DateTime.Now;
                    tmDelay = curTime.Millisecond + 5000;
                }
                if (tank.State)
                {
                    _logger.LogInformation($"Calling Timer in {tmDelay - curTime.Millisecond}");
                    _TimerKitchen = new Timer(TankOffCallback, tank, 5000, Timeout.Infinite);
                    await _kitchenHub.Clients.All.SendAsync("OnNotification", $"{tank.Name} is full, please check if the motor is turned off");
                }
            }

            await _kitchenHub.Clients.All.SendAsync("Levels", UpperTank.Depth, LowerTank.Depth);
        }

        private async void TankOffCallback(object state)
        {
            var tank = state as Tank;
            _logger.LogInformation($"Turning Tank {tank.Id} is Off");
            tank.State = false;
            SetTankStatus(tank.Id, false);
            if (tank.Id == 1)
            {
                await _kitchenHub.Clients.All.SendAsync("UpperTankPumpStatus", tank.State);
            }
            else
            {
                await _kitchenHub.Clients.All.SendAsync("LowerTankPumpStatus", tank.State);
            }
            tmDelay = 0;
            curTime = DateTime.MinValue;
        }

        public async Task SetVentStatus(int id, bool state, int speed)
        {
            var vent = ChimneyVent;
            if (vent != null)
            {
                vent.State = state;
                vent.Speed = speed;
                int st = (state) ? 1 : 0;
                string sId = "vt" + Convert.ToString(id, 16).PadLeft(2, '0');
                string sData = speed.ToString();
                string msg = $"*^{sId}^{st}^{sData}^000|"; // Ack.id.state.length
                string chk = $"*^{sId}^{st}^{sData}^{msg.Length - 1}|";
                SendMessage("Kitchen", 0, chk);
            }
        }

        public async Task SetTankStatus(int id, bool state)
        {
            var tank = (id == 1) ? UpperTank : LowerTank;
            if (tank != null)
            {
                tank.State = state;
                int st = (state) ? 1 : 0;
                string sId = "tk" + Convert.ToString(id, 16).PadLeft(2, '0');
                string msg = $"*^{sId}^{st}^*^000|"; // Ack.id.state.length
                string chk = $"*^{sId}^{st}^*^{msg.Length - 1}|";
                _logger.LogInformation(chk);
                SendMessage("Kitchen", 0, chk);
            }
        }

        public static float f = 0.0f;

        protected override void timerCallback(object state)
        {
            try
            {
                UpperTank._GpioLevelTrigger.Ping();
                LowerTank._GpioLevelTrigger.Ping();
            }
            catch (Exception ex)
            {
                UpperTank._GpioLevelTrigger.InitPins();
                LowerTank._GpioLevelTrigger.InitPins();
                _logger.LogInformation(ex.Message);
            }
        }
        protected override bool ProcessMessage(string message, string AckState = "*")
        {
            if (AckState != "A")
            {
                var msg = message.Split('^');
                float humidity = 0.0f;
                float temperature = 0.0f;
                // foreach (var m in msg)
                //     _logger.LogInformation($"\n{m}\n");
                string ackState = msg[0];
                string sID = msg[1];
                bool state;
                string sDType = msg[1].Substring(0, 2);
                string sDId = msg[1].Substring(2, 2);
                int iD = int.Parse(sDId, NumberStyles.HexNumber);
                bool parseStatus = false;

                switch (sDType)
                {
                    case "vt":
                        int speed;
                        parseStatus = bool.TryParse(msg[2], out state);
                        if (parseStatus)
                        {
                            parseStatus = int.TryParse(msg[3], out speed);
                            if (!parseStatus)
                            {
                                var catMsg = msg[3].Split('-');
                                parseStatus = int.TryParse(catMsg[0], out speed);
                                if (parseStatus)
                                {
                                    bool calStatus;
                                    parseStatus = bool.TryParse(catMsg[1], out calStatus);
                                    ChimneyVent.CalibrationState = calStatus;
                                }
                            }
                        }
                        break;
                    case "tk":
                        switch (iD)
                        {
                            case 1:
                                // f += (f <= 1.0f) ? 0.01f : 0.0f;
                                // tank1.Depth = f;
                                // _logger.LogInformation($"Upper Tank Depth: {tank1.Depth}");
                                break;
                            case 2:
                                // f += (f <= 1.0f) ? 0.01f : 0.0f;
                                // tank2.Depth = f;
                                // _logger.LogInformation($"Lower Tank Depth: {tank2.Depth}");
                                break;
                            default:
                                break;
                        }
                        break;
                    case "dh":
                        // _logger.LogInformation("Entering DHT sensor case");
                        // parseStatus = bool.TryParse(msg[2], out state);
                        // if (parseStatus)
                        {
                            string data = msg[3];
                            int semC = data.IndexOf(';');
                            string humString = data.Substring(0, semC);
                            int H = humString.IndexOf(":");
                            parseStatus = float.TryParse(humString.Substring(H + 1, humString.Length - (H + 1)), out humidity);
                            // if (parseStatus)
                            //     _logger.LogInformation($"Humidity: {humidity}");
                            string tempString = data.Substring(semC + 1, data.Length - semC - 1);
                            int T = humString.IndexOf(":");
                            parseStatus = float.TryParse(tempString.Substring(T + 1, tempString.Length - (T + 1)), out temperature);
                            // if (parseStatus)
                            //     _logger.LogInformation($"Temperature: {temperature}");
                            _kitchenRepo.Dht11.Temp = temperature;
                            _kitchenRepo.Dht11.Humidity = humidity;
                            _kitchenHub.Clients.All.SendAsync("WeatherData", _kitchenRepo.Dht11.Temp, _kitchenRepo.Dht11.Humidity);
                        }
                        break;
                    default:
                        break;
                }
                _kitchenHub.Clients.All.SendAsync("WeatherData", _kitchenRepo.Dht11.Temp, _kitchenRepo.Dht11.Humidity);
                _kitchenHub.Clients.All.SendAsync("Levels", UpperTank.Depth, LowerTank.Depth);
                return true;
            }
            return true;
        }
    }
}
