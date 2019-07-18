using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Server.Repositories
{
    public interface IKitchenRepo
    {
        bool UpperTankPumpState { get; set; }
        bool LowerTankPumpState { get; set; }

        float UpperTankDepth { get; set; }
        float LowerTankDepth { get; set; }
    }

    public class KitchenRepo : IKitchenRepo
    {
        private bool _upperTankPumpState;
        private bool _lowerTankPumpState;

        private float _upperTankDepth;
        private float _lowerTankDepth;

        public KitchenRepo()
        {
            _upperTankPumpState = UpperTankPumpState;
            _lowerTankPumpState = LowerTankPumpState;

            _upperTankDepth = UpperTankDepth;
            _lowerTankDepth = LowerTankDepth;
        }

        public bool UpperTankPumpState
        {
            get => _upperTankPumpState;
            set => _upperTankPumpState = value;
        }

        public bool LowerTankPumpState
        {
            get => _lowerTankPumpState;
            set => _lowerTankPumpState = value;
        }

        public float UpperTankDepth
        {
            get => _upperTankDepth;
            set => _upperTankDepth = value;
        }

        public float LowerTankDepth
        {
            get => _lowerTankDepth;
            set => _lowerTankDepth = value;
        }
    }
}