using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// AI trigger. Now used only for boost use. But you can add any logic of your own by analogy.
    /// </summary>
    public class AITrigger :MonoBehaviour
    {
        public bool Boost { get { return BoostProbability > 0; } }
        [Range(0, 1)] public float BoostProbability;
    }
}
