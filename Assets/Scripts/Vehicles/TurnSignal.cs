using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using System;

namespace Vehicles{
    public enum IndicatorDirection { None, Left, Right, All }
    public class TurnSignal : ITurnSignal
    {
        public TurnSignal(Transform parent, MonoBehaviour monoBehaviour, VehicleMaterials vehicleMaterials)
        {
            leftRenderers = parent.Find(LEFT).GetComponentsInChildren<MeshRenderer>();
            rightRenderers = parent.Find(RIGHT).GetComponentsInChildren<MeshRenderer>();

            mono = monoBehaviour;

            activeBlinkList = new List<MeshRenderer>();

            blinkPulseRate = .4f;

            materials = vehicleMaterials;
        }

        const string LEFT = "_turn_signal_left";
        const string RIGHT = "_turn_signal_right";

        MeshRenderer[] leftRenderers;
        MeshRenderer[] rightRenderers;

        List<MeshRenderer> activeBlinkList;

        MonoBehaviour mono;

        IndicatorDirection indicatorDirection;

        IEnumerator updateBlinkLightsCo;
        Coroutine blinkCoroutine;

        float blinkPulseTimer;
        float blinkPulseRate;

        bool isBlinkActive;

        VehicleMaterials materials;

        public Action<IndicatorDirection>OnStateChange { get; set; }

        public void SetIndicatorState(IndicatorDirection indicatorDir)
        {
            if(leftRenderers.Length <= 0 || rightRenderers.Length <= 0) return;
            
            if (blinkCoroutine != null && indicatorDirection == indicatorDir) indicatorDirection = IndicatorDirection.None;
            else indicatorDirection = indicatorDir;

            for (int i = 0; i < activeBlinkList.Count; i++) // resets defaults
            {
                activeBlinkList[i].material = materials.GetDefaultMaterial;
            }

            activeBlinkList.Clear();

            switch (indicatorDirection)
            {
                case IndicatorDirection.None: break;
                case IndicatorDirection.Left: activeBlinkList = leftRenderers.ToList<MeshRenderer>(); break;
                case IndicatorDirection.Right: activeBlinkList = rightRenderers.ToList<MeshRenderer>(); break;
                case IndicatorDirection.All: activeBlinkList = leftRenderers.Concat(rightRenderers).ToList<MeshRenderer>(); break;
            }

            OnStateChange?.Invoke(indicatorDirection);

            if (blinkCoroutine != null) return;

            updateBlinkLightsCo = updateBlinkLights();
            blinkCoroutine = mono.StartCoroutine(updateBlinkLightsCo);
        }

        IEnumerator updateBlinkLights()
        { /// Schimbă materialul de semnalizare + timerul de pulse
            blinkPulseTimer = 0;
            isBlinkActive = false;

            while (indicatorDirection != IndicatorDirection.None)
            {

                if (blinkPulseTimer <= 0)
                {
                    blinkPulseTimer = blinkPulseRate;

                    for (int i = 0; i < activeBlinkList.Count; i++)
                    {
                        if (!isBlinkActive)
                        {
                            activeBlinkList[i].material = materials.GetTurnSignal;
                        }
                        else
                        {
                            activeBlinkList[i].material = materials.GetDefaultMaterial;
                        }
                    }

                    isBlinkActive = !isBlinkActive;
                }
                else
                {
                    blinkPulseTimer -= Time.deltaTime;
                }

                yield return null;
            }

            blinkCoroutine = null;
        }
        public void Reset(){
            indicatorDirection = IndicatorDirection.None;
        }
    }
}