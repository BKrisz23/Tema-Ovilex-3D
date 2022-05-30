using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    //This part of the component contains the gear shift logic (automatic and manual), 
    //and the logic for transferring torque from the engine to the drive wheels.
    public partial class CarController :VehicleController
    {
        public GearboxConfig Gearbox;
        public event System.Action<int> OnChangeGearAction;

        int _CurrentGear;
        public int CurrentGear              //Current gear, starting at -1 for reverse gear: -1 - reverse, 0 - neutral, 1 - 1st gear, etc.
        {
            get { return _CurrentGear; }

            set
            {
                if (_CurrentGear != value)
                {
                    _CurrentGear = value;
                    OnChangeGearAction.SafeInvoke (_CurrentGear);
                }
            }
        }

        public int CurrentGearIndex { get { return CurrentGear + 1; } }     //Current gear index, starting at 0 for reverse gear: 0 - reverse, 1 - neutral, 2 - 1st gear, etc.
        public bool InChangeGear { get { return ChangeGearTimer > 0; } }

        float ChangeGearTimer = 0;
        float[] AllGearsRatio;
        Wheel[] DriveWheels;

        void AwakeTransmition ()
        {
            //Calculated gears ratio with main ratio
            AllGearsRatio = new float[Gearbox.GearsRatio.Length + 2];
            AllGearsRatio[0] = -Gearbox.ReversGearRatio * Gearbox.MainRatio;
            AllGearsRatio[1] = 0;
            for (int i = 0; i < Gearbox.GearsRatio.Length; i++)
            {
                AllGearsRatio[i + 2] = Gearbox.GearsRatio[i] * Gearbox.MainRatio;
            }

            var driveWheels = new  List<Wheel>();
            foreach (var wheel in Wheels)
            {
                if (wheel.DriveWheel)
                {
                    driveWheels.Add (wheel);
                }
            }

            DriveWheels = driveWheels.ToArray ();
        }

        void FixedUpdateTransmition ()
        {
            if (!Mathf.Approximately (CurrentAcceleration, 0))
            {
                var motorTorque = CurrentAcceleration * (CurrentMotorTorque * (MaxMotorTorque * AllGearsRatio[CurrentGearIndex]));

                if (InChangeGear)
                {
                    motorTorque = 0;
                }

                //Calculation of target rpm for driving wheels.
                var targetWheelsRPM = AllGearsRatio[CurrentGearIndex] == 0? 0: EngineRPM / AllGearsRatio[CurrentGearIndex];
                var offset = (400 / AllGearsRatio[CurrentGearIndex]).Abs();

                for (int i = 0; i < DriveWheels.Length; i++)
                {
                    var wheel = DriveWheels[i];
                    var wheelTorque = motorTorque;

                    //The torque transmitted to the wheels depends on the difference between the target RPM and the current RPM. 
                    //If the current RPM is greater than the target RPM, the wheel will brake. 
                    //If the current RPM is less than the target RPM, the wheel will accelerate.

                    if (targetWheelsRPM != 0 && Mathf.Sign (targetWheelsRPM * wheel.RPM) > 0)
                    {
                        var multiplier = wheel.RPM.Abs () / (targetWheelsRPM.Abs () + offset);
                        if (multiplier >= 1f)
                        {
                            wheelTorque *= (1 - multiplier);
                        }
                    }

                    //Apply of torque to the wheel.
                    wheel.SetMotorTorque (wheelTorque);
                }
            }
            else
            {
                for (int i = 0; i < DriveWheels.Length; i++)
                {
                    DriveWheels[i].SetMotorTorque (0);
                }
            }

            if (InChangeGear)
            {
                ChangeGearTimer -= Time.fixedDeltaTime;
            }

            //Automatic gearbox logic. 
            if (!InChangeGear && Gearbox.AutomaticGearBox)
            {

                bool forwardIsSlip = false;
                bool anyWheelIsGrounded = false;
                float avgSign = 0;
                for (int i = 0; i < DriveWheels.Length; i++)
                {
                    if (DriveWheels[i].CurrentForwardSlip > Gearbox.MaxForwardSlipToBlockChangeGear)
                    {
                        forwardIsSlip |= true;
                    }
                    anyWheelIsGrounded |= DriveWheels[i].IsGrounded;
                    avgSign += DriveWheels[i].RPM;
                }

                avgSign = Mathf.Sign (avgSign);

                if (anyWheelIsGrounded && !forwardIsSlip && EngineRPM > Engine.RPMToNextGear && CurrentGear >= 0 && CurrentGear < (AllGearsRatio.Length - 2))
                {
                    NextGear ();
                }
                else if (CurrentGear > 0 && (EngineRPM + 10 <= MinRPM || CurrentGear != 1) &&
                    Engine.RPMToNextGear > EngineRPM / AllGearsRatio[CurrentGearIndex] * AllGearsRatio[CurrentGearIndex - 1] + Engine.RPMToPrevGearDiff)
                {
                    PrevGear ();
                }

                //Old PrevGear condition
                //else if (EngineRPM < Engine.RPMToPrevGear && CurrentGear > 0 && (EngineRPM + 10 <= MinRPM || CurrentGear != 1))
                //{
                //    PrevGear ();
                //}

                //Switching logic from neutral gear.
                if (CurrentGear == 0 && CurrentBrake > 0)
                {
                    CurrentGear = -1;
                }
                else if (CurrentGear == 0 && CurrentAcceleration > 0)
                {
                    CurrentGear = 1;
                }
                else if ((avgSign > 0 && CurrentGear < 0 || VehicleDirection == 0) && Mathf.Approximately(CurrentAcceleration, 0))
                {
                    CurrentGear = 0;
                }
            }
        }

        public void NextGear ()
        {
            if (!InChangeGear && CurrentGear < (AllGearsRatio.Length - 2))
            {
                CurrentGear++;
                ChangeGearTimer = Gearbox.ChangeUpGearTime;
                PlayBackfireWithProbability ();
            }
        }

        public void PrevGear ()
        {
            if (!InChangeGear && CurrentGear >= 0)
            {
                CurrentGear--;
                ChangeGearTimer = Gearbox.ChangeDownGearTime;
            }
        }

        [System.Serializable]
        public class GearboxConfig
        {
            public float ChangeUpGearTime = 0.3f;                   //Delay after upshift.
            public float ChangeDownGearTime = 0.2f;                 //Delay after downshift.

            [Header("Automatic gearbox")]
            public bool AutomaticGearBox = true;
            public float MaxForwardSlipToBlockChangeGear = 0.5f;    //If any wheel slides more than this value, automatic gear changes will not occur.

            [Header("Ratio")]
            public float[] GearsRatio;                              //Gear ratio. The values ​​are best take from the technical data of real transmissions.
            public float MainRatio;
            public float ReversGearRatio;
        }
    }
}
