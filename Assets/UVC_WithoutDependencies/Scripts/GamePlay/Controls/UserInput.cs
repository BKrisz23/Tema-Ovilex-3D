using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.InputSystem.Users;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;

namespace PG
{
    /// <summary>
    /// For user multiplatform control. This way of implementing the input is chosen to be able to implement control of several players for one device.
    /// </summary>
    public class UserInput :InitializePlayer, ICarControl
    {
        public float HorizontalChangeSpeed = 10;            //To simulate the use of a keyboard trigger.
        public float Horizontal { get; private set; }
        public float Acceleration { get; set; }
        public float BrakeReverse { get; set; }
        public float Pitch { get; private set; }
        public bool HandBrake { get; private set; }
        public bool Boost { get; private set; }
        public Vector2 ViewDelta { get; private set; }
        public InputDevice ViewDeltaDevice { get; private set; }
        public InputDevice ActiveDevice { get; private set; }
        public bool ManualCameraRotation { get; private set; }

        public event System.Action OnChangeViewAction;

        float TargetHorizontal;

        CarLighting CarLighting;

        IInputActionCollection PlayerInputActions;
        int TouchCount;
        List<TouchControl> TouchesInProgress;
        TouchControl RotateTouch;
        event System.Action OnDestroyAction;

        InputUser User = default(InputUser);

        public static InputDevice DevicePlayer1;
        public static InputDevice DevicePlayer2;

        /// Workaround **********************************
        public System.Action<bool> OnBreakStateChange {get; set;} 
        public System.Action OnAccelerationStateChange {get; set;}

        ///**************************************
        private void Update ()
        {
            Horizontal = Mathf.MoveTowards (Horizontal, TargetHorizontal, Time.deltaTime * HorizontalChangeSpeed);

            var touchScreen = Touchscreen.current;

            if (Mouse.current != null)
            {
                ManualCameraRotation = Mouse.current.leftButton.isPressed;
            }
            else if (touchScreen != null)
            {
                var touchCount = touchScreen.touches.Count(t => t.isInProgress);
                if (touchCount != TouchCount)
                {
                    TouchesInProgress = touchScreen.touches.Where (t => t.isInProgress).ToList ();
                    if (touchCount > TouchCount && !ManualCameraRotation)
                    {
                        var lastTouch = TouchesInProgress.Last (t => t.press.wasPressedThisFrame);
                        if (!IsPointerOverUIObject (lastTouch))
                        {
                            RotateTouch = lastTouch;
                            ManualCameraRotation = true;
                        }
                        
                    }
                    else if (touchCount < TouchCount && ManualCameraRotation)
                    {
                        ManualCameraRotation = false;
                        RotateTouch = null;
                        for (int i = 0; i < touchCount; i++)
                        {
                            if (!IsPointerOverUIObject (TouchesInProgress[i]))
                            {
                                RotateTouch = TouchesInProgress[i];
                                ManualCameraRotation = true;
                                break;
                            }
                        }
                    }

                    TouchCount = touchCount;
                }

                if (RotateTouch != null)
                {
                    ViewDelta = RotateTouch.delta.ReadValue ();
                }
            }
        }

        private void OnDestroy ()
        {
            if (PlayerInputActions != null)
            {
                PlayerInputActions.Disable ();
            }

            OnDestroyAction.SafeInvoke ();

            if (User != null && !GameController.IsMobilePlatform && User.id != InputUser.InvalidId)
            {
                User.UnpairDevicesAndRemoveUser ();
            }
        }

        void OnSetPause (bool value)
        {
            if (value)
            {
                PlayerInputActions.Disable ();
            }
            else
            {
                PlayerInputActions.Enable ();
            }
        }

        public void PairWithDevice (InputDevice device)
        {
            if (device != null)
            {
                User = InputUser.PerformPairingWithDevice (device, User);
            }
        }

