using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomButton :Button
{
    public bool Pressed { get { return IsPressed (); } }

    public bool IsPointerInside { get; private set; }

    public override void OnPointerEnter (PointerEventData eventData)
    {
        base.OnPointerEnter (eventData);
        IsPointerInside = true;
    }

    public override void OnPointerExit (PointerEventData eventData)
    {
        base.OnPointerExit (eventData);
        IsPointerInside = false;
    }
}
