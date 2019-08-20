using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Net;
using static Devices.DevicesExtensions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Gpio;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

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
        private IHubContext<Hub> hubContext;
        protected ILogger _logger;
        

        public int Id { get => mcuId; set => mcuId = value; }
        public string Room { get => mcuRoom; set => mcuRoom = value; }
        public string IPAddress { get => mcuIPAddress; set => mcuIPAddress = value; }
        public int UdpPort { get => mcuUdpPort; set => mcuUdpPort = value; }

        
        public Microcontroller()
        {

           
        }
        protected void SetLogger(ILogger log)
        {
            _logger = log;
        }
        protected virtual void SendMessage(string msg)
        {
        }
        protected virtual bool ProcessMessage(string message, string Ack = "*") { return true; }
        protected virtual void Do() { }

    }

    public class TankStatusChangedEventArgs : EventArgs
    {
        private float depth = 0.0f;
        public int Level { get; set; }
        public string DebugMessage { get; set; }

        public float Depth { get { return GetLevel(); } }
        public int TankID { get; set; }
        public bool State { get; set; }

        public TankStatusChangedEventArgs(int level, int id)
        {
            TankID = id;
            Level = level;
        }

        public TankStatusChangedEventArgs(bool state, int id)
        {
            State = state;
            TankID = id;
        }

        public TankStatusChangedEventArgs(int level, int id, string debugMessage)
        {
            TankID = id;
            Level = level;
            DebugMessage = debugMessage;
        }

        private float GetLevel()
        {
            depth = (Level / 3.0f);
            return depth;
        }
    }

    public class Vent : IDevice
    {
        public Vent()
        {
            State = false;
            Id = 0;
            Name = "Chimney Vent";
            CalibrationState = false;
            Speed = 0;
        }

        private bool deviceState;
        private bool calibrationState;
        private int speed;
        private int deviceId;
        private string deviceName;


        public bool State { get => deviceState; set => deviceState = value; }
        public bool CalibrationState { get => calibrationState; set => calibrationState = value; }
        public int Speed { get => speed; set => speed = value; }
        public int Id { get => deviceId; set => deviceId = value; }
        public string Name { get => deviceName; set => deviceName = value; }


    }

    public class Tank : IDevice
    {
        public Tank()
        {
            State = false;
            Name = "Upper Tank";
            Id = 1;
            Depth = 0.0f;
        }

        private bool deviceState;
        private float depth;
        private int deviceId;
        private string deviceName;
        private static bool tankFilled = false;

        private List<int> deviceIds = new List<int>() { 1, 2 };

        public bool State { get => deviceState; set => deviceState = value; }
        public float Depth { get => depth; set => depth = value; }
        public int Id
        {
            get => deviceId;
            set
            {
                if (!DeviceIds.Contains(value))
                    throw new DevicesProtocolException("Tank Ids out of bounds");
                else deviceId = value;
            }
        }

        public int[] LevelPins { get; set; }
        public string Name { get => deviceName; set => deviceName = value; }
        public static bool TankFilled { get => tankFilled; set => tankFilled = value; }
        public List<int> DeviceIds { get => deviceIds; private set => deviceIds = value; }
        public event EventHandler<TankStatusChangedEventArgs> OnTankStatusChanged = delegate { };
        public void RaiseTankStatusChangedEvent(int level)
        {
            OnTankStatusChanged(this, new TankStatusChangedEventArgs(level, Id));
        }

        public void RaiseTankStatusChangedEvent(bool state)
        {
            OnTankStatusChanged(this, new TankStatusChangedEventArgs(state, Id));
        }
    }

    [Serializable()]
    public class Switch : IDevice
    {
        private bool state;
        private int id;
        private string name;

        [JsonPropertyName("state")]
        public bool State { get => state; set => state = value; }
        [JsonPropertyName("id")]
        public int Id { get => id; set => id = value; }
        [JsonPropertyName("name")]
        public string Name { get => name; set => name = value; }
    }

    [Serializable()]
    public class Relay : IDevice
    {
        private bool state;
        private int id;
        private string name;

        [JsonPropertyName("state")]
        public bool State { get => state; set => state = value; }
        [JsonPropertyName("id")]
        public int Id { get => id; set => id = value; }
        [JsonPropertyName("name")]
        public string Name { get => name; set => name = value; }
    }

    public class DHT11 : IDevice
    {
        private bool state;
        private int id;
        private string name;
        private float temp;
        private float humidity;
        public bool State { get => state; set => state = value; }
        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public float Temp { get => temp; set => temp = value; }
        public float Humidity { get => humidity; set => humidity = value; }

        public event EventHandler<EventArgs> OnWeatherDataChanged = delegate { };
        public void RaiseWeatherDataChangedEvent()
        {
            OnWeatherDataChanged(this, new EventArgs());
        }
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