using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    namespace GameBalance
    {

        /// <summary>
        /// Mask and layers settings.
        /// </summary>

        [CreateAssetMenu (fileName = "LayerSettings", menuName = "GameBalance/Settings/LayerSettings")]
        public class LayerSettings :ScriptableObject
        {
            public LayerMask ResetCarTrigger;
            public LayerMask RoadMask;

            public LayerMask VehicleMask;
        }

    }
}
