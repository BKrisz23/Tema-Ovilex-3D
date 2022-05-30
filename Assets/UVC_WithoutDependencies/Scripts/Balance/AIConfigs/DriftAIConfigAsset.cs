using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    namespace GameBalance
    {
        [CreateAssetMenu (fileName = "DriftAIConfig", menuName = "AI/DriftAIConfigAsset")]
        public class DriftAIConfigAsset :BaseAIConfigAsset
        {
            public DriftAIConfig DriftAIConfig;
        }
    }
}
