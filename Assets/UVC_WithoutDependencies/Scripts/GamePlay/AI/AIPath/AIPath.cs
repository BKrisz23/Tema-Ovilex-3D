using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace PG
{
    /// <summary>
    /// The path for AI.
    /// The basis of the script is taken from the "Standard Assets" and slightly modified.
    /// </summary>

    public partial class AIPath: MonoBehaviour
    {

#pragma warning disable 0649

        public bool LoopedPath = true;
        public float MaxSpeedLimit = 150;
        public PG.GameBalance.BaseAIConfigAsset AIConfigAsset;

        [Header("Waypoints settings")]

        public List<WaypointData> Waypoints = new List<WaypointData>();

        public static List<AIPath> AIPaths = new List<AIPath>();
        public static AIPath FirstPath { get; private set; }

#pragma warning restore 0649

        private int PointsCount;
        private List<Vector3> Points;
        private List<float> Distances;

        public Transform GetLastPoint { get { return Waypoints[Waypoints.Count - 1].Point; } }

        public float Length { get; private set; }

        //this being here will save GC allocs
        private int P0n;
        private int P1n;
        private int P2n;
        private int P3n;

        private float I;
        private Vector3 P0;
        private Vector3 P1;
        private Vector3 P2;
        private Vector3 P3;

        private void Awake ()
        {
            AIPaths.RemoveAll (p => p == null);
            
            if (AIPaths.Count == 0)
            {
                FirstPath = this;
            }

            AIPaths.Add (this);

            if (Waypoints.Count > 1)
            {
                CachePositionsAndDistances ();
            }
            PointsCount = Waypoints.Count;

            for (int i =0; i < Waypoints.Count; i++)
            {
                var w = Waypoints[i];
                w.SpeedLimit = w.SpeedLimit.Clamp (0, MaxSpeedLimit);
                Waypoints[i] = w;
            }
        }

        public RoutePoint GetRoutePoint (float dist)
        {
            // position and direction
            float overtakeZoneLeft;
            float overtakeZoneRight;
            float speedLimit;
            float temp1;

            Vector3 p1 = GetRoutePosition (dist, out overtakeZoneLeft, out overtakeZoneRight, out speedLimit);
            Vector3 p2 = GetRoutePosition (dist + 0.1f, out temp1, out temp1, out temp1);
            Vector3 delta = p2 - p1;
            return new RoutePoint (p1, delta.normalized, overtakeZoneLeft, overtakeZoneRight, speedLimit);
        }


        public Vector3 GetRoutePosition (float dist, out float overtakeZoneLeft, out float overtakeZoneRight, out float speedLimit)
        {
            int point;

            dist = LoopedPath ? Mathf.Repeat (dist, Length) : Mathf.Min (dist, Length);

            for (point = 0; point < Distances.Count; point++)
            {
                if (Distances[point] > dist)
                {
                    break;
                }
            }

            // get nearest two points, ensuring points wrap-around start & end of circuit
            P1n = ((point - 1) + PointsCount) % PointsCount;
            P2n = point % PointsCount;

            // found point numbers, now find interpolation value between the two middle points

            I = Mathf.InverseLerp (Distances[P1n], Distances[point % Distances.Count], dist);

            // smooth catmull-rom calculation between the two relevant points

            //get the distance for overtaking
            overtakeZoneLeft = Mathf.Lerp (Waypoints[P1n].OvertakeZoneLeft, Waypoints[P2n].OvertakeZoneLeft, I);
            overtakeZoneRight = Mathf.Lerp (Waypoints[P1n].OvertakeZoneRight, Waypoints[P2n].OvertakeZoneRight, I);
            speedLimit = Mathf.Lerp (Waypoints[P1n].SpeedLimit, Waypoints[P2n].SpeedLimit, I);

            // get indices for the surrounding 2 points, because
            // four points are required by the catmull-rom function
            if (LoopedPath)
            {
                P0n = ((point - 2) + PointsCount) % PointsCount;
                P3n = (point + 1) % PointsCount;
            }
            else
            {
                P0n = (Mathf.Max (point - 2, 0) + PointsCount) % PointsCount;
                P3n = Mathf.Min (point + 1, Points.Count - 1);
            }

            // 2nd point may have been the 'last' point - a dupe of the first,
            // (to give a value of max track distance instead of zero)
            // but now it must be wrapped back to zero if that was the case.
            P2n = P2n % PointsCount;

            P0 = Points[P0n];
            P1 = Points[P1n];
            P2 = Points[P2n];
            P3 = Points[P3n];

            return CatmullRom (P0, P1, P2, P3, I);
        }


        private Vector3 CatmullRom (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float i)
        {
            // comments are no use here... it's the catmull-rom equation.
            // Un-magic this, lord vector!
            return 0.5f *
                   ((2 * p1) + (-p0 + p2) * i + (2 * p0 - 5 * p1 + 4 * p2 - p3) * i * i +
                    (-p0 + 3 * p1 - 3 * p2 + p3) * i * i * i);
        }


        private void CachePositionsAndDistances ()
        {
            // transfer the position of each point and distances between points to arrays for
            // speed of lookup at runtime
            Points = new List<Vector3> ();
            Distances = new List<float> ();

            float accumulateDistance = 0;
            for (int i = 0; i < Waypoints.Count; i++)
            {
                var t1 = Waypoints[(i) % Waypoints.Count];
                var t2 = Waypoints[(i + 1) % Waypoints.Count];

                Vector3 p1 = t1.position;
                Vector3 p2 = t2.position;
                Points.Add (Waypoints[i % Waypoints.Count].position);
                Distances.Add (accumulateDistance);
                accumulateDistance += (p1 - p2).magnitude;
            }

            if (LoopedPath)
            {
                Distances.Add (accumulateDistance);
                Points.Add (Waypoints[0].position);

            }

            Length = Distances[Distances.Count - 1];
        }

        public struct RoutePoint
        {
            public Vector3 Position;
            public Vector3 Direction;
            public float OvertakeZoneLeft;
            public float OvertakeZoneRight;
            public float SpeedLimit;

            public RoutePoint (Vector3 position, Vector3 direction, float overtakeZoneLeft, float overtakeZoneRight, float speedLimit)
            {
                this.Position = position;
                this.Direction = direction;
                this.OvertakeZoneLeft = overtakeZoneLeft;
                this.OvertakeZoneRight = overtakeZoneRight;
                this.SpeedLimit = speedLimit;
            }
        }

        [System.Serializable]
        public struct WaypointData
        {
            public Transform Point;
            public float OvertakeZoneLeft;
            public float OvertakeZoneRight;
            public float SpeedLimit;

            public Vector3 position { get { return Point? Point.position : Vector3.zero; } }
            public Vector3 localPosition { get { return Point? Point.localPosition : Vector3.zero; } }

            public WaypointData (Transform point, float overtakeZoneLeft = 2, float overtakeZoneRight = 2, float speedLimit = float.PositiveInfinity)
            {
                Point = point;
                OvertakeZoneLeft = overtakeZoneLeft;
                OvertakeZoneRight = overtakeZoneRight;
                SpeedLimit = speedLimit;
            }
        }
    }
}
