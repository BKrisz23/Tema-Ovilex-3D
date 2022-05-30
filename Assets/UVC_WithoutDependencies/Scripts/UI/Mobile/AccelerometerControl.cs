using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace PG
{
    /// <summary>
    /// Mobile input accelerometer.
    /// </summary>
    public class AccelerometerControl :OnScreenControl
    {

#pragma warning disable 0649

        [SerializeField] CarController TargetCar;
        [SerializeField] float DeadZone = 5f;
        [SerializeField] float MaxAngle = 45f;
        [SerializeField] float AccelerometerLerpSpeed = 500;

        [InputControl(layout = "Value")]
        [SerializeField] private string m_ControlPath;

#pragma warning restore 0649

        float HorizontalAxis;

        CarController getTargetCar { get { return TargetCar ?? (GameController.Instance ? GameController.Instance.PlayerCar1 : null); } }

        protected override string controlPathInternal
        {
            get => m_ControlPath;
            set => m_ControlPath = value;
        }

        private void Awake ()
        {
            if (Accelerometer.current != null)
            {
                InputSystem.EnableDevice (Accelerometer.current);
            }
        }

        private void OnApplicationFocus (bool focus)
        {
            if (focus && Accelerometer.current != null)
            {
                InputSystem.EnableDevice (Accelerometer.current);
            }
        }

        private void OnDestroy ()
        {
            if (Accelerometer.current != null)
            {
                InputSystem.DisableDevice (Accelerometer.current);
            }
        }

        private void Update ()
        {
            //The tilt of the phone sets the velocity vector to the desired angle.
            if (Accelerometer.current != null)
            {
                float axisX = Accelerometer.current.acceleration.ReadValue().x * 90;
                float targetAnge = 0;
                if (axisX > DeadZone || axisX < -DeadZone)
                {
                    targetAnge = Mathf.Clamp ((axisX + (axisX > 0 ? -DeadZone : DeadZone)) / (MaxAngle), -1, 1) * 90;
                }

                if (getTargetCar != null && !getTargetCar.InHandBrake && getTargetCar.VehicleDirection >= 0 && getTargetCar.CurrentSpeed > 1)
                {
                    targetAnge += getTargetCar.VelocityAngle;
                }

                targetAnge = targetAnge.Clamp (-90, 90) / 90;

                targetAnge = Mathf.Lerp (HorizontalAxis, targetAnge, Time.deltaTime * AccelerometerLerpSpeed);
                if (HorizontalAxis != targetAnge)
                {
                    HorizontalAxis = targetAnge;
                    SendValueToControl (HorizontalAxis);
                }
            }
        }
    }
}
