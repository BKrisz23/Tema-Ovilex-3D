using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{

    //Engine logic, current rpm, engine load, braking, etc.
    public partial class CarController :VehicleController
    {
        public EngineConfig Engine;
        public DamageableObject EngineDamageableObject;

        public float CurrentMotorTorque 
        { 
            get
            { 
                return 
                    Engine.MotorTorqueFromRpmCurve.Evaluate (EngineRPM * 0.001f) * 
                    (1 + CurrentTurbo * Engine.TurboAdditionalTorque) *
                    (InBoost? 1 + Engine.BoostAdditionalPower: 1); 
            } 
        }

        public float EngineRPM { get; private set; }            //Current RPM.
        public float TargetRPM { get; private set; }            //TargetRPM Calculated based on the drive wheel rpm and the current gear ratio
        public float EngineLoad { get; private set; }           //Current Load

        public float MaxRPM { get { return Engine.MaxRPM; } }
        public float MinRPM { get { return Engine.MinRPM; } }

        public event System.Action BackFireAction;              //TODO Add BackFire vfx and sfx.

        float MaxMotorTorque;
        float CutOffTimer;
        bool InCutOff;

        public float CurrentAcceleration { get; private set; }
        public float CurrentTurbo { get; private set; }
        public bool InHandBrake { get { return CarControl != null && CarControl.HandBrake && !BlockControl; } }
        public float CurrentBrake { get; private set; }
        public float BoostAmount { get; private set; }
        public bool InBoost { get; private set; }
        public float EngineHealth { get { return EngineDamageableObject ? EngineDamageableObject.HealthPercent : 1; } }

        private void AwakeEngine ()
        {
            MaxMotorTorque = Engine.MaxMotorTorque / DriveWheels.Length;    //Division of torque to all driving wheels.
            BoostAmount = Engine.BoostAmount;
        }

        void FixedUpdateEngine ()
        {
            //Acceleration control logic. 
            //If the automatic transmission is turned on, the gear is in reverse and the brake/reverse button is pressed, the car will drive in reverse and vice versa. 
            //If the automatic transmission is turned off, then to drive back you need to select the reverse gear and press the acceleration button.
            if (CarControl == null || BlockControl)
            {
                CurrentAcceleration = 0;
                CurrentBrake = 0;
            }
            else if (!Gearbox.AutomaticGearBox || CurrentGear >= 0)
            {
                CurrentAcceleration = CarControl.Acceleration;
                CurrentBrake = CarControl.BrakeReverse;
            }
            else if (CurrentGear < 0)
            {
                CurrentAcceleration = CarControl.BrakeReverse;
                CurrentBrake = CarControl.Acceleration;
            }

            CurrentAcceleration *= EngineHealth;

            //CutOff timer.
            if (InCutOff)
            {
                if (CutOffTimer > 0)
                {
                    CutOffTimer -= Time.fixedDeltaTime;
                    EngineRPM = Mathf.Lerp (EngineRPM, Engine.TargetCutOffRPM, Engine.RPMEngineToRPMWheelsFast * Time.fixedDeltaTime);
                }
                else
                {
                    EngineRPM = Engine.TargetCutOffRPM;
                    InCutOff = false;
                }
            }

            //Calculation of the average rpm of all driving wheels.
            float avgRPM = 0;
            int enabledWheelsCount = 0;
            for (int i = 0; i < DriveWheels.Length; i++)
            {
                if (DriveWheels[i].enabled)
                {
                    avgRPM += DriveWheels[i].RPM;
                    enabledWheelsCount++;
                }
            }

            if (enabledWheelsCount > 0)
            {
                avgRPM /= enabledWheelsCount;
            }
            else
            {
                avgRPM = Engine.MinRPM;
            }
            

            EngineLoad = 0;

            if (!InCutOff)
            {
                //Calculation of the current engine rpm.
                TargetRPM = (avgRPM * CurrentGear) <= 0 && !InHandBrake ? ((EngineRPM + 1000) * CurrentAcceleration) : (avgRPM.Abs () * AllGearsRatio[CurrentGearIndex].Abs ());
                TargetRPM = TargetRPM.Clamp(MinRPM, MaxRPM);
                var changeRPMSpeed = CurrentAcceleration.Abs() > 0.1f && TargetRPM > EngineRPM? Engine.RPMEngineToRPMWheelsFast: Engine.RPMEngineToRPMWheelsSlow;

                //Calculation of the current engine load.
                EngineLoad = (TargetRPM - EngineRPM).Clamp (-300, 300) / 300 * CurrentAcceleration;

                EngineRPM = Mathf.Lerp (EngineRPM, TargetRPM, changeRPMSpeed * Time.fixedDeltaTime);
            }

            //Check CutOff.
            if (EngineRPM >= Engine.CutOffRPM)
            {
                PlayBackfireWithProbability ();
                InCutOff = true;
                CutOffTimer = Engine.CutOffTime;
            }


            //Turbo logic. The speed and power of the turbo depends on the EnigneRPM and the Acceleration value.
            if (Engine.EnableTurbo)
            {
                float rpmToCutOffNormolize = (EngineRPM / Engine.CutOffRPM).Clamp ();
                float targetTurbo = InChangeGear || CurrentAcceleration < 0.2f? 0: rpmToCutOffNormolize;
                CurrentTurbo = Mathf.Lerp (
                    CurrentTurbo, 
                    targetTurbo, 
                    (targetTurbo > CurrentTurbo ? Engine.TurboIncreaseSpeed * rpmToCutOffNormolize : Engine.TurboDecreaseSpeed) * Time.fixedDeltaTime); 
            }

            //Boost logic
            InBoost = Engine.EnableBoost && CarControl != null && CarControl.Boost && BoostAmount > 0 && !BlockControl;

            if (InBoost)
            {
                BoostAmount = Mathf.Max (0, BoostAmount - Time.deltaTime);
            }
        }

        /// <summary>
        /// Braking logic.
        /// </summary>
        void FixedUpdateBrakeLogic ()
        {
            //HandBrake
            if (InHandBrake)
            {
                for (int i = 0; i < Wheels.Length; i++)
                {
                    Wheels[i].SetHandBrake (true);
                }
            }
            //Brake and acceleration pressed at the same time for burnout.
            else if (CurrentAcceleration > 0 && CurrentBrake > 0)
            {
                for (int i = 0; i < Wheels.Length; i++)
                {
                    Wheels[i].SetBrakeTorque(Wheels[i].DriveWheel? 0: CurrentBrake);
                }
            }
            //Just braking.
            else
            {
                for (int i = 0; i < Wheels.Length; i++)
                {
                    Wheels[i].SetBrakeTorque(CurrentBrake);
                }
            }
        }

        //To BackFire vfx and sfx.
        void PlayBackfireWithProbability ()
        {
            PlayBackfireWithProbability (Engine.ProbabilityBackfire);
        }

        void PlayBackfireWithProbability (float probability)
        {
            if (Random.Range (0f, 1f) <= probability)
            {
                BackFireAction.SafeInvoke ();
            }
        }


        [System.Serializable]
        public class EngineConfig
        {
            [Header("Power")]
            public float MaxMotorTorque = 150;                  //Maximum torque, reached at 1 value(y) of MotorTorqueFromRpmCurve.
            public AnimationCurve MotorTorqueFromRpmCurve;
            public float MaxRPM = 7000;
            public float MinRPM = 700;
            public float RPMEngineToRPMWheelsFast = 15;         //Rpm change with increasing speed.
            public float RPMEngineToRPMWheelsSlow = 4;          //Rpm change with decreasing speed.

            [Header("Cut off")]
            public float CutOffRPM = 6800;                      //The rpm at which the cut-off is triggered.
            public float TargetCutOffRPM = 6400;                //The value to which the rpm fall.
            public float CutOffTime = 0.1f;                     //The time for which the rpm fall during the cut-off.

            [Header("Turbo")]
            public bool EnableTurbo = false;                                                //Enables / Disables the turbo of the car.
            [ShowInInspectorIf("EnableTurbo")] public float TurboIncreaseSpeed = 3f;        //The speed at which the turbo value increases.
            [ShowInInspectorIf("EnableTurbo")] public float TurboDecreaseSpeed = 10;        //The speed at which the turbo value decreases.
            [ShowInInspectorIf("EnableTurbo")] public float TurboAdditionalTorque = 0.2f;   //Additional engine power multiplier at maximum turbo.

            [Header("Boost")]
            public bool EnableBoost = false;                                                //Enables / Disables boost for the car.
            [ShowInInspectorIf("EnableBoost")] public float BoostAmount = 2;                //The amount of boost, measured in seconds.
            [ShowInInspectorIf("EnableBoost")] public float BoostAdditionalPower = 1;       //Additional engine power multiplier at maximum when using boost.

            [Header("Back fire")]
            [Range(0, 1)]public float ProbabilityBackfire = 0.2f;

            //Automatic gear shifting is in EngineConfig because the maximum number of rpm can be different for the motors, and the gearbox can be the same.
            [Header("Automatic change gear")]
            public float RPMToNextGear = 6500;
            public float RPMToPrevGearDiff = 500;
        }
    }
}
