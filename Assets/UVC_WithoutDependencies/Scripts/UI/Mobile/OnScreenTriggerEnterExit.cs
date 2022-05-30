using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// The same as the "On-Screen Button", with an additional check for pressing to prevent sticking of buttons.
/// </summary>

[AddComponentMenu ("Input/On-Screen Trigger EnterExit Custom")]
public class OnScreenTriggerEnterExit :OnScreenControl, IPointerEnterHandler, IPointerExitHandler
{
    [InputControl(layout = "Button")]
    [SerializeField]
    private string m_ControlPath;

    float value;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    public void OnPointerEnter (PointerEventData eventData)
    {
        value = 1;
        SendValueToControl (value);
    }

    public void OnPointerExit (PointerEventData eventData)
    {
        value = 0;
        SendValueToControl (value);
    }

    public void Disable ()
    {
        if (value == 1)
        {
            value = 0;
            SendValueToControl (value);
        }
    }
}
