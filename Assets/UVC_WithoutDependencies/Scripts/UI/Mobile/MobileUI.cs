using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PG
{
    public class MobileUI :MonoBehaviour
    {

#pragma warning disable 0649

        [SerializeField] Button SelectNextControl;
        [SerializeField] TextMeshProUGUI CurrentControlText;
        [SerializeField] List<GameObject> AllControls;

#pragma warning restore 0649

        int SelectedIndex = 0;

        void Start ()
        {
            gameObject.SetActive (GameController.IsMobilePlatform);
            
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            SelectNextControl.onClick.AddListener (OnSelectNextControl);
            SelectControl (SelectedIndex);
        }

        void OnSelectNextControl ()
        {
            SelectedIndex = MathExtentions.Repeat (SelectedIndex+1, 0, AllControls.Count - 1);
            SelectControl (SelectedIndex);
        }

        void SelectControl (int index)
        {
            for (int i = 0; i < AllControls.Count; i++)
            {
                AllControls[i].SetActive (index == i);
                if (AllControls[i].activeInHierarchy)
                {
                    CurrentControlText.text = AllControls[i].name;
                }
                else
                {
                    foreach (var o in AllControls[i].GetComponentsInChildren<OnScreenButtonCustom> (true))
                    {
                        o.Disable ();
                    }

                    foreach (var o in AllControls[i].GetComponentsInChildren<OnScreenTriggerEnterExit> (true))
                    {
                        o.Disable ();
                    }
                }
            }
        }
    }
}
