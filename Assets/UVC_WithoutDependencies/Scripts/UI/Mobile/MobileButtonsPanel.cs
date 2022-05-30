using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PG
{
    public class MobileButtonsPanel :MonoBehaviour
    {

#pragma warning disable 0649

        [SerializeField] float CloseY;
        [SerializeField] float OpenY;
        [SerializeField] float OpenCloseSpeed = 1000;
        [SerializeField] Button OpenButton;
        [SerializeField] Button CloseButton;

#pragma warning restore 0649

        RectTransform RectTR;
        float TargetY;

        void Start ()
        {
            OpenButton.SetActive (true);
            CloseButton.SetActive (false);

            RectTR = GetComponent<RectTransform> ();
            RectTR.SetAnchoredY (CloseY);

            OpenButton.onClick.AddListener (() => 
            { 
                TargetY = OpenY;
                OpenButton.SetActive (false);
                CloseButton.SetActive (true);
            });

            CloseButton.onClick.AddListener (() =>
            {
                TargetY = CloseY;
                OpenButton.SetActive (true);
                CloseButton.SetActive (false);
            });
        }

        // Update is called once per frame
        void Update ()
        {
            if (RectTR.anchoredPosition.y != TargetY)
            {
                RectTR.SetAnchoredY (Mathf.MoveTowards(RectTR.anchoredPosition.y, TargetY, OpenCloseSpeed * Time.deltaTime));
            }
        }
    }
}
