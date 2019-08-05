using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Device.Gpio;

using Microsoft.Extensions.Logging;

namespace Gpio
{
    public class GpioEventArgs : EventArgs
    {
        public bool Level { get; set; }
        public string DebugMessage { get; set; }

        public GpioEventArgs(bool level)
        {
            Level = level;
        }

        public GpioEventArgs(bool level, string msg)
        {
            Level = level;
            DebugMessage = msg;
        }

        public GpioEventArgs(string msg)
        {
            DebugMessage = msg;
        }
    }

    public class Pin
    {
        private int _gpioPinId;
        private ManualResetEventSlim mre;
        private GpioController gpioController;
        private bool eventRaised = false;

        public int GpioPinId { get => _gpioPinId; set => _gpioPinId = value; }

        public event EventHandler<GpioEventArgs> EventGpio = delegate { };
        public Pin()
        {
            mre = new ManualResetEventSlim();
        }

        public string Init(PinMode driveMode, int gpioPinId)
        {
            GpioPinId = gpioPinId;

            // using (gpioController = new GpioController())
            // {
            try
            {
                gpioController = new GpioController();
                gpioController.OpenPin(GpioPinId, driveMode);

                Debug.WriteLine($"Pin: {GpioPinId} is open and set to {driveMode}");

                if (driveMode == PinMode.Input)
                {
                    gpioController.RegisterCallbackForPinValueChangedEvent(GpioPinId, PinEventTypes.Rising, callback);
                }
                else if (driveMode == PinMode.InputPullUp)
                {
                    gpioController.RegisterCallbackForPinValueChangedEvent(GpioPinId, PinEventTypes.Rising, callback);
                }
                else if (driveMode == PinMode.InputPullDown)
                {
                    gpioController.RegisterCallbackForPinValueChangedEvent(GpioPinId, PinEventTypes.Rising, callback);
                }
                else if (driveMode == PinMode.Output)
                {
                    gpioController.Write(GpioPinId, PinValue.Low);
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            // }
            return $"Initialized pin {gpioPinId} successfully";
        }

        public void Pulse(double delay)
        {
            if (gpioController.GetPinMode(GpioPinId) == PinMode.Output)
            {
                if (!eventRaised)
                {
                    EventGpio(this, new GpioEventArgs(false, $"{GpioPinId} reads Low"));
                }
                gpioController.Write(GpioPinId, PinValue.High);

                delayMilli(delay);

                gpioController.Write(GpioPinId, PinValue.Low);

                eventRaised = false;
            }
        }

        // public void Read()
        // {
        //     if ((_gpioPin.GetPinMode() == PinMode.Input) || (_gpioPin.GetPinMode() == PinMode.InputPullUp))
        //     {
        //         //PinValue gp = _gpioPin.Read();
        //         //Debug.WriteLine($"Time for {_gpioPin.PinNumber} : {_stopwatch.Elapsed.TotalMilliseconds} : Value : {gp}");
        //     }
        // }

        private void callback(object sender, PinValueChangedEventArgs args)
        {
            var value = gpioController.Read(args.PinNumber);

            if (value == PinValue.High)
            {
                EventGpio(this, new GpioEventArgs(true, $"{GpioPinId} reads {value}"));
                eventRaised = true;
            }
        }

        private void delayMilli(double delay)
        {
            mre.Wait(
                TimeSpan.FromMilliseconds(
                    (delay)
                )
            );
        }
    }
}
