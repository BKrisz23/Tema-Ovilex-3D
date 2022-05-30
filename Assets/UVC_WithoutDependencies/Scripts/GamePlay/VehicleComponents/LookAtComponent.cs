using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    public class LookAtComponent :MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] Transform LookAtTransform;
        [SerializeField] Direction UpWorld;

#pragma warning restore 0649

        Dictionary <Direction, Vector3> Directions = new Dictionary<Direction, Vector3>()
        {
            { Direction.up, Vector3.up },
            { Direction.down, Vector3.down },
            { Direction.left, Vector3.left },
            { Direction.right, Vector3.right },
            { Direction.forward, Vector3.forward },
            { Direction.back, Vector3.back }
        };

        void Update ()
        {
            transform.LookAt (LookAtTransform, Directions[UpWorld]);
        }

#if UNITY_EDITOR

        [ContextMenu("UpdateTransform")]
        void UpdateTransform ()
        {
            Update ();
        }

#endif
    }

    public enum Direction
    {
        up,
        down,
        left,
        right,
        forward,
        back
    }
}
