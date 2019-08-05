using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Net;
using Devices;
using static Devices.DevicesExtensions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Home.Server.Daemons
{
    public abstract class Daemon : BackgroundService
    {
        public string currentName = "";
        private List<Microcontroller> microcontroller;
        private Timer timer;
        public Timer _Timer { get => timer; set => timer = value; }
        protected ILogger _logger;
        public static bool messageSent = false;
        private List<UdpClient> mUdpClient = new List<UdpClient>();

        public List<Microcontroller> Microcontroller { get => microcontroller; set => microcontroller = value; }
        public List<UdpClient> MUdpClient { get => mUdpClient; set => mUdpClient = value; }

        public virtual string CurrentName { get => currentName; set => currentName = value; }

        public Daemon(List<Microcontroller> micro, ILogger<Daemon> logger)
        {
            _logger = logger;
            Microcontroller = micro;
            foreach (var mcu in Microcontroller)
            {
                if (mcu.UdpPort == 0)
                    throw new DevicesProtocolException("Invalid Port Set");
                else if (mcu.MUdpClient != null)
                {
                    _logger.LogInformation($"Udp Port already exists");
                }
                else
                {
                    try
                    {
                        _logger.LogInformation($"Instantiating UDP Port: {mcu.UdpPort}\n");
                        Microcontroller.Where(m => (m == mcu)).FirstOrDefault().MUdpClient = new UdpClient(mcu.UdpPort);
                        MUdpClient.Add(Microcontroller.Where(m => (m == mcu)).FirstOrDefault().MUdpClient);
                        MUdpClient.Last().BeginReceive(new AsyncCallback(OnUdpData), MUdpClient.Last());
                    }
                    catch (SocketException se)
                    {
                        _logger.LogInformation($"{se.Message}\n");
                    }
                }
            }

            _Timer = new Timer(timerCallback, null, 5000, 5000);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (MUdpClient != null)
                    foreach (var client in MUdpClient)
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                client.BeginReceive(new AsyncCallback(OnUdpData), client);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogInformation($"\nError: {ex.Message}\n");
                            }
                        }
                    }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception in {this.ToString()}, {ex.Source} at {LineNumber(ex)} with Exception: {ex.Message}");
            }

            await Task.Delay(1000, cancellationToken);
        }

        public void OnUdpData(IAsyncResult result)
        {
            // this is what had been passed into BeginReceive as the second parameter:
            UdpClient client = result.AsyncState as UdpClient;
            // points towards whoever had sent the message:
            IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);
            var mcu = Microcontroller.Where(m => m.IPAddress == source.Address.ToString()).FirstOrDefault();
            // get the actual message and fill out the source:
            Byte[] receivedBytes = client.EndReceive(result, ref source);
            // do what you'd like with `message` here:
            string message = Encoding.ASCII.GetString(receivedBytes);
            if (message[0] == 'N')
            {
                // NACK received
                var msg = message.Replace("N^", "*^");
                SendMessage(mcu.Room, mcu.Id, msg);
            }
            else if (message[0] == '*')
            {
                // Process Message as MCU to Server message
                // _logger.LogInformation($"Do Something with : {message}");
                ProcessMessage(message);
            }
            else if (message[0] == 'A')
            {
                ProcessMessage(message, "A");
                messageSent = false;
            }
            else
            {
                string ex = "Invalid response from client";
                _logger.LogInformation($"{ex}");
                throw new DevicesProtocolException(ex);
            }

            // schedule the next receive operation once reading is done:
            client.BeginReceive(new AsyncCallback(OnUdpData), client);
        }

        public void SendMessage(string room, int id, string msg = "")
        {
            var mcu = Microcontroller.Where(m => (m.Room == room) && (m.Id == id)).FirstOrDefault();
            // schedule the first receive operation:
            mcu.MUdpClient.BeginReceive(new AsyncCallback(OnUdpData), mcu.MUdpClient);

            IPEndPoint target = new IPEndPoint(IPAddress.Parse(mcu.IPAddress), mcu.UdpPort);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(msg);
            mcu.MUdpClient.Send(sendBytes, sendBytes.Length, target);
            _logger.LogInformation($"Sent \n{Encoding.ASCII.GetString(sendBytes)}");
            _logger.LogInformation($"\nPacket Size: {sendBytes.Length}\n");
            messageSent = true;
        }

        protected virtual void timerCallback(object state) { }

        protected virtual bool ProcessMessage(string message, string Ack = "*") { return true; }
    }
}