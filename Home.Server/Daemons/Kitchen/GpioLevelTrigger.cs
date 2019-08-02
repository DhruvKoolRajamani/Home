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

namespace Home.Server.Daemons
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

        public string LogString { get; set; }

        public event EventHandler<TankStatusChangedEventArgs> EventTankStatusChanged = delegate { };

        public GpioLevelTrigger(int trigger, int level_1, int level_2, int level_3, int tankId)
        {
            // _logger = logger;
            TankId = tankId;

            TRIGGER = trigger;

            LevelPins = new int[3];

            LevelPins[0] = level_1;
            LevelPins[1] = level_2;
            LevelPins[2] = level_3;

            gpioLevels = new Pin[3];
            gpioTrigger = new Pin();
            
            var log = gpioTrigger.Init(PinMode.Output, TRIGGER);

            for (int i = 0; i < 3; i++)
            {
                gpioLevels[i] = new Pin();
                var logstr = gpioLevels[i].Init(PinMode.InputPullDown, LevelPins[i]);
                gpioLevels[i].EventGpio += TankStatusChange_EventGpio;
            }
            // throw new DevicesProtocolException(log);
        }

        public void InitPins()
        {
            gpioTrigger.Init(PinMode.Output, TRIGGER);
            for (int i = 0; i < 3; i++)
            {
                gpioLevels[i] = new Pin();
                var logstr = gpioLevels[i].Init(PinMode.InputPullDown, LevelPins[i]);
                // gpioLevels[i].EventGpio += TankStatusChange_EventGpio;
            }
        }

        private void TankStatusChange_EventGpio(object sender, GpioEventArgs e)
        {
            Pin gpio = sender as Pin;
            if (e.Level)
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
        }

        public void Ping()
        {
            _max = 0;

            Debug.WriteLine($"Trigger");

            gpioTrigger.Pulse(100);

            EventTankStatusChanged(this, new TankStatusChangedEventArgs(_max, TankId));
        }
    }
}