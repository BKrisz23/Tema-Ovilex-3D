using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    namespace GameBalance
    {
        [CreateAssetMenu (fileName = "BaseAIConfig", menuName = "AI/BaseAIConfigAsset")]
        public class BaseAIConfigAsset :ScriptableObject
        {
            public BaseAIConfig AIConfig;
        }
    }
}
