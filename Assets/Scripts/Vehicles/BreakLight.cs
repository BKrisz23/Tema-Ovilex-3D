using System;
using UnityEngine;

namespace Vehicles{
    public class BreakLight : IBreakLight
    {
        public BreakLight(Transform parent, VehicleMaterials vehicleMaterials)
        {
            meshRenderer = parent.Find(BREAK_LIGHT_MESH).GetComponentInChildren<MeshRenderer>();

            materials = vehicleMaterials;
        }

        const string BREAK_LIGHT_MESH = "_break_lights";

        MeshRenderer meshRenderer;

        VehicleMaterials materials;

        public void SetBreakMaterial(bool state)
        {
            if(meshRenderer == null) return;
            switch(state){
                case true: meshRenderer.material = materials.GetBreakLight; break;
                case false: Reset(); break;
            }
        }

        public void Reset()
        {
            if(meshRenderer == null) return;
            meshRenderer.material = materials.GetDefaultMaterial;
        }
    }
}