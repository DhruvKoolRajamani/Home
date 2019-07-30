using System;
using System.Collections.Generic;
using Devices;
using static Devices.DevicesExtensions;

namespace Home.Server.Daemons
{
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
        public string Name { get => deviceName; set => deviceName = value; }
        public List<int> DeviceIds { get => deviceIds; private set => deviceIds = value; }

        public event Action OnDepthChange = delegate { };
        public void Raise()
        {
            //Invoke OnChange Action
            OnDepthChange();
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
}