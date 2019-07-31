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


// TODO: Convert all populate methods to config file that is loaded.

namespace Devices
{
    public interface IDevice
    {
        bool State { get; set; }
        int Id { get; set; }
        string Name { get; set; }
    }
    public class Microcontroller
    {
        private int mcuId;
        private string mcuRoom;
        private string mcuIPAddress;
        private int mcuUdpPort;
        private UdpClient microcontrollerUdpClient = null;
        public int Id { get => mcuId; set => mcuId = value; }
        public string Room { get => mcuRoom; set => mcuRoom = value; }
        public string IPAddress { get => mcuIPAddress; set => mcuIPAddress = value; }
        public int UdpPort { get => mcuUdpPort; set => mcuUdpPort = value; }
        public UdpClient MUdpClient { get => microcontrollerUdpClient; set => microcontrollerUdpClient = value; }
    }

    public abstract class Daemon : BackgroundService
    {
        private List<Microcontroller> microcontroller;
        private List<UdpClient> udpClients = null;
        private bool messageSent = false;
        public List<Microcontroller> Microcontroller { get => microcontroller; set => microcontroller = value; }
        public List<UdpClient> MUdpClient
        {
            get
            {
                return udpClients;
            }
            set
            {
                if (udpClients == null)
                    udpClients = new List<UdpClient>();
                foreach (var mcu in Microcontroller)
                {
                    if (!udpClients.Contains(mcu.MUdpClient))
                    {
                        udpClients.Add(mcu.MUdpClient);
                    }
                }
                udpClients = value;
            }
        }

