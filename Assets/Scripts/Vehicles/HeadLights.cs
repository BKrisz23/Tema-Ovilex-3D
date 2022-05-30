using System;
using UnityEngine;

namespace Vehicles{
    public enum Headlight { None, Low, High }
    public class HeadLights : IHeadLights
    {
        public HeadLights(Transform parent)
        {
            Transform headLightsT = parent.Find(PATH);

            meshRenderers = headLightsT.GetComponentsInChildren<MeshRenderer>();
            lowLight = headLightsT.Find(LOW_LIGHT).gameObject;
            highLight = headLightsT.Find(HIGH_LIGHT).gameObject;
        }
        const string PATH = "_headlights";
        const string LOW_LIGHT = "_low_light";
        const string HIGH_LIGHT = "_high_light";

        MeshRenderer[] meshRenderers;

        GameObject lowLight;
        GameObject highLight;

        Headlight headlight;

        public Action<Headlight> OnStateChange { get; set; }

        public void Toggle()
        {
            if (headlight == Headlight.None) headlight = Headlight.Low;
            else if (headlight == Headlight.Low) headlight = Headlight.High;
            else if (headlight == Headlight.High) headlight = Headlight.None;

            if (headlight == Headlight.Low)
            {
                if(meshRenderers.Length > 0)
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        meshRenderers[i].enabled = true;
                    }

                if(lowLight != null)
                    lowLight.SetActive(true);

            }
            else if (headlight == Headlight.High)
            {
                if(highLight != null)
                    highLight.SetActive(true);
            }
            else
            {
                Reset();
            }

            OnStateChange?.Invoke(headlight);
        }

        public void Reset()
        {
            if (headlight != Headlight.None){
                headlight = Headlight.None;
                OnStateChange?.Invoke(headlight);
            }

            if(meshRenderers.Length > 0)
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    meshRenderers[i].enabled = false;
                }

            if(lowLight != null)
                lowLight.SetActive(false);
            if(highLight != null)
                highLight.SetActive(false);
        }
    }
}