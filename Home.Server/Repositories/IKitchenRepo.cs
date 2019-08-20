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
using System.Net.Sockets;
using Home.Server.Hubs;
using System.Net;



namespace Home.Server.Repositories
{
    
    public interface IKitchenRepo
    {
        Tank UpperTank { get; set; }
        Tank LowerTank { get; set; }
        Vent ChimneyVent { get; set; }
        DHT11 Dht11 { get; set; }
        public void ProcMsg(string[] arrMsg);
    }

    public class KitchenRepo : IKitchenRepo
    {
        private static ILogger _logger;

        public KitchenRepo(ILogger<KitchenRepo> logger)
        {
            _logger = logger;
            
        }


        private Tank upperTank = new Tank() { Id = 0x01, Name = "Upper Tank", State = false, Depth = 0.0f };
        private Tank lowerTank = new Tank() { Id = 0x02, Name = "Lower Tank", State = false, Depth = 0.0f };
        private Vent chimneyVent = new Vent() { Id = 0x00, Name = "Chimney Vent", State = false, Speed = 0, CalibrationState = false };
        private DHT11 dht11 = new DHT11() { Id = 0x00, Name = "DHT Sensor", State = false, Temp = 0.0f, Humidity = 0.0f };

        public Tank UpperTank { get => upperTank; set => upperTank = value; }
        public Tank LowerTank { get => lowerTank; set => lowerTank = value; }
        public Vent ChimneyVent { get => chimneyVent; set => chimneyVent = value; }
        public DHT11 Dht11 { get => dht11; set => dht11 = value; }

        public void ProcMsg(string[] arrMsg)
        {

        }
    }


    public interface iCommunication
    {
        public void Register(string sMsg);
        public void SendMessage(string sMsg, IPEndPoint[] msgTargets);
    }
    public class Wire : iCommunication
    {
        public UdpClient _UDPClient = null;
        private ILogger _logger = null;
        private IKitchenRepo _kitRep = null;
        private IKitchenHub _kitchen = null;
        private IPEndPoint _ipLocalEP;
        public Wire(IHome _home , IKitchenHub kitchen, ILogger<Wire> logger, IKitchenRepo kitchenRepo)
        {
            //gather the services
            _logger = logger;
            _kitRep = kitchenRepo;
            _kitchen = kitchen;
            
            InitUdP();
        }
        private void InitUdP()
        {
            _ipLocalEP = new IPEndPoint(IPAddress.Any, 11466);
            _UDPClient = new UdpClient(_ipLocalEP);
            _logger.LogInformation($"Listening on UDP:{_ipLocalEP}");
            _UDPClient.BeginReceive(new AsyncCallback(OnUdpData), _UDPClient);

        }
        private void OnUdpData(IAsyncResult result)
        {
            UdpClient client = result.AsyncState as UdpClient;
            //merely for loggin source of message
            IPEndPoint source = new IPEndPoint(IPAddress.Any, 0);

            Byte[] receivedBytes = client.EndReceive(result, ref source);

            //stick to ascii because of ESP8266. odnt know if they work with utf.....
            string message = Encoding.ASCII.GetString(receivedBytes);
            _logger.LogInformation($"recd : {source.ToString()} {message}");

            // use ^ as per protocol ;  
            string[] sMsgPack = message.Split(new char[] { '^' });
            _kitRep.ProcMsg(sMsgPack);
            //Back to listening
            client.BeginReceive(new AsyncCallback(OnUdpData), client);
        }
        public void Register(string sMsg)
        {
            //throw new NotImplementedException();
        }

        public void SendMessage(string msg, IPEndPoint[] msgTargets)
        {

            Byte[] sendBytes = Encoding.ASCII.GetBytes(msg);
            foreach (var ipEP in msgTargets)
            {
                _UDPClient.Send(sendBytes, sendBytes.Length, ipEP);
                _logger.LogInformation($"Msg: {msg} - {sendBytes.Length} bytes sent to {ipEP}");
            }

        }
    }

}