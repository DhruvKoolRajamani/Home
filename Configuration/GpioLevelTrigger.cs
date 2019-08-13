using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gpio;
using System.Device.Gpio;
using Devices;

using Microsoft.Extensions.Logging;

namespace Devices
{
    public class GpioLevelTrigger
    {
        private int TRIGGER;
        private int[] LevelPins;

        private int n = 0;

        private int _max = 0;

        private Pin gpioTrigger;
        private Pin[] gpioLevels;

        public int TankId { get; set; }

        public string GpioDebugString { get; set; }

        public string LogString { get; set; }

        public event EventHandler<TankStatusChangedEventArgs> EventTankStatusChanged = delegate { };

        public GpioLevelTrigger(int trigger, int level_1, int level_2, int level_3, int tankId)
        {
            TankId = tankId;

            TRIGGER = trigger;

            LevelPins = new int[3];

            LevelPins[0] = level_1;
            LevelPins[1] = level_2;
            LevelPins[2] = level_3;

            gpioLevels = new Pin[3];
            gpioTrigger = new Pin();

            try
            {
                Debug.WriteLine(gpioTrigger.Init(PinMode.Output, TRIGGER));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message}");
            }

            for (int i = 0; i < 3; i++)
            {
                gpioLevels[i] = new Pin();
                try
                {
                    Debug.WriteLine(gpioLevels[i].Init(PinMode.InputPullDown, LevelPins[i]));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{ex.Message}");
                }
                gpioLevels[i].EventGpio += TankStatusChange_EventGpio;
            }
            // throw new DevicesProtocolException(log);
        }

        public void InitPins()
        {
            try
            {
                gpioTrigger.Init(PinMode.Output, TRIGGER);
                for (int i = 0; i < 3; i++)
                {
                    gpioLevels[i] = new Pin();
                    Debug.WriteLine(gpioLevels[i].Init(PinMode.InputPullDown, LevelPins[i]));
                    gpioLevels[i].EventGpio += TankStatusChange_EventGpio;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void TankStatusChange_EventGpio(object sender, GpioEventArgs e)
        {
            Pin gpio = sender as Pin;
            GpioDebugString = e.DebugMessage;
            if (e.Level) // bool
            {
                if (gpio.GpioPinId == LevelPins[0])
                {
                    n = 1;
                }
                else if (gpio.GpioPinId == LevelPins[1])
                {
                    n = 2;
                }
                else if (gpio.GpioPinId == LevelPins[2])
                {
                    n = 3;
                }
                else
                    Debug.WriteLine("Error");
                _max = Math.Max(_max, n);
            }

            EventTankStatusChanged(this, new TankStatusChangedEventArgs(_max, TankId, GpioDebugString));
        }

        public float Ping()
        {
            _max = 0;

            Debug.WriteLine($"Trigger");

            // gpioTrigger.Pulse(1000);

            int i = 0;
            for (i = 0; i < gpioLevels.Length; i++)
            {
                if (gpioLevels[i].Probe() == 0)
                    break;
            }
            return (float)i/(float)gpioLevels.Length;
            // EventTankStatusChanged(this, new TankStatusChangedEventArgs(i, TankId, GpioDebugString));
        }
    }
}