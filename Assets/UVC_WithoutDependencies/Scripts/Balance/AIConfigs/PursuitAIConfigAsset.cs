using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    namespace GameBalance
    {
        [CreateAssetMenu (fileName = "PursuitAIConfig", menuName = "AI/PursuitAIConfigAsset")]
        public class PursuitAIConfigAsset :BaseAIConfigAsset
        {
            public PursuitAIConfig PursuitAIConfig;
        }
    }
}