        public Daemon(List<Microcontroller> micro)
        {
            Microcontroller = micro;
            foreach (var mcu in Microcontroller)
            {
                if (mcu.UdpPort == 0)
                    throw new DevicesProtocolException("Invalid Port Set");
                else
                {
                    Microcontroller.Where(m => (m == mcu)).FirstOrDefault().MUdpClient = new UdpClient(mcu.UdpPort);
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var client in MUdpClient)
                {
                    var mcu = Microcontroller.Where(m => m.MUdpClient == client).FirstOrDefault();
                    if (client == null)
                    {
                        if (mcu.UdpPort == 0)
                            throw new DevicesProtocolException("Invalid Port Set");
                        else
                            mcu.MUdpClient = new UdpClient(mcu.UdpPort);
                    }
                    else
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                client.BeginReceive(new AsyncCallback(OnUdpData), client);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"\nError: {ex.Message}\n");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in {this.ToString()}, {ex.Source} at {LineNumber(ex)} with Exception: {ex.Message}");
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
                // Debug.WriteLine($"Do Something with : {message}");
                ProcessMessage(message);
            }
            else if (message[0] == 'A')
            {
                messageSent = false;
            }
            else
            {
                string ex = "Invalid response from client";
                Debug.WriteLine($"{ex}");
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
            Debug.WriteLine($"Sent \n{Encoding.ASCII.GetString(sendBytes)}");
            Debug.WriteLine($"\nPacket Size: {sendBytes.Length}\n");
        }

        protected virtual void ProcessMessage(string message) { }
    }
}

// public struct sRoom
//     {
//         public string room { get; set; }
//         public string ip { get; set; }

//         public sRoom(string room_, string ip_)
//         {
//             room = room_;
//             ip = ip_;
//         }
//     }

//     public struct Stamp
//     {
//         // private string year;
//         // private string month;
//         // private string day;
//         // private string hour;
//         // private string minute;
//         // private string second;
//         // private string millisecond;

//         // public Stamp()
//         // {
//         //     SetDateTime();
//         // }

//         [XmlAttribute]
//         public string Year { get; set; }
//         [XmlAttribute]
//         public string Month { get; set; }
//         [XmlAttribute]
//         public string Day { get; set; }
//         [XmlAttribute]
//         public string Hour { get; set; }
//         [XmlAttribute]
//         public string Minute { get; set; }
//         [XmlAttribute]
//         public string Second { get; set; }
//         [XmlAttribute]
//         public string Millisecond { get; set; }

//         public override string ToString()
//         {
//             string str = "";
//             str = $"{Year}-{Month}-{Day}:{Hour}-{Minute}-{Second}-{Millisecond}\n";
//             return str;
//         }
//     }

//     public class Device
//     {
//         public Device()
//         {
//         }
//         public Device(string name = "", int id = 0, string data = null, bool state = false)
//         {
//             Name = name;
//             Id = id;
//             SData = data;
//             State = state;
//             if (data == null)
//                 dType = 3;
//             else
//                 dType = 0;
//         }

//         public Device(string name = "", int id = 0, Nullable<int> data = null, bool state = false)
//         {
//             Name = name;
//             Id = id;
//             IData = data;
//             State = state;
//             if (data == null)
//                 dType = 3;
//             else
//                 dType = 1;
//         }

//         public Device(string name = "", int id = 0, Nullable<float> data = null, bool state = false)
//         {
//             Name = name;
//             Id = id;
//             FData = data;
//             State = state;
//             if (data == null)
//                 dType = 3;
//             else
//                 dType = 2;
//         }

//         private string sdata;
//         [XmlIgnore]
//         public string SData { get { return sdata; } set { sdata = value; dType = 0; Data = sdata.ToString(); } }
//         private Nullable<float> fdata;
//         [XmlIgnore]
//         public Nullable<float> FData { get { return fdata; } set { fdata = value; dType = 2; Data = fdata.ToString(); } }
//         private Nullable<int> idata;
//         [XmlIgnore]
//         public Nullable<int> IData { get { return idata; } set { idata = value; dType = 1; Data = idata.ToString(); } }
//         private int dType = 0;
//         public int GetDType() { return dType; }
//         [XmlElement]
//         public bool State { get; set; }
//         [XmlElement]
//         public string Name { get; set; }
//         [XmlElement]
//         public int Id { get; set; }
//         [XmlElement]
//         public string Data { get; set; }
//     }

//     public class DeviceMessage
//     {
//         public DeviceMessage()
//         {
//             // _Stamp = new Stamp();
//             SetDateTime();
//         }
//         public DeviceMessage(Device device = null)
//         {
//             if (device != null)
//                 _Device = device;

//             // _Stamp = new Stamp();
//             SetDateTime();
//         }

//         private Device _device;
//         [XmlElement("Device")]
//         public Device _Device { get { return _device; } set { _device = value; } }

//         private Stamp _stamp = new Stamp();
//         [XmlElement("Stamp")]
//         public Stamp _Stamp { get { return _stamp; } set { _stamp = value; } }

//         public string Serialize()
//         {
//             try
//             {
//                 XmlWriterSettings settings = new XmlWriterSettings();
//                 settings.Indent = true;
//                 settings.IndentChars = "\t";
//                 settings.OmitXmlDeclaration = true;
//                 settings.ConformanceLevel = ConformanceLevel.Document;

//                 var stringwriter = new StringWriter();

//                 using (XmlWriter writer = XmlWriter.Create(stringwriter, settings))
//                 {
//                     var serializer = new XmlSerializer(this.GetType());

//                     var xns = new XmlSerializerNamespaces();
//                     xns.Add(string.Empty, string.Empty);

//                     serializer.Serialize(writer, this, xns);
//                     writer.Close();
//                     return stringwriter.ToString();
//                 }
//             }
//             catch (Exception ex)
//             {
//                 throw;
//             }

//             // return serialMsg;
//         }

//         public override string ToString()
//         {
//             string str = "";

//             switch (_Device.GetDType())
//             {
//                 case 0:
//                     str =
//                             $"Stamp: {_Stamp.ToString()}\n" +
//                             $"Device: (\n" +
//                             $"  Name: {_Device.Name}\n" +
//                             $"  Id: {_Device.Id}\n" +
//                             $"  Data: {_Device.SData}\n" +
//                             $"  State: {_Device.State}\n" +
//                             $"  )\n";
//                     break;

//                 case 1:
//                     str =
//                             $"Stamp: {_Stamp.ToString()}\n" +
//                             $"Device: (\n" +
//                             $"  Name: {_Device.Name}\n" +
//                             $"  Id: {_Device.Id}\n" +
//                             $"  Data: {_Device.IData}\n" +
//                             $"  State: {_Device.State}\n" +
//                             $"  )\n";
//                     break;

//                 case 2:
//                     str =
//                             $"Stamp: {_Stamp.ToString()}\n" +
//                             $"Device: (\n" +
//                             $"  Name: {_Device.Name}\n" +
//                             $"  Id: {_Device.Id}\n" +
//                             $"  Data: {_Device.FData}\n" +
//                             $"  State: {_Device.State}\n" +
//                             $"  )\n";
//                     break;

//                 default:
//                     str =
//                             $"Stamp: {_Stamp.ToString()}\n" +
//                             $"Device: (\n" +
//                             $"  Name: {_Device.Name}\n" +
//                             $"  Id: {_Device.Id}\n" +
//                             $"  Data: NULL\n" +
//                             $"  State: {_Device.State}\n" +
//                             $"  )\n";
//                     break;
//             }

//             return str;
//         }

//         public string SetDateTime()
//         {
//             _stamp.Year = DateTime.Now.ToString("yyyy");
//             _stamp.Month = DateTime.Now.ToString("MM");
//             _stamp.Day = DateTime.Now.ToString("dd");
//             _stamp.Hour = DateTime.Now.ToString("hh");
//             _stamp.Minute = DateTime.Now.ToString("mm");
//             _stamp.Second = DateTime.Now.ToString("ss");
//             _stamp.Millisecond = DateTime.Now.ToString("fff");

//             return _stamp.ToString();
//         }

//         public string ToKippuMessage()
//         {
//             // <Ack>.<device_id>.<command>.<data>.<stop>
//         }
//     }