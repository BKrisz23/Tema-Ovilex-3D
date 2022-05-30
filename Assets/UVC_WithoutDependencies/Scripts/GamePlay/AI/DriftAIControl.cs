using PG.GameBalance;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PG
{

    /// <summary>
    /// AI for drift mode.
    /// </summary>
    public class DriftAIControl :PositioningAIControl
    {
        DriftAIConfig DriftAIConfig;

        float ObstacleHitDistance { get { return DriftAIConfig.ObstacleHitDistance; } }
        float HitPointHeight { get { return DriftAIConfig.HitPointHeight; } }
        LayerMask ObstacleHitMask { get { return DriftAIConfig.ObstacleHitMask; } }
        float HitDellayTime { get { return DriftAIConfig.HitDellayTime; } }

        bool Reverse;
        float ReverseTimer = 0;
        float PrevSpeed = 0;
        float LastReverceTime;

        public Vector3 TargetPointResult { get; private set; }
        public AIPath.RoutePoint TargetPoint { get; private set; }
        public AIPath.RoutePoint TurnPredictionPoint { get; private set; }

        private Rigidbody AheadRB;
        private float DistanceToAheadRB;

        public override void Start ()
        {
            base.Start ();

            var selectedDriftAsset =  AIConfigAsset as DriftAIConfigAsset;

            if (selectedDriftAsset)
            {
                DriftAIConfig = selectedDriftAsset.DriftAIConfig;
            }
            else
            {
                DriftAIConfig = new DriftAIConfig ();
            }

            StartHits ();
        }

        protected override void FixedUpdate ()
        {
            if (Finished)
            {
                HandBrake = true;
                Horizontal = 0;
                Vertical = 0;
                return;
            }

            base.FixedUpdate ();

            if (Reverse)
            {
                ReverseMove ();
            }
            else
            {
                ForwardMove ();
                UpdateMainHit ();
            }
        }

        /// <summary>
        /// All behavior of AI is defined in this method.
        /// The TargetPointResult is the point between two points (TargetPoint and TurnPredictionPoint) calculated from the parameters from AIConfig.
        /// The acceleration is calculated from the angle of the car to the target point.
        /// For a detailed description of the algorithm, see the documentation.
        /// </summary>
        private void ForwardMove ()
        {
            TargetPoint = AIPath.GetRoutePoint (ProgressDistance + OffsetToTargetPoint + (SpeedFactorToTargetPoint * Car.CurrentSpeed));
            TurnPredictionPoint = AIPath.GetRoutePoint (ProgressDistance + OffsetTurnPrediction + (SpeedFactorToTurnPrediction * Car.CurrentSpeed));
            TargetPointResult = (TargetPoint.Position + TurnPredictionPoint.Position) * 0.5f;

            var angleToTargetPoint = Vector3.SignedAngle (Vector3.forward,
                                                    transform.InverseTransformPoint (TargetPointResult).ZeroHeight(),
                                                    Vector3.up);

            var angleToTargetPointABS = angleToTargetPoint.Abs();

            float desiredSpeed;

            desiredSpeed = (1 - (angleToTargetPointABS / LookAngleSppedFactor)).AbsClamp ();
            desiredSpeed = desiredSpeed * (MaxSpeed - MinSpeed) + MinSpeed;
            desiredSpeed = desiredSpeed.Clamp (MinSpeed, MaxSpeed);

            if (AheadRB)
            {
                //If the car in front is close, then the speed of the followed car is taken as the desired speed.
                float aheadRBSpeed = AheadRB.velocity.magnitude;
                desiredSpeed = Mathf.Min (desiredSpeed, Mathf.Lerp (aheadRBSpeed, desiredSpeed, DistanceToAheadRB / ObstacleHitDistance));
            }

            desiredSpeed = Mathf.Min (desiredSpeed, SpeedLimit);

            var vertical = ((desiredSpeed / Car.CurrentSpeed - 1));


            //If the angle to the angleToTargetPoint is too large, then the handbrake is turned on to skid.
            if (angleToTargetPointABS > LookAngleSppedFactor * 0.5f)
            {
                HandBrake = vertical < -0.2f;
                vertical = vertical.Clamp ();
            }
            else
            {
                vertical = vertical.Clamp (-1, 1);
                HandBrake = false;
            }

            Vertical = vertical;

            //Steer angle logic
            Horizontal = (angleToTargetPoint / Car.Steer.MaxSteerAngle * SetSteerAngleMultiplayer).Clamp (-1, 1);


            //Reverse logic. if the car does not move forward (stuck against the collider or stuck), then reverse gear is activated.
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
                    Vertical = -1;
                    ReverseTimer = 0;
                    Reverse = true;
                }
            }
            else
            {
                ReverseTimer = 0;
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

        #region Hits

        RaycastHit MainHit;

        float LastMainHitTime;
        float StartPointDistance;

        void StartHits ()
        {
            //Finding the maximum length of a car. To prevent the ray from getting into yourself.
            StartPointDistance = Mathf.Max (Car.Bounds.size.x, Car.Bounds.size.y, Car.Bounds.size.z) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Check on the car ahead to prevent ramming.
        /// </summary>
        void UpdateMainHit ()
        {
            if (Time.time - LastMainHitTime < HitDellayTime)
            {
                return;
            }

            AheadRB = null;
            LastMainHitTime = Time.time;
            DistanceToAheadRB = float.MaxValue;

            var direction = (transform.forward + Car.RB.velocity.normalized) * 0.5f;
            var position = transform.TransformPoint(new Vector3 (0, HitPointHeight, 0)) + direction * StartPointDistance;

            if (Physics.Raycast (position, direction, out MainHit, ObstacleHitDistance, ObstacleHitMask))
            {
                DistanceToAheadRB = MainHit.distance;
                AheadRB = MainHit.rigidbody;
            }
        }

        #endregion //Hits

        private void OnDrawGizmosSelected ()
        {
            if (Application.isPlaying && this.enabled)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine (transform.position, TargetPointResult);
                Gizmos.DrawWireSphere (TargetPointResult, 0.5f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere (TargetPoint.Position, 0.5f);
                Gizmos.DrawWireSphere (TurnPredictionPoint.Position, 0.5f);

                var direction = (transform.forward + Car.RB.velocity.normalized) * 0.5f;
                var position = transform.TransformPoint(new Vector3 (0, HitPointHeight, 0)) + direction * StartPointDistance;

                Gizmos.color = MainHit.collider != null ? Color.red : Color.blue;
                Gizmos.DrawLine (position, position + direction * ObstacleHitDistance);
            }
        }
    }

    [System.Serializable]
    public class DriftAIConfig
    {
        public float ObstacleHitDistance = 15f;         //The distance to be checked from the car in front.
        public float HitPointHeight = 0.5f;             
        public LayerMask ObstacleHitMask = 1 << 8;      //Mask for checking the car in front, by default only layer 8 (vehicle), your layer number may differ.
        public float HitDellayTime = 0.5f;              //Check interval for optimization.
    }
}

