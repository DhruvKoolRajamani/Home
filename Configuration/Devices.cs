using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using static Devices.DevicesExtensions;

// TODO: Convert all populate methods to config file that is loaded.

namespace Devices
{
    //Message format : JSON
    //{
    //    "room" : {
    //        "source": (string),
    //        "destination": (string)
    //    },
    //    "time" : (YYYY-MM-DD:HH-Mm-SS-mm),
    //    "device" : {
    //        "name" : (string),
    //        "id" : (int),
    //        "dtype" : (type),
    //        "data" : (string)
    //    }
    //    "direction" : (string)[m2s | s2m],
    //    "status": (string)
    //}
    //Example:
    //{
    //    "room" : {
    //        "source": "Living Room",
    //        "destination": "Kitchen"
    //        },
    //    "time" : "2019-07-02:16-27-33-02",
    //    "device" : {
    //        "name" : "Motor",
    //        "id" : 1,
    //        "dtype" : "bool",
    //        "data" : true
    //    }
    //    "direction" : "m2s",
    //    "status": "Connected <websocket_id>"
    //}

    // public struct sRoom
    // {
    //     public string _room { get; set; }
    //     public string _ip { get; set; }
    //     public string _key { get; set; }

    //     public sRoom(string room, string ip, string key)
    //     {
    //         _room = room;
    //         _ip = ip;
    //         _key = key;
    //     }
    // }

    public struct sRoom
    {
        public string room { get; set; }
        public string ip { get; set; }

        public sRoom(string room_, string ip_)
        {
            room = room_;
            ip = ip_;
        }
    }

    //public class Data
    //{
    //    public Data(string _data = "")
    //    {
    //        if (_data != null)
    //            data = _data;
    //        else
    //            data = "";
    //    }

    //    [JsonPropertyName("data")]
    //    public string data
    //    {
    //        get
    //        {
    //            return data;
    //        }
    //        set
    //        {
    //            data = value;
    //            DType = data.GetType().ToString();
    //        }
    //    }

    //    [JsonPropertyName("dtype")]
    //    public string DType
    //    {
    //        get;
    //        set;
    //    }
    //}

    [Serializable]
    public class Room
    {
        public Room() { }
        public Room(string source = "", string destination = ""/*, int mcu = 1*/)
        {
            Source = source;
            Destination = destination;
            // MCU = mcu;
        }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("destination")]
        public string Destination { get; set; }
        // [JsonPropertyName("mcu")]
        // public int MCU { get; set; }
    }

    [Serializable]
    public class Device
    {
        public Device() { }
        public Device(string name = "", int id = 0, string data = "", string dtype = "")
        {
            Name = name;
            Id = id;
            Data = data;
            DType = dtype;
        }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("dtype")]
        public string DType { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    [Serializable]
    public class DeviceMessage
    {
        public DeviceMessage()
        {
            Stamp = DateTime.Now;
            Time = Stamp.ToString("yyyy-MM-dd:HH-mm-ss-fff");
        }
        public DeviceMessage(string direction = "", string status = "", Room room = null, Device device = null)
        {
            if (room != null)
                Room = room;

            if (device != null)
                Device = device;

            Time = SetTime();
            Debug.WriteLine(Time);
            Direction = direction;
            Status = status;
        }

        private Room room = new Room();
        [JsonPropertyName("room")]
        public Room Room { get { return room; } set { room = value; } }
        //public Room Room { get; set; }

        private Device device = new Device();
        [JsonPropertyName("device")]
        public Device Device { get { return device; } set { device = value; } }

        [JsonPropertyName("time")]
        public string Time { get; set; }
        [JsonIgnore]
        private DateTime Stamp { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        public override string ToString()
        {
            string str = "";

            str = $"Room: (\n" +
                    $"  Source: {Room.Source}\n" +
                    $"  Destination: {Room.Destination}\n" +
                    // $"  MCU: {Room.MCU}\n" +
                    $"  )\n" +
                    $"Time: {Time}\n" +
                    $"Device: (\n" +
                    $"  Name: {Device.Name}\n" +
                    $"  Id: {Device.Id}\n" +
                    $"  Data: {Device.Data}\n" +
                    $"  DType: {Device.DType}\n" +
                    $"  )\n" +
                    $"Direction: {Direction}\n" +
                    $"Status: {Status}";

            return str;
        }
    }
}