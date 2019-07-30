using System;
using System.Collections.Generic;
using System.Text;
using Home.Server.Daemons;

namespace Home.Server.Repositories
{
    public interface IKitchenRepo
    {
        Tank UpperTank { get; set; }
        Tank LowerTank { get; set; }
        Vent ChimneyVent { get; set; }
        // bool UpperTankPumpState { get; set; }
        // bool LowerTankPumpState { get; set; }

        // Tuple<bool, int> VentState { get; set; } // bool state, int speed
        // bool VentCalibrationState { get; set; }

        // float UpperTankDepth { get; set; }
        // float LowerTankDepth { get; set; }
    }

    public class KitchenRepo : IKitchenRepo
    {
        private Tank upperTank = new Tank() { Id = 1, Name = "Upper Tank", State = false, Depth = 0.0f };
        private Tank lowerTank = new Tank() { Id = 2, Name = "Lower Tank", State = false, Depth = 0.0f };
        private Vent chimneyVent = new Vent() { Id = 0, Name = "Chimney Vent", State = false, Speed = 0, CalibrationState = false };

        public Tank UpperTank { get => upperTank; set => upperTank = value; }
        public Tank LowerTank { get => lowerTank; set => lowerTank = value; }
        public Vent ChimneyVent { get => chimneyVent; set => chimneyVent = value; }
    }
}