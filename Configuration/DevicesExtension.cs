using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace Devices
{
    public static class DevicesExtensions
    {
        private static List<sRoom> _roomsList = new List<sRoom>();
        public static List<sRoom> RoomsList { get { return _roomsList; } set { _roomsList = value; } }
        private static Dictionary<string, int> _devicesDict = new Dictionary<string, int>();
        public static Dictionary<string, int> DevicesDict { get { return _devicesDict; } set { _devicesDict = value; } }

        public static string LivingRoomKey = "01";
        public static string DemoClientKey = "03";
        public static string KitchenKey = "02";

        public static void PopulateRooms()
        {
            _roomsList = new List<sRoom>
            {
                // new sRoom(LivingRoomKey, DevicesExtensions.LocalIP),
                new sRoom(KitchenKey, "192.168.1.25"),
                new sRoom(DemoClientKey, DevicesExtensions.LocalIP)
            };
        }

        public static void PopulateDevices()
        {
            _devicesDict = new Dictionary<string, int>
            {
                { "Booster", 1 },
                { "Motor", 1 },
                { "Water Level", 2 },
                { "Vent", 1 },
                { "Web Client", 1 }
            };
        }

        private static string localIP;
        public static string LocalIP
        {
            get
            { return localIP; }
            set
            {
                localIP = value;
            }
        }

        public enum eDirection
        {
            M2S = 1,
            S2M = 2
        }

        public enum eEncoding
        {
            UTF8 = 0,
            ASCII = 1,
            Unicode
        }

        public static byte[] StringToByteArray(string enter, int encoding)
        {
            if (encoding == (int)eEncoding.UTF8)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(enter);
                return bytes;
            }
            else if (encoding == (int)eEncoding.ASCII)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(enter);
                return bytes;
            }
            else if (encoding == (int)eEncoding.Unicode)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(enter);
                return bytes;
            }

            return null;
        }

        public static string ByteArrayToString(byte[] enter, int encoding)
        {
            if (encoding == (int)eEncoding.UTF8)
            {
                string str = Encoding.UTF8.GetString(enter);
                string msg = str.Substring(0, str.IndexOf('\0'));
                return msg;
            }
            else if (encoding == (int)eEncoding.ASCII)
            {
                string str = Encoding.UTF8.GetString(enter);
                return str;
            }
            else if (encoding == (int)eEncoding.Unicode)
            {
                string str = Encoding.Unicode.GetString(enter);
                return str;
            }

            return null;
        }
        public static string SetDirection(int i)
        {
            string direction;
            switch (i)
            {
                case 1:
                    direction = "M2S";
                    break;
                case 2:
                    direction = "S2M";
                    break;
                default:
                    direction = "ERR";
                    break;
            }
            return direction;
        }

        // public static string Serialize(DeviceMessage DeviceMessage)
        // {
        //     string serialMsg = JsonSerializer.ToString<DeviceMessage>(DeviceMessage);
        //     return serialMsg;
        // }

        public static int defaultEncoding = (int)eEncoding.UTF8;
        public static byte[] Serialize(DeviceMessage DeviceMessage)
        {
            string serialMsg = JsonSerializer.ToString<DeviceMessage>(DeviceMessage);
            return StringToByteArray(serialMsg, defaultEncoding);

            //var serialMsg = JsonSerializer.ToUtf8Bytes<DeviceMessage>(DeviceMessage);
            //return serialMsg;
        }

        public static string Serialize(DeviceMessage DeviceMessage, bool DebugMode)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.ToString<DeviceMessage>(DeviceMessage, options);
        }

        public static DeviceMessage DeSerialize(string json)
        {
            // var options = new JsonSerializerOptions
            // {
            //     AllowTrailingCommas = true,
            //     WriteIndented = true
            // };

            return JsonSerializer.Parse<DeviceMessage>(json);
        }

        public static DeviceMessage DeSerialize(byte[] jsonArray)
        {
            string json = ByteArrayToString(jsonArray, defaultEncoding);

            return DeSerialize(json);
        }
        public static string DeSerializeToString(byte[] jsonArray)
        {
            string str = ByteArrayToString(jsonArray, defaultEncoding);

            return str;
        }

        public static string SetTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd:HH-mm-ss-fff");
        }

        public static int LineNumber(Exception ex)
        {
            var lineNumber = 0;
            const string lineSearch = ":line ";
            var index = ex.StackTrace.LastIndexOf(lineSearch);
            if (index != -1)
            {
                var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
                if (int.TryParse(lineNumberText, out lineNumber))
                {
                }
            }
            return lineNumber;
        }
    }
}
