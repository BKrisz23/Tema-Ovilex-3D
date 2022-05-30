#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// AI spawner, for debugging. 
    /// Will spawn the selected car along the entire selected path (it is necessary that the car was at the beginning of the path). 
    /// If the path is not looped, then the car returns to the beginning of the path after the finish.
    /// </summary>
    public class AIDebugSpawner :MonoBehaviour
    {
        public PositioningAIControl AIControl;
        public AIPath AIPath;
        public float SpawnInterval = 50;            //The interval between AI. Be careful, the longer the path, the larger the interval should be.

        public List<PositioningAIControl> AIs = new List<PositioningAIControl>();

        Vector3 StartPos;
        Quaternion StartRot;

        IEnumerator Start ()
        {
            if (AIPath == null)
            {
                AIPath = AIPath.FirstPath;
            }
            if (AIPath == null)
            {
                Debug.LogError ("AIPath not found");
                enabled = false;
                yield break;
            }

            yield return null;

            AIs.Add (AIControl);

            StartPos = AIControl.transform.position;
            StartRot = AIControl.transform.rotation;

            float dist = AIControl.ProgressDistance + SpawnInterval;
            AIPath.RoutePoint routePoint;

            //Spawn AI.
            while (dist < AIPath.Length)
            {
                routePoint = AIPath.GetRoutePoint (dist);
                var ai = Instantiate(AIControl);
                ai.transform.position = routePoint.Position;
                ai.transform.rotation = Quaternion.LookRotation(routePoint.Direction);

                AIs.Add (ai);

                dist += SpawnInterval;
            }
        }

        //Reset AI After finish.
        void RestartAI (PositioningAIControl ai)
        {
            ai.ProgressDistance = 0;
            ai.transform.position = StartPos;
            ai.transform.rotation = StartRot;
            ai.Car.RB.velocity = Vector3.zero;
            ai.Car.RB.angularVelocity = Vector3.zero;
            ai.Start ();
        }

        private void Update ()
        {
            foreach (var ai in AIs)
            {
                if (ai.Finished)
                {
                    RestartAI (ai);
                }
            }
        }
    }
}

#endif
