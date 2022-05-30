using System;
using UnityEngine;

namespace Vehicles{
    public enum PowerDirection { Forward, Backward }

    public interface IDriveDirection
    {
        PowerDirection GetPowerDirection { get; }
        void ToggleDriveDirection(float speed);
        Action<PowerDirection> OnDriveDirectionStateChange { get; set; }
        void Reset();
    }

    public class DriveDirection : IDriveDirection
    {
        public DriveDirection()
        {
            powerDir = PowerDirection.Forward;
        }
        PowerDirection powerDir;

        public PowerDirection GetPowerDirection => powerDir;

        public Action<PowerDirection> OnDriveDirectionStateChange {get; set;}

        public void ToggleDriveDirection(float speed)
        {
            if(speed > 1) return;

            powerDir = powerDir == PowerDirection.Forward ? powerDir = PowerDirection.Backward : PowerDirection.Forward;

            OnDriveDirectionStateChange?.Invoke(powerDir);
        }

        public void Reset(){
            powerDir = PowerDirection.Forward;

            OnDriveDirectionStateChange?.Invoke(powerDir);
        }
    }
}