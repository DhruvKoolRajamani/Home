using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Devices;
using static Devices.DevicesExtensions;

namespace Home.Server.Daemons
{
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
        private GpioLevelTrigger gpioLevelTrigger;

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
        public GpioLevelTrigger _GpioLevelTrigger { get => gpioLevelTrigger; set => gpioLevelTrigger = value; }

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

    public class Switch : IDevice
    {
        private bool state;
        private int id;
        private string name;
        public bool State { get => state; set => state = value; }
        public int Id { get => id; set => id = value; }
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