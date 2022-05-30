using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PG.GameBalance;

namespace PG
{
    /// <summary>
    /// Class for pursuit a rigid body.
    /// </summary>
    public class PursuitAIControl :BaseAIControl
    {
        public Rigidbody TargetRB;                      //Pursuit body, you can add initialization or rigid body detection logic.

        PursuitAIConfig PursuitAIConfig;

        LayerMask ObstacleHitMask { get { return PursuitAIConfig.ObstacleHitMask; } }
        float HitPointHeight { get { return PursuitAIConfig.HitPointHeight; } }
        float VisibilityArea { get { return PursuitAIConfig.VisibilityArea; } }
        float VisibilityAreaIgnoreObstacle { get { return PursuitAIConfig.VisibilityAreaIgnoreObstacle; } }
        float HitDellayTime { get { return PursuitAIConfig.HitDellayTime; } }

        Vector3 TargetPoint;
        Vector3 TurnPredictionPoint;
        float VisibilityAreaIgnoreObstacleSqr;          //Square of the visibility area without using the ray.

        bool Reverse;
        float ReverseTimer = 0;
        float PrevSpeed = 0;
        float LastReverceTime;

        float LastHitTime;
        float StartHitPointDistance;
        RaycastHit Hit;

        bool InPursuit;                                 //Pursuit enable flag. You can also change the logic for switching this flag to your taste.

        public override void Start ()
        {
            base.Start ();

            var selectedRaceAsset =  AIConfigAsset as PursuitAIConfigAsset;

            if (selectedRaceAsset)
            {
                PursuitAIConfig = selectedRaceAsset.PursuitAIConfig;
            }
            else
            {
                PursuitAIConfig = new PursuitAIConfig ();
            }

            VisibilityAreaIgnoreObstacleSqr = Mathf.Pow(VisibilityAreaIgnoreObstacle, 2);

            //Finding the maximum length of a car. To prevent the ray from getting into yourself.
            StartHitPointDistance = Mathf.Max (Car.Bounds.size.x, Car.Bounds.size.y, Car.Bounds.size.z) * 0.5f + 0.1f;
        }

        protected override void FixedUpdate ()
        {
            if (Reverse)
            {
                ReverseMove ();
            }
            else
            {
                ForwardMove ();
                UpdateHit ();
            }
        }

        /// <summary>
        /// All behavior of AI is defined in this method.
        /// Target point is the position of the tracked body + OffsetToTargetPoint (To be able to predict the position of the tracked body).
        /// The acceleration is calculated from the angle of the car to the TurnPredictionPoint.
        /// </summary>
        private void ForwardMove ()
        {
            if (InPursuit)
            {
                HandBrake = false;

                var dir = TargetRB.velocity;

                TargetPoint = TargetRB.position + dir * OffsetToTargetPoint + dir * SpeedFactorToTargetPoint;
                TurnPredictionPoint = TargetRB.position + dir * OffsetTurnPrediction + dir * SpeedFactorToTurnPrediction;

                var angleToPredictionPoint = Vector3.SignedAngle (Vector3.forward,
                                                            transform.InverseTransformPoint ((TurnPredictionPoint)).ZeroHeight(),
                                                            Vector3.up).Abs();

                float desiredSpeed = (1 - (angleToPredictionPoint / LookAngleSppedFactor)).AbsClamp ();
                desiredSpeed = desiredSpeed * (MaxSpeed - MinSpeed) + MinSpeed;
                desiredSpeed = desiredSpeed.Clamp (MinSpeed, MaxSpeed);

                //If the target body is behind, then the minimum speed for turning is set.
                if (Vector3.Dot (Vector3.forward, transform.InverseTransformPoint(TargetPoint)) < 0)
                {
                    desiredSpeed = MinSpeed;
                }

                // Acceleration and brake logic
                Vertical = ((desiredSpeed / Car.CurrentSpeed - 1)).Clamp (-1, 1);

                //Steer angle logic
                var angleToTargetPoint = Vector3.SignedAngle (Vector3.forward,
                                                            transform.InverseTransformPoint (TargetPoint).ZeroHeight(),
                                                            Vector3.up);

                Horizontal = (angleToTargetPoint / Car.Steer.MaxSteerAngle * SetSteerAngleMultiplayer).Clamp (-1, 1);


                //Reverse logic
                var deltaSpeed = Mathf.Abs (Car.CurrentSpeed - PrevSpeed);
                if (Vertical > 0.1f && deltaSpeed < 1 && Car.CurrentSpeed < 10)
                {
                    if (ReverseTimer < ReverceWaitTime)
                    {
                        ReverseTimer += Time.fixedDeltaTime;
                    }
                    else if (Time.time - LastReverceTime <= BetweenReverceTimeForReset)
                    {
                        Horizontal = 0;
                        Vertical = 0;
                        Car.ResetVehicle ();
                        ReverseTimer = 0;
                    }
                    else
                    {
                        Horizontal = -Horizontal;
                        Vertical = -Vertical;
                        ReverseTimer = 0;
                        Reverse = true;
                    }
                }
                else
                {
                    ReverseTimer = 0;
                }
            }
            else
            {
                Horizontal = 0;
                Vertical = 0;
                HandBrake = true;
            }
        }

        private void ReverseMove ()
        {
            if (ReverseTimer < ReverceTime)
            {
                ReverseTimer += Time.fixedDeltaTime;
            }
            else
            {
                LastReverceTime = Time.time;
                ReverseTimer = 0;
                Reverse = false;
            }
        }

        /// <summary>
        /// Checking the location of the tracked body in the VisibilityArea.
        /// Here you can change the detection logic as needed.
        /// </summary>
        void UpdateHit ()
        {
            //If pursuit is enabled, then no checking is required.
            if (InPursuit)
            {
                return;
            }

            //If the pursued car is too close, the pursuit is activated ignoring the line of sight.
            if ((transform.position - TargetRB.position).sqrMagnitude <= VisibilityAreaIgnoreObstacleSqr)
            {
                InPursuit = true;
                return;
            }

            if (Time.time - LastHitTime < HitDellayTime)
            {
                return;
            }

            LastHitTime = Time.time;

            //Ray point and direction calculation.
            var direction = (TargetRB.position - transform.position).normalized;
            var position = transform.position + direction * StartHitPointDistance;
            position.y += HitPointHeight;

            Physics.Raycast (position, direction, out Hit, VisibilityArea, ObstacleHitMask);
            InPursuit = (InPursuit || Vector3.Dot (direction, transform.forward) > 0) && Hit.rigidbody == TargetRB;
        }

        private void OnDrawGizmosSelected ()
        {
            if (Application.isPlaying && this.enabled)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine (transform.position, TargetPoint);
                Gizmos.DrawWireSphere (TargetPoint, 0.5f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere (TargetPoint, 0.5f);
                Gizmos.DrawWireSphere (TurnPredictionPoint, 0.5f);
            }
        }
    }

    [System.Serializable]
    public class PursuitAIConfig
    {
        public LayerMask ObstacleHitMask = 1 << 0 | 1 << 8 | 1 << 10 | 1 << 11 | 1 << 12 | 1 << 13 | 1 << 15 | 1 << 17; //Ray mask, to determine obstacles to the tracked body, you may have different layer numbers.
        public float HitPointHeight = 0.5f;
        public float VisibilityArea = 150;                      //Visibility area in front of the car, in the radius of which the ray is launched.
        public float VisibilityAreaIgnoreObstacle = 30;         //Visibility around the car, ignoring obstacles.
        public float HitDellayTime = 5;                         //Check interval for optimization.
    }
}
