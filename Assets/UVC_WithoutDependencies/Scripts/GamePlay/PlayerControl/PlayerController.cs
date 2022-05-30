using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// A class for initializing player objects (such as camera, UI, etc.).
    /// </summary>
    public class PlayerController :InitializePlayer
    {
#pragma warning disable 0649

        [SerializeField] List<InitializePlayer> InitializeObjects = new List<InitializePlayer>();               //All objects to be initialized.

#pragma warning restore 0649

        public override bool Initialize (VehicleController vehicle)
        {
            if (!base.Initialize (vehicle))
            {
                Destroy (gameObject);
                return false;
            }

            InitializeObjects.ForEach (i => i.Initialize (vehicle));

            return true;
        }

        enum DeviceType
        {
            ConcoleOrPC,
            Mobile,
        }
    }
}