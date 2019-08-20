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
    
    public class Kitchen : Microcontroller
    //public class Kitchen : Daemon
    {
        public new string currentName => nameof(Kitchen);
        public string CurrentName { get => currentName; }
        private readonly IKitchenRepo _kitchenRepo;
        private IHubContext<KitchenHub> _kitchenHub;
        public Tank UpperTank { get; set; }
        public Tank LowerTank { get; set; }
        public Vent ChimneyVent { get; set; }
        private Timer _TimerKitchen { get; set; }
        private float PrevValue = -1.0f;
        private int rejectedCounter = 0;
        
        //public Kitchen(List<Microcontroller> micro, IKitchenRepo repo, IHubContext<KitchenHub> hub, ILogger<Daemon> logger) : base(micro.FindAll(m =>  m.Room.ToLower() == "kitchen"), logger)
        public Kitchen ()
        {/*
            IHubContext<KitchenHub> hub, IKitchenRepo repo
            _kitchenRepo = repo;

            UpperTank = repo.UpperTank;
            LowerTank = repo.LowerTank;

            ChimneyVent = repo.ChimneyVent;
            _kitchenHub = hub;
            */
        }

        private static DateTime curTime = DateTime.MinValue;
        private static int tmDelay = 0;

        private async void TankStatusChanged(int tankId, float level)
        {
            return;
         
        }

        private async void TankOffCallback(object state)
        {
            var tank = state as Tank;
            
            tank.State = false;
            await SetTankStatus(tank.Id, false);
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
            /*
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
            }*/
        }

        public async Task <bool>SetTankStatus(int id, bool state)
        {
            var tank = (id == 1) ? UpperTank : LowerTank;
            if (tank != null)
            {
                tank.State = state;
                int st = (state) ? 1 : 0;

                //Message format : <Orig>^<TargetType>^<ID>^<Command>^<Data>^<Totalbytes not including the totalbytes>
                // srv^tk^01^switch^0^19

                string msg = $"srv^tk^{id:00}^switch^{st}";
                msg = msg + $"^{msg.Length}";

                //send the message to the tank relay and the kitchen button (?)
                // we should make groups here or atleast each room should be considered a group 

                SendGroupMessage(msg,"kitchen");

                await _kitchenHub.Clients.All.SendAsync("UpperTankPumpStatus", UpperTank.State);
                await _kitchenHub.Clients.All.SendAsync("LowerTankPumpStatus", LowerTank.State);
            }
            return true;
        }

        private void SendGroupMessage(string msg, string room)
        {
        }
        protected override bool ProcessMessage(string message, string AckState = "*")
        {
            // _logger.LogInformation($"{message}\n");
            if (AckState != "A")
            {
                var msg = message.Split('^');

                float humidity = 0.0f;
                float temperature = 0.0f;
                
                string ackState = msg[0];
                try
                {
                    string sID = msg[1];
                    bool state;
                    string sDType = msg[1];
                    string sDId = msg[2];
                    int iD = int.Parse(sDId, NumberStyles.HexNumber);
                    bool parseStatus = false;
                    parseStatus = bool.TryParse(msg[3], out state);
                    string data = msg[4];
                    switch (sDType)
                    {
                        case "vt":
                            int speed;
                            if (parseStatus)
                            {
                                parseStatus = int.TryParse(data, out speed);
                                if (!parseStatus)
                                {
                                    var catMsg = msg[4].Split('-');
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
                                    switch (data)
                                    {
                                        case "st":
                                            var tmpStr = msg[5];
                                            //SetTankStatus(1, !UpperTank.State);
                                            SetTankStatus(1, !UpperTank.State);
                                            break;
                                        case "lv":
                                            //parseStatus = bool.Parse(msg[4]);
                                           // TankStatusChanged(1, level);
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                case 2:
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case "dh":
                            /*
                            int semC = data.IndexOf(';');
                            string humString = data.Substring(0, semC);
                            int H = humString.IndexOf(":");
                            parseStatus = float.TryParse(humString.Substring(H + 1, humString.Length - (H + 1)), out humidity);
                            string tempString = data.Substring(semC + 1, data.Length - semC - 1);
                            int T = humString.IndexOf(":");
                            parseStatus = float.TryParse(tempString.Substring(T + 1, tempString.Length - (T + 1)), out temperature);
                            _kitchenRepo.Dht11.Temp = temperature;
                            _kitchenRepo.Dht11.Humidity = humidity;
                            _kitchenHub.Clients.All.SendAsync("WeatherData", _kitchenRepo.Dht11.Temp, _kitchenRepo.Dht11.Humidity);
                            */
                            break;
                        default:
                            break;
                    }
                    _kitchenHub.Clients.All.SendAsync("WeatherData", _kitchenRepo.Dht11.Temp, _kitchenRepo.Dht11.Humidity);
                    // _kitchenHub.Clients.All.SendAsync("Levels", UpperTank.Depth, LowerTank.Depth);
                }
                catch (IndexOutOfRangeException ex)
                {
                    _logger.LogInformation($"{ex.Message}, {ex.Source}\n");
                    
                }
            }

            return true;
        }
    }
}
