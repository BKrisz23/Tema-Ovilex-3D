using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    namespace GameBalance
    {

        /// <summary>
        /// Main game settings.
        /// </summary>

        [CreateAssetMenu (fileName = "GameSettings", menuName = "GameBalance/Settings/GameSettings")]
        public class GameSettings :ScriptableObject
        {
            public MeasurementSystem EnumMeasurementSystem;
            public List<VehicleController> AvailableVehicles = new List<VehicleController>();
        }
    }
}
