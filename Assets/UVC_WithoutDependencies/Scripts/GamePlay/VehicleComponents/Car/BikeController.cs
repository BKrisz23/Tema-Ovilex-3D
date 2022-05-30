using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class BikeController :CarController
    {
        [Header ("Bike Settings")]
        public BikeConfig Bike;

        //The parents of the moving parts of the bike.
        public Transform Handlebar;
        public Transform FrontFork;
        public Transform RearForkParent;
        public Transform RearFork;

        public Transform FrontComPosition;  //the point to which COM moves with positive pitch.
        public Transform RearComPosition;   //the point to which COM moves with negative pitch.

        public event System.Action OnCrashAction;   //Action executed in case of a crash.

        public bool InCrash { get; private set; }

        public Wheel FrontWheel => Wheels[0];
        public Wheel RearWheel => Wheels[1];

        Quaternion HandlebarAwakeRotation;
        Vector3 FrontForkAwakePosition;
        Quaternion RearForkAwakeLockRotation;

        Vector3 PrevVelocity;           //To calculate g-force.

        float AdditionalPitchAngular;   //For rotate the bike in air.
        float AdditionalYawAngular;     //For rotate the bike in air.

        protected override void Awake ()
        {
            base.Awake ();

            if (RearForkParent == null)
            {
                RearForkParent = new GameObject (RearFork.name + "_Parent").transform;
                RearForkParent.SetParent (RearFork.parent);
                RearForkParent.localPosition = RearFork.localPosition;
                RearForkParent.localRotation = Quaternion.identity;
                RearFork.SetParent (RearForkParent);
            }

            HandlebarAwakeRotation = Handlebar.localRotation;
            FrontForkAwakePosition = FrontFork.localPosition;
            RearForkAwakeLockRotation = Quaternion.LookRotation (-RearFork.InverseTransformPoint (RearWheel.transform.position));
            var eulerAngles = RearForkAwakeLockRotation.eulerAngles;
            eulerAngles.x = -eulerAngles.x;
            RearForkAwakeLockRotation.eulerAngles = eulerAngles;
        }

        protected override void FixedUpdate ()
        {
            base.FixedUpdate ();

            var currentVelocity = transform.InverseTransformDirection(RB.velocity);

            var gForce = PrevVelocity - currentVelocity;

            PrevVelocity = currentVelocity;

            //Roll balance is calculated if the bike is not in a crash.
            if (!InCrash && !FrontWheel.IsDead && !RearWheel.IsDead)
            {
                //Crash condition.
                if (gForce.sqrMagnitude > Bike.MaxSqrGForceForCrash || currentVelocity.z < -Bike.MaxReverseSpeedForCrash && VehicleIsGrounded)
                {
                    InCrash = true;
                    OnCrashAction.SafeInvoke ();
                    BlockControl = true;
                    RB.centerOfMass = COM.localPosition;
                    return;
                }

                //Current roll.
                var fwd = transform.forward;
                fwd.y = 0;
                fwd *= Mathf.Sign (transform.up.y);
                var right = Vector3.Cross(Vector3.up, fwd).normalized;
                float roll = Vector3.Angle(right, transform.right) * Mathf.Sign(transform.right.y);

                float targetRoll = Bike.SpeedRollAngle.Evaluate(
                    CurrentSpeed *
                    (
                        CurrentSteerAngle / (Steer.EnableSteerLimit? Steer.SteerLimitCurve.Evaluate(CurrentSpeed): 1) / Steer.MaxSteerAngle
                    )
                );

                if (CarControl != null)
                {
                    //Yaw changing. Depends on the front wheel.
                    if (!FrontWheel.IsGrounded)
                    {
                        AdditionalYawAngular = Bike.MaxYawAngularInAir * CarControl.Horizontal;
                    }
                    else
                    {
                        AdditionalYawAngular = 0;
                    }
                    if (VehicleIsGrounded)
                    {
                        //COM position changing on Pitch change, if VehicleIsGrounded.
                        float distanceBetweenWheels = ((FrontWheel.transform.position.y - RearWheel.transform.position.y) / Bike.MaxHeightDiffWheelsForChangeCom);
                        if (CarControl.Pitch > 0)
                        {
                            RB.centerOfMass = Vector3.Lerp(FrontComPosition.localPosition, COM.localPosition, (-distanceBetweenWheels).Clamp ());
                        }
                        else if (CarControl.Pitch < 0)
                        {
                            RB.centerOfMass = Vector3.Lerp (RearComPosition.localPosition, COM.localPosition, distanceBetweenWheels.Clamp ());
                        }
                        else
                        {
                            RB.centerOfMass = COM.localPosition;
                        }

                        AdditionalPitchAngular = 0;
                    }
                    else
                    {
                        //AngularVelocity.x changing on Pitch change, if !VehicleIsGrounded.
                        AdditionalPitchAngular = Bike.MaxPitchAngularInAir * CarControl.Pitch;
                    }
                }


                //Reverse logic.
                if (CurrentGear == -1)
                {
                    if (RearWheel.RPM == 0)
                    {
                        RearWheel.SetMotorTorque (-0.001f);
                    }
                    else
                    {
                        RearWheel.SetMotorTorque (0);
                    }
                    currentVelocity.z = Mathf.MoveTowards (currentVelocity.z, CarControl.BrakeReverse * -Bike.TargetReverseSpeed, Time.fixedDeltaTime * 5);
                    currentVelocity = transform.TransformDirection (currentVelocity);
                    RB.velocity = currentVelocity;
                }

                //Applying calculated AngularVelocity
                var clampValue = Bike.RollAngualrLimit.Evaluate ((targetRoll - roll).Abs());
                Vector3 targetAngularVelocity = transform.InverseTransformDirection (RB.angularVelocity);
                targetAngularVelocity.x += AdditionalPitchAngular;
                targetAngularVelocity.z = (targetRoll - roll).Clamp(-clampValue, clampValue);
                targetAngularVelocity = transform.TransformDirection (targetAngularVelocity);
                targetAngularVelocity.y += AdditionalYawAngular;

                RB.angularVelocity = targetAngularVelocity;
            }
        }

        protected override void LateUpdate ()
        {
            //Calculation of visually moving parts.
            if (VehicleIsVisible)
            {
                Handlebar.localRotation = HandlebarAwakeRotation * Quaternion.AngleAxis (CurrentSteerAngle, Vector3.up);

                if (!FrontWheel.IsDead)
                {
                    var frontForkOffset = Wheels[0].transform.InverseTransformPoint (FrontWheel.Position);
                    FrontFork.localPosition = Vector3.Lerp (FrontFork.localPosition, FrontForkAwakePosition + frontForkOffset, 0.5f);
                }

                if (!RearWheel.IsDead)
                {
                    var rearForkLookPoint = RearForkParent.InverseTransformPoint(RearWheel.Position);
                    RearFork.localRotation = Quaternion.Lerp (RearFork.localRotation, RearForkAwakeLockRotation * Quaternion.LookRotation (-rearForkLookPoint, Vector3.up), 0.5f);
                }
            }
        }


        public override void ResetVehicle ()
        {
            base.ResetVehicle ();
            InCrash = false;
            BlockControl = false;
            PrevVelocity = Vector3.zero;
        }

        [System.Serializable]
        public class BikeConfig
        {
            public AnimationCurve SpeedRollAngle;               //The roll angle of the bike, depends on the speed.
            public AnimationCurve RollAngualrLimit;             //AngularVelocity roll limit, depends on the difference between the current roll and the target roll.

            public float MaxHeightDiffWheelsForChangeCom = 1;   //To change the Center of Mass, If the difference between the wheel heights == 0, then the center of mass will change as much as possible. 
                                                                //If the difference is greater than this value, then the center of mass will be in the center.
            public float MaxPitchAngularInAir = 0.02f;          //The speed of pitch change if the bike is in the air.
            public float MaxYawAngularInAir = 0.02f;            //The speed of Yaw change if the bike is in the air.

            public float MaxReverseSpeedForCrash = 5;           //Reverse speed at which the bike will crash.
            public float MaxSqrGForceForCrash = 100;            //sqr of G-force, с которой велосипед разобьется

            public float TargetReverseSpeed = 3;                //Reverse force is applied to RigidBody, not to wheels.
        }
    }
}
