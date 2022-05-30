using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    namespace GameBalance
    {
        [CreateAssetMenu (fileName = "RaceAIConfig", menuName = "AI/RaceAIConfigAsset")]
        public class RaceAIConfigAsset :BaseAIConfigAsset
        {
            public RaceAIConfig RaceAIConfig;
        }
    }
}
