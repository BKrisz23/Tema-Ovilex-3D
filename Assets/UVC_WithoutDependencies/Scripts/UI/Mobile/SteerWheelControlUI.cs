using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace PG
{
    /// <summary>
    /// Mobile input SteerWheel.
    /// </summary>
    public class SteerWheelControlUI :OnScreenControl, IPointerDownHandler, IPointerUpHandler
    {

#pragma warning disable 0649

        [SerializeField] CarController TargetCar;

        [SerializeField] float MaxSteerWheelAngle = 180;
        [SerializeField] float SteerWheelToDefaultSpeed = 360;

        [InputControl(layout = "Value")]
        [SerializeField] private string m_ControlPath;

#pragma warning restore 0649

        float HorizontalAxis;
        float CurrentSteerAngle;
        TouchControl SteerTouch;
        Vector2 PrevTouchPos;

#if UNITY_EDITOR
        bool WheelIsPressed;
#else
        bool WheelIsPressed => SteerTouch != null && SteerTouch.isInProgress;
#endif

        CarController getTargetCar { get { return TargetCar ?? (GameController.Instance ? GameController.Instance.PlayerCar1 : null); } }
        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private void Update ()
        {
            float targetAnge;
            float carVelocityAngleNormolized = getTargetCar? getTargetCar.VelocityAngle / 90: 0;
            bool needGetCarVelocity = getTargetCar && getTargetCar.VehicleDirection >= 0 && getTargetCar.CurrentSpeed > 1;
            if (!WheelIsPressed)
            {
                targetAnge = (needGetCarVelocity ? carVelocityAngleNormolized : 0) * MaxSteerWheelAngle;
                CurrentSteerAngle = Mathf.MoveTowards (CurrentSteerAngle, targetAnge, Time.deltaTime * SteerWheelToDefaultSpeed);
                if (SteerTouch != null)
                {
                    SteerTouch = null;
                }
            }
            else
            {
                Vector2 pressedPos = transform.position;
                if (Application.isMobilePlatform && Touchscreen.current != null)
                {
                    if (SteerTouch != null)
                    {
                        pressedPos -= SteerTouch.position.ReadValue();
                    }
                }
                else if (Mouse.current != null)
                {
                    pressedPos -= Mouse.current.position.ReadValue();
                }
                float angleDelta = Vector2.SignedAngle (PrevTouchPos, pressedPos);
                PrevTouchPos = pressedPos;
                CurrentSteerAngle = Mathf.Clamp (CurrentSteerAngle + angleDelta, -MaxSteerWheelAngle, MaxSteerWheelAngle);
            }
            transform.rotation = Quaternion.AngleAxis (CurrentSteerAngle, Vector3.forward);

            targetAnge = -CurrentSteerAngle / MaxSteerWheelAngle;

            if (needGetCarVelocity)
            {
                targetAnge += carVelocityAngleNormolized;
            }

            targetAnge = targetAnge.Clamp (-1, 1);
            if (HorizontalAxis != targetAnge)
            {
                HorizontalAxis = targetAnge;
                SendValueToControl (targetAnge);
            }
        }

        public void OnPointerDown (PointerEventData eventData)
        {
            PrevTouchPos = (Vector2)transform.position - eventData.position;
            if (Touchscreen.current != null)
            {
                for (int i = 0; i < Touchscreen.current.touches.Count; i++)
                {
                    if (Touchscreen.current.touches[i].press.wasPressedThisFrame)
                    {
                        SteerTouch = Touchscreen.current.touches[i];
                        return;
                    }
                }
            }
#if UNITY_EDITOR
            WheelIsPressed = true;
#endif
        }

        public void OnPointerUp (PointerEventData eventData)
        {
            SteerTouch = null;
#if UNITY_EDITOR
            WheelIsPressed = false;
#endif
        }
    }
}