        public override bool Initialize (VehicleController car)
        {
            base.Initialize (car);

            if (GameController.SplitScreen)
            {
                foreach (var device in InputSystem.devices)
                {
                    if (device is Keyboard)
                    {
                        PairWithDevice (device);
                    }
                }

                if (GameController.PlayerCar1 == car)
                {
                    PlayerInputActions = new P1Input ();
                    PairWithDevice (Mouse.current);
                    PairWithDevice (DevicePlayer1);
                }
                else
                {
                    PlayerInputActions = new P2Input ();
                    
                    PairWithDevice (DevicePlayer2);
                }

                User.AssociateActionsWithUser (PlayerInputActions);
            }
            else
            {
                PlayerInputActions = new P1Input ();
            }

            PlayerInputActions.Enable ();
            var actions = PlayerInputActions.ToList();

            InputAction action;

            action = actions.Find (a => a.name == "Acceleration");
            AddAction (action, OnAcceleration, InputActionPhase.Performed | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "BrakeReverse");
            AddAction (action, OnBrakeReverse, InputActionPhase.Performed | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "Steer");
            AddAction (action, OnSteer, InputActionPhase.Performed | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "Pitch");
            AddAction (action, OnPitch, InputActionPhase.Performed | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "NextGear");
            AddAction (action, OnNextGear, InputActionPhase.Started);

            action = actions.Find (a => a.name == "PrevGear");
            AddAction (action, OnPrevGear, InputActionPhase.Started);

            action = actions.Find (a => a.name == "Lights");
            AddAction (action, OnLights, InputActionPhase.Started);

            action = actions.Find (a => a.name == "LeftTurnSignal");
            AddAction (action, OnLeftTurnSignal, InputActionPhase.Started);

            action = actions.Find (a => a.name == "RightTurnSignal");
            AddAction (action, OnRightTurnSignal, InputActionPhase.Started);

            action = actions.Find (a => a.name == "Alarm");
            AddAction (action, OnAlarm, InputActionPhase.Started);

            action = actions.Find (a => a.name == "ResetCar");
            AddAction (action, OnResetCar, InputActionPhase.Started);

            action = actions.Find (a => a.name == "ChangeView");
            AddAction (action, OnChangeView, InputActionPhase.Started);

            action = actions.Find (a => a.name == "ViewDelta");
            AddAction (action, OnViewDelta, InputActionPhase.Performed | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "HandBrake");
            AddAction (action, OnHandBrake, InputActionPhase.Started | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "Boost");
            AddAction (action, OnBoost, InputActionPhase.Started | InputActionPhase.Canceled);

            action = actions.Find (a => a.name == "ConnectTrailer");
            AddAction (action, OnConnectTrailer, InputActionPhase.Started);

            if (Car)
            {
                CarLighting = Car.GetComponent<CarLighting> ();
                var aiControl = Car.GetComponent<ICarControl>();
                if (aiControl == null || !(aiControl is PositioningAIControl))
                { 
                    Car.CarControl = this;
                }
            }

            return IsInitialized;
        }

        void AddAction (InputAction action, InputActionDelegate actionDelegate, InputActionPhase phases)
        {
            if (phases.HasFlag (InputActionPhase.Started))
            {
                action.started += actionDelegate.Invoke;

                OnDestroyAction += () =>
                {
                    action.started -= actionDelegate.Invoke;
                };
            }

            if (phases.HasFlag (InputActionPhase.Performed))
            {
                action.performed += actionDelegate.Invoke;

                OnDestroyAction += () =>
                {
                    action.performed -= actionDelegate.Invoke;
                };
            }

            if (phases.HasFlag (InputActionPhase.Canceled))
            {
                action.canceled += actionDelegate.Invoke;

                OnDestroyAction += () =>
                {
                    action.canceled -= actionDelegate.Invoke;
                };
            }
        }

        public void OnAcceleration (InputAction.CallbackContext context)
        {
            Acceleration = context.ReadValue<float> ();
            if(context.started) OnAccelerationStateChange?.Invoke();
            if(context.canceled) OnAccelerationStateChange?.Invoke();
        }

        public void OnBrakeReverse (InputAction.CallbackContext context)
        {
            BrakeReverse = context.ReadValue<float> ();
            if(context.started) OnBreakStateChange?.Invoke(true);
            if(context.canceled) OnBreakStateChange?.Invoke(false);
        }
        public void OnSteer (InputAction.CallbackContext context)
        {
            TargetHorizontal = context.ReadValue<float> ();
        }

        public void OnPitch (InputAction.CallbackContext context)
        {
            Pitch = context.ReadValue<float> ();
        }

        public void OnNextGear (InputAction.CallbackContext context)
        {
            if (Car)
            {
                Car.NextGear ();
            }
        }

        public void OnPrevGear (InputAction.CallbackContext context)
        {
            if (Car)
            {
                Car.PrevGear ();
            }
        }

        public void OnLights (InputAction.CallbackContext context)
        {
            CarLighting.SwitchMainLights ();
        }

        public void OnLeftTurnSignal (InputAction.CallbackContext context)
        {
            CarLighting.TurnsEnable (TurnsStates.Left);
        }

        public void OnRightTurnSignal (InputAction.CallbackContext context)
        {
            CarLighting.TurnsEnable (TurnsStates.Right);
        }

        public void OnAlarm (InputAction.CallbackContext context)
        {
            CarLighting.TurnsEnable (TurnsStates.Alarm);
        }

        public void OnConnectTrailer (InputAction.CallbackContext context)
        {
            if (Car)
            {
                Car.TryConnectDisconnectTrailer ();
            }
        }

        public void OnResetCar (InputAction.CallbackContext context)
        {
            Vehicle.ResetVehicle ();
        }

        public void OnChangeView (InputAction.CallbackContext context)
        {
            OnChangeViewAction.SafeInvoke ();
        }

        public void OnViewDelta (InputAction.CallbackContext context)
        {
            ViewDelta = context.ReadValue<Vector2> ();
            ViewDeltaDevice = context.control.device;
        }

        public void OnHandBrake (InputAction.CallbackContext context)
        {
            HandBrake = context.phase == InputActionPhase.Started;
        }

        public void OnBoost (InputAction.CallbackContext context)
        {
            Boost = context.phase == InputActionPhase.Started;
        }

        bool IsPointerOverUIObject (TouchControl touch)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = touch.position.ReadValue();
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll (eventDataCurrentPosition, results);
            return results.Count > 0;
        }
    }

    delegate void InputActionDelegate (InputAction.CallbackContext context);
}
