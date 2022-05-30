using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace PG
{
    /// <summary>
    /// This component is needed to toggle the FPS limit on mobile devices.
    /// </summary>
    public class TargetFrameSwitch :MonoBehaviour, IPointerClickHandler
    {

#pragma warning disable 0649

        [SerializeField] TextMeshProUGUI CurrentFpsText;

#pragma warning restore 0649

        void Start ()
        {
            Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
            CurrentFpsText.text = string.Format("Max FPS: {0}", Application.targetFrameRate);
        }

        public void OnPointerClick (PointerEventData eventData)
        {
            Application.targetFrameRate = Application.targetFrameRate == 60 ? 30 : 60;
            CurrentFpsText.text = string.Format ("Max FPS: {0}", Application.targetFrameRate);
        }
    }
}
