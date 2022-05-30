using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace PG
{
    /// <summary>
    /// This component is needed to toggle the FPS limit on mobile devices.
    /// </summary>
    public class QualitySwitch :MonoBehaviour, IPointerClickHandler
    {
#pragma warning disable 0649

        [SerializeField] TextMeshProUGUI CurrentFpsText;

#pragma warning restore 0649

        int QualityIndex
        {
            get
            {
                return PlayerPrefs.GetInt ("Quality", QualitySettings.GetQualityLevel());
            }
            set
            {
                PlayerPrefs.SetInt ("Quality", value);
            }
        }

        void Start ()
        {
            QualitySettings.SetQualityLevel (QualityIndex);
            CurrentFpsText.text = string.Format ("Quality: {0}", QualitySettings.names[QualityIndex]);
        }

        public void OnPointerClick (PointerEventData eventData)
        {
            var count = QualitySettings.names.Length;
            QualityIndex = MathExtentions.Repeat (QualityIndex + 1, 0, count - 1);
            QualitySettings.SetQualityLevel (QualityIndex);
            CurrentFpsText.text = string.Format ("Quality: {0}", QualitySettings.names[QualityIndex]);
        }
    }
}
