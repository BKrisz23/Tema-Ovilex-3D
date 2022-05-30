using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// The moved object, upon damage just moves from the side of the damage velocity.
    /// </summary>
    public class MoveableDO :DamageableObject
    {
        public virtual void MoveObject(Vector3 damageVelocity)
        {
            transform.localPosition += damageVelocity;
        }
    }
}
