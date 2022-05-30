using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PG
{
    // One of the most important parts of the component, it is responsible for management and management assistance.
    public partial class CarController :VehicleController
    {
        public SteerConfig Steer;

        protected float PrevSteerAngle;
        protected float CurrentSteerAngle;
        protected float WheelMaxSteerAngle;
        protected Wheel[] SteeringWheels;
        protected float HorizontalControl { get { return CarControl != null && !BlockControl ? CarControl.Horizontal : 0; } }

        void AwakeSteering ()
        {
            var steeringWheels = new List<Wheel>();
            foreach (var wheel in Wheels)
            {
                if (wheel.IsSteeringWheel)
                {
                    steeringWheels.Add (wheel);
                    if (wheel.SteerPercent.Abs() > WheelMaxSteerAngle)
                    {
                        WheelMaxSteerAngle = wheel.SteerPercent.Abs();
                    }
                }
            }
            SteeringWheels = steeringWheels.ToArray ();
        }

        /// <summary>
        /// Update all helpers logic.
        /// </summary>
        void FixedUpdateSteering ()
        {
            var needHelp = VelocityAngle.Abs() > 0.001f && VelocityAngle.Abs() < Steer.MaxVelocityAngleForHelp && CurrentSpeed > Steer.MinSpeedForHelp && CurrentGear > 0;
            float helpAngle = 0;

            if (needHelp)
            {
                for (int i = 0; i < SteeringWheels.Length; i++)
                {
                    if (Wheels[i].IsGrounded)
                    {
                        HelpAngularVelocity ();
                        break;
                    }
                }

                helpAngle = Mathf.Clamp (VelocityAngle * Steer.HelpDriftIntensity, -Steer.MaxSteerAngle, Steer.MaxSteerAngle);
            }
            else if (CurrentSpeed < Steer.MinSpeedForHelp && CurrentAcceleration > 0 && CurrentBrake > 0)
            {
                var angularVelocity = RB.angularVelocity;
                angularVelocity.y = Steer.HandBrakeAngularHelpCurve.Evaluate (angularVelocity.y.Abs()) * HorizontalControl * 5;
                RB.angularVelocity = angularVelocity;
            }

            float targetSteerAngle = HorizontalControl * Steer.MaxSteerAngle;

            var steerMultiplayer = Steer.SteerLimitCurve.Evaluate (CurrentSpeed);

            targetSteerAngle *= Steer.EnableSteerLimit && VehicleDirection > 0 ? steerMultiplayer : 1;

            //Wheel turn limitation.
            var targetAngle = Mathf.Clamp (helpAngle + targetSteerAngle, -Steer.MaxSteerAngle, Steer.MaxSteerAngle);

            //Calculation of the steering speed. The steering wheel should turn faster towards the velocity angle.
            //More details (With images) are described in the documentation.
            float steerAngleChangeSpeed;

            float currentAngleDiff = (VelocityAngle - CurrentSteerAngle).Abs();

            if (!needHelp || PrevSteerAngle > CurrentSteerAngle && CurrentSteerAngle > VelocityAngle || PrevSteerAngle < CurrentSteerAngle && CurrentSteerAngle < VelocityAngle)
            {
                steerAngleChangeSpeed = Steer.SteerChangeSpeedToVelocity.Evaluate (currentAngleDiff);
            }
            else
            {
                steerAngleChangeSpeed = Steer.SteerChangeSpeedFromVelocity.Evaluate (currentAngleDiff);
            }

            PrevSteerAngle = CurrentSteerAngle;
            CurrentSteerAngle = Mathf.MoveTowards (CurrentSteerAngle, targetAngle, steerAngleChangeSpeed * steerMultiplayer * Time.fixedDeltaTime);

            //Apply a turn to the front wheels.
            for (int i = 0; i < SteeringWheels.Length; i++)
            {
                SteeringWheels[i].SetSteerAngle (CurrentSteerAngle);
            }
        }

        /// <summary>
        /// The method of turning the car in the steering direction. Gives driving a little arcade feel.
        /// </summary>
        void HelpAngularVelocity ()
        {
            var angularVelocity = RB.angularVelocity;

            float angularHelp = Steer.AngularHelperCurve.Evaluate(VelocityAngle.Abs()) * HorizontalControl;     //Calculation of the turning force.
            float intensity;

            //Calculation of the intensity of the turn. 
            //Depends on which way the steering wheel is turned, depending on the current angularVelocity.
            //More details (With images) are described in the documentation.
            if (HorizontalControl * RB.angularVelocity.y >= 0)
            {
                intensity = Steer.PositiveChangeIntensity.Evaluate (RB.angularVelocity.y.Abs ());
            }
            else
            {
                intensity = Steer.OppositeChangeIntensity.Evaluate (RB.angularVelocity.y.Abs ());
            }

            angularVelocity.y += angularHelp * intensity;

            if (InHandBrake)
            {
                angularHelp = Steer.HandBrakeAngularHelpCurve.Evaluate (angularVelocity.y * -Mathf.Sign(angularVelocity.y)) * HorizontalControl * (CurrentSpeed / 30).Clamp();
                angularVelocity.y += angularHelp;
            }

            RB.angularVelocity = angularVelocity;
        }

        [System.Serializable]
        public class SteerConfig
        {
            [Header("Steer settings")]
            public float MaxSteerAngle = 25;
            public bool EnableSteerLimit = true;                    //Enables limiting wheel turning based on car speed.
            public AnimationCurve SteerLimitCurve;                  //Limiting wheel turning if the EnableSteerLimit flag is enabled
            public AnimationCurve SteerChangeSpeedToVelocity;       //The speed of turn of the wheel in the direction of the velocity of the car.
            public AnimationCurve SteerChangeSpeedFromVelocity;     //The speed of turn of the wheel from the direction of the velocity of the car.

            [Header("Steer assistance")]
            public float MaxVelocityAngleForHelp = 120;             //The maximum degree of angle of the car relative to the velocity, at which the steering assistance will be provided.
            public float MinSpeedForHelp = 1.5f;

            [Space(10)]
            [Range(0, 1)] public float HelpDriftIntensity = 0.8f;   //The intensity of the automatic steering while drifting.

            [Header("Angular help")]
            public AnimationCurve HandBrakeAngularHelpCurve;               //The power of assistance that turns the car with the hand brake.
            public AnimationCurve AngularHelperCurve;               //The curve that determines the angular force of the car depends on the current angle of velocity.
            public AnimationCurve PositiveChangeIntensity;          //The multiplier of the change in angular velocity, if the Horizontal is directed towards the angular velocity.
            public AnimationCurve OppositeChangeIntensity;          //The multiplier of the change in angular velocity, if the Horizontal is directed from the angular velocity.
        }
    }
}
