using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// A simple GameObject (eg a road) with GroundConfig.
    /// </summary>
    public class ObjectGroundEntity :IGroundEntity
    {
#pragma warning disable 0649

        [SerializeField] GroundConfig GroundConfig;

#pragma warning restore 0649

        public override GroundConfig GetGroundConfig (Vector3 position)
        {
            return GroundConfig;
        }
    }
}
