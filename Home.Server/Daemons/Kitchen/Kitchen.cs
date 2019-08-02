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

namespace Home.Server.Daemons
{
    public class Kitchen : Daemon
    {
        private readonly IKitchenRepo _kitchenRepo;
        private IHubContext<KitchenHub> _kitchenHub;
        public List<Vent> _VentList { get; set; }
        public List<Tank> _TankList { get; set; }

        public Kitchen(List<Microcontroller> micro, IKitchenRepo repo, IHubContext<KitchenHub> hub, ILogger<Daemon> logger) : base(micro, logger)
        {
            _TankList = new List<Tank>();
            _VentList = new List<Vent>();
            _kitchenRepo = repo;

            SetTank(repo.UpperTank);

            SetTank(repo.LowerTank);

            SetVent(repo.ChimneyVent);
            _kitchenHub = hub;
        }

        public void SetTank(Tank tank)
        {
            if (!_TankList.Contains(tank))
            {
                _TankList.Add(tank);
                try
                {
                    tank._GpioLevelTrigger.EventTankStatusChanged += TankStatusChanged;
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                }
            }
            else
            {
                throw new DevicesProtocolException("Tank already contains instance");
            }
        }

        private bool[] tankFillStatus = new bool[2] { false, false };

        private async void TankStatusChanged(object sender, TankStatusChangedEventArgs e)
        {
            var tank = _TankList.Where(t => t.Id == e.TankID).FirstOrDefault();
            tank.Depth = e.Depth;

            _logger.LogInformation($"Tank {e.TankID} is {tank.Depth} full");

            if (e.Level == 3)
            {
                if (tank.State)
                {
                    _logger.LogInformation($"Turning Tank {e.TankID} is Off");
                    tank.State = false;
                    SetTankStatus(tank.Id, false);
                    if (tank.Id == 1)
                    {
                        _kitchenRepo.UpperTank.State = false;
                        tankFillStatus[0] = true;
                        await _kitchenHub.Clients.All.SendAsync("UpperTankPumpStatus", tank.State);
                    }
                    else
                    {
                        _kitchenRepo.LowerTank.State = false;
                        tankFillStatus[1] = true;
                        await _kitchenHub.Clients.All.SendAsync("LowerTankPumpStatus", tank.State);
                    }
                    await _kitchenHub.Clients.All.SendAsync("OnNotification", $"{tank.Name} is full, please check if the motor is turned off");
                }
            }

            await _kitchenHub.Clients.All.SendAsync("Levels", _TankList[0].Depth, _TankList[1].Depth);
        }

        public void SetVent(Vent vent)
        {
            if (!_VentList.Contains(vent))
            {
                _VentList.Add(vent);
            }
            else
            {
                throw new DevicesProtocolException("Vent already contains instance");
            }
        }

        public async Task SetVentStatus(int id, bool state, int speed)
        {
            var vent = _VentList.FirstOrDefault(a => a.Id == id);
            if (vent != null)
            {
                _VentList.Where(v => v.Id == id).FirstOrDefault().State = state;
                _VentList.Where(v => v.Id == id).FirstOrDefault().Speed = speed;
                int st = (state) ? 1 : 0;
                string sId = "vt" + id.ToString().PadLeft(2, '0');
                string sData = speed.ToString();
                string msg = $"*^{sId}^{st}^{sData}^000|"; // Ack.id.state.length
                string chk = $"*^{sId}^{st}^{sData}^{msg.Length - 1}|";
                SendMessage("Kitchen", 0, chk);
            }
        }

        public async Task SetTankStatus(int id, bool state)
        {
            var tank = _TankList.FirstOrDefault(a => a.Id == id);
            if (tank != null)
            {
                _TankList.Where(t => t.Id == id).FirstOrDefault().State = state;
                int st = (state) ? 1 : 0;
                string sId = "tk" + id.ToString().PadLeft(2, '0');
                string msg = $"*^{sId}^{st}^*^000|"; // Ack.id.state.length
                string chk = $"*^{sId}^{st}^*^{msg.Length - 1}|";
                _logger.LogInformation(chk);
                SendMessage("Kitchen", 0, chk);
            }
        }

        public static float f = 0.0f;

        protected override void timerCallback(object state)
        {
            if (_TankList.Capacity > 1)
                foreach (var tank in _TankList)
                {
                    try
                    {
                        tank._GpioLevelTrigger.Ping();
                    }
                    catch (Exception ex)
                    {
                        tank._GpioLevelTrigger.InitPins();
                        _logger.LogInformation(ex.Message);
                    }
                }
        }
        protected override void ProcessMessage(string message)
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
            int iD = int.Parse(sDId);
            Tank tank1 = _TankList.Where(t => t.Id == 1).FirstOrDefault();
            Tank tank2 = _TankList.Where(t => t.Id == 2).FirstOrDefault();
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
                                _VentList.Where(v => v.Id == iD).FirstOrDefault().CalibrationState = calStatus;
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
            _kitchenHub.Clients.All.SendAsync("Levels", tank1.Depth, tank2.Depth);
        }
    }
}
