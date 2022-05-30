using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

/// <summary>
/// The same as the "On-Screen Button", with an additional check for pressing to prevent sticking of buttons.
/// </summary>

[AddComponentMenu ("Input/On-Screen Button Custom")]
public class OnScreenButtonCustom :OnScreenControl, IPointerDownHandler, IPointerUpHandler
{
    [InputControl(layout = "Button")]
    [SerializeField]
    private string m_ControlPath;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    bool Pressed;

    Touchscreen Touchscreen => Touchscreen.current;
    TouchControl Touch;

    public void OnPointerUp (PointerEventData eventData)
    {
        OnPointerUp ();
    }

    public void OnPointerUp ()
    {
        SendValueToControl (0.0f);
        Pressed = false;
        Touch = null;
    }

    public void OnPointerDown (PointerEventData eventData)
    {
        SendValueToControl (1.0f);
        Pressed = true;

        var extEventData = eventData as ExtendedPointerEventData;
        if (Touchscreen != null && extEventData != null)
        {
            foreach (var touch in Touchscreen.touches)
            {
                if (extEventData.touchId == touch.touchId.ReadValue())
                {
                    Touch = touch;
                    break;
                }
            }
        }
    }

    private void Update ()
    {

#if !UNITY_EDITOR
        if ((Touch == null || !Touch.press.isPressed) && Pressed)
        {
            OnPointerUp ();
        }
#endif
    }

    public void Disable ()
    {
        if (Pressed)
        {
            SendValueToControl (0.0f);
            Pressed = false;
            Touch = null;
        }
    }
}
