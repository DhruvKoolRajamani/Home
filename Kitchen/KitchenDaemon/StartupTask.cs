using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using System.Threading;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Sockets;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace KitchenDaemon
{
    public sealed class StartupTask : IBackgroundTask
    {
        //private static readonly String UrlPi = "http://192.168.1.12:5000/app";
        private static readonly String UrlPc = "http://192.168.1.13:5000/kitchen";

        HubConnection connection;

        Timer t1 = null;
        private Timer t2;
        private BackgroundTaskDeferral _Deferral;
        private float upperTankDepth = 0.0f;
        private float lowerTankDepth = 0.0f;

        private readonly string LOCAL_IP_ADDRESS = "192.168.1.13";
        private readonly int LOCAL_PORT = 4210;
        private readonly int REMOTE_PORT = 4210;
        private readonly string REMOTE_IP_ADDRESS = "192.168.1.25";

        private UdpClient udpClient { get; set; }

        //private void InitLevels()
        //{
        //    UpperTankLevel = new WaterLevel(26, 19, 13, 6);
        //    UpperTankLevel.EventLevels += UpperTankLevel_EventLevels;

        //    LowerTankLevel = new WaterLevel(21, 20, 16, 12);
        //    LowerTankLevel.EventLevels += LowerTankLevel_EventLevels;
        //}

        //private void InitPumps()
        //{
        //    UpperTankPump = new Pump(2, "UpperTankPump"); //Red
        //    UpperTankPump.Init();
        //    UpperTankPump.EventPumpStatus += UpperTankPump_EventPumpStatus;

        //    LowerTankPump = new Pump(4, "LowerTankPump"); //Green
        //    LowerTankPump.Init();
        //    LowerTankPump.EventPumpStatus += LowerTankPump_EventPumpStatus;

        //    Mains = new Pump(17, "Mains"); //Green
        //    Mains.Init();
        //    Mains.Actuate(true);
        //}

        //private void UpperTankPump_EventPumpStatus(object sender, PumpStatusEventArgs e)
        //{
        //    //await connection.InvokeAsync("SetUpperTankPumpState", UpperTankPump.State);
        //    Debug.WriteLine("Upper Tank Set");
        //}

        //private void LowerTankPump_EventPumpStatus(object sender, PumpStatusEventArgs e)
        //{
        //    //await connection.InvokeAsync("SetLowerTankPumpState", LowerTankPump.State);
        //    Debug.WriteLine("Lower Tank Set");
        //}

        private async void InitSignalR()
        {
            connection = new HubConnectionBuilder()
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Debug);

                    loggingBuilder.ToString();
                })
                .WithUrl(UrlPc)
                .Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };

            connection.On<bool>("UpperTankPumpStatus", (state) =>
            {
                //UpperTankPump.Actuate(state);
                Debug.WriteLine($"OnConnect Upper Tank: {state}");
            });

            connection.On<bool>("LowerTankPumpStatus", (state) =>
            {
                //LowerTankPump.Actuate(state);
                Debug.WriteLine($"OnConnect Lower Tank: {state}");
            });

            await connection.StartAsync();
            await connection.InvokeAsync("GetPumpStates");
        }

        //private async void LowerTankLevel_EventLevels(object sender, LevelsEventArgs e)
        //{
        //    lowerTankDepth = e.Depth;
        //    Debug.WriteLine($"Lower Depth: {e.Depth}");

        //    if (e.Level == 3)
        //    {
        //        //LowerTankPump.Actuate(false);
        //        await connection.InvokeAsync("Notification", $"The {sender.ToString()} is full");
        //    }
        //    await connection.InvokeAsync("SetLowerTankLevel", lowerTankDepth);
        //}

        //private async void UpperTankLevel_EventLevels(object sender, LevelsEventArgs e)
        //{
        //    upperTankDepth = e.Depth;
        //    Debug.WriteLine($"Upper Depth: {e.Depth}");

        //    if (e.Level == 3)
        //    {
        //        //UpperTankPump.Actuate(false);
        //        await connection.InvokeAsync("Notification", "The tank: " + sender.ToString() + " is full");
        //    }

        //    await connection.InvokeAsync("SetUpperTankLevel", upperTankDepth);
        //}

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
            _Deferral = taskInstance.GetDeferral();

            InitSignalR();

            t1 = new Timer(t1Callback, null, 0, 5000);
            //t2 = new Timer(t2Callback, null, 0, 5000);

            udpClient = new UdpClient(LOCAL_PORT);
        }

        private void t2Callback(object state)
        {
            try
            {
                udpClient.Connect(REMOTE_IP_ADDRESS, REMOTE_PORT);

                Byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");

                udpClient.Send(sendBytes, sendBytes.Length);

                udpClient.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}");
            }
        }

        private void t1Callback(object state)
        {
            
        }
    }
}
