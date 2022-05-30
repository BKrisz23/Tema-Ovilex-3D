using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG 
{
    /// <summary>
    /// For AI with track positioning functions (for Race or Drift mode).
    /// </summary>
    [RequireComponent (typeof(CarController))]
    public class PositioningAIControl :BaseAIControl
    {
        public AIPath AIPath;                                           //The path to which the AI is tied.

        protected float SpeedLimit;                                     //Current speed limit.

        public float ProgressDistance { get; set; }                     //Distance of progress along the AIPath
        public AIPath.RoutePoint ProgressPoint { get; private set; }

        /// <summary>
        /// If the path is not looped, then the property returns true when the end of the path is reached.
        /// </summary>
        public bool Finished 
        { 
            get 
            {
                return !AIPath.LoopedPath && ProgressDistance >= AIPath.Length;
            } 
        }

        public override void Start ()
        {
            //If path is null, then the first path of the scene is taken.
            if (!AIPath)
            {
                AIPath = AIPath.FirstPath;
            }

            if (!AIPath)
            {
                Debug.LogError ("AIPath not found");
                enabled = false;
                return;
            }

            if (!AIConfigAsset && !AIPath.AIConfigAsset)
            {
                Debug.LogError ("AIConfig not found");
                enabled = false;
                return;
            }

            if (!AIConfigAsset)
            {
                AIConfigAsset = AIPath.AIConfigAsset;
            }

            BaseAIConfig = AIConfigAsset.AIConfig;

            ProgressPoint = AIPath.GetRoutePoint (0);
            Car = GetComponent<CarController> ();
            Car.CarControl = this;

            //Finding the closest waypoint at the start.
            float minProgress = 0;
            float curProgress = 0;
            float minDist = (AIPath.GetRoutePoint (0).Position - transform.position).sqrMagnitude;
            float curDist;
            while (curProgress < AIPath.Length)
            {
                curProgress += 0.5f;
                curDist = (AIPath.GetRoutePoint (curProgress).Position - transform.position).sqrMagnitude;
                if (curDist < minDist)
                {
                    minDist = curDist;
                    minProgress = curProgress;
                }
            }
            ProgressDistance = minProgress;
            ProgressPoint = AIPath.GetRoutePoint (ProgressDistance);
        }

        protected override void FixedUpdate ()
        {
            //Determination of the current progress distance on the way.

            Vector3 progressDelta = ProgressPoint.Position - transform.position;
            float dotProgressDelta = Vector3.Dot (progressDelta, ProgressPoint.Direction);

            if (dotProgressDelta < 0)
            {
                //Forward move direction logic
                while (dotProgressDelta < 0)
                {
                    ProgressDistance += Mathf.Max (0.5f, Car.CurrentSpeed * Time.fixedDeltaTime);
                    ProgressPoint = AIPath.GetRoutePoint (ProgressDistance);
                    progressDelta = ProgressPoint.Position - transform.position;
                    dotProgressDelta = Vector3.Dot (progressDelta, ProgressPoint.Direction);
                }
            }
            else if (ProgressDistance > 0 && progressDelta.sqrMagnitude < 0)
            {
                //Wrog move direction logic
                dotProgressDelta = Vector3.Dot (progressDelta, -ProgressPoint.Direction);

                if (dotProgressDelta < 0f)
                {
                    ProgressDistance -= progressDelta.magnitude * 0.5f;
                    ProgressPoint = AIPath.GetRoutePoint (ProgressDistance);
                }
            }

            SpeedLimit = ProgressPoint.SpeedLimit;
        }
    }
}
