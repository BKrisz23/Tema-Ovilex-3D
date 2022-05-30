using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Car light logic.
    /// </summary>
    public class CarLighting :MonoBehaviour
    {
#pragma warning disable 0649

        [SerializeField] float TurnsSwitchHalfRepeatTime = 0.5f;   //Half time light on/off.

#pragma warning restore 0649

        //All light is searched for in child elements, 
        //depending on the set tag, the light gets into the desired list.
        List<LightObject> MainLights = new List<LightObject>();
        List<LightObject> LeftTurnLights = new List<LightObject>();
        List<LightObject> RightTurnLights = new List<LightObject>();
        List<LightObject> BrakeLights = new List<LightObject>();
        List<LightObject> ReverseLights = new List<LightObject>();

        CarController _Car;
        //Used property, to be able to connect the trailer to the vehicle.
        public CarController Car 
        { 
            get 
            { 
                return _Car; 
            }
            set
            {
                if (_Car != null)
                {
                    Car.OnChangeGearAction -= OnChangeGear;
                    OnChangeGear (0);
                }
                _Car = value;

                if (_Car != null)
                {
                    Car.OnChangeGearAction += OnChangeGear;
                }
            }
        }

        bool InBrake;
        bool MainLightsIsOn;
        Coroutine TurnsCotoutine;
        List<LightObject> ActiveTurns = new List<LightObject>();
        TurnsStates CurrentTurnsState = TurnsStates.Off;

        public event System.Action<CarLightType, bool> OnSetActiveLight;

        public CarLighting AdditionalLighting { get; set; }

        void Start ()
        {
            //Searching and distributing all lights.
            var lights = GetComponentsInChildren<LightObject>();
            foreach (var l in lights)
            {
                switch (l.CarLightType)
                {
                    case CarLightType.Main:
                    MainLights.Add (l); break;
                    case CarLightType.TurnLeft:
                    LeftTurnLights.Add (l);
                    break;
                    case CarLightType.TurnRight:
                    RightTurnLights.Add (l);
                    break;
                    case CarLightType.Brake:
                    BrakeLights.Add (l);
                    break;
                    case CarLightType.Reverse:
                    ReverseLights.Add (l);
                    break;

                }
            }

            Car = GetComponent<CarController> ();

            //Initializing soft light switching.
            InitSoftSwitches (MainLights);
            InitSoftSwitches (ReverseLights);
            InitSoftSwitches (BrakeLights);
            InitSoftSwitches (LeftTurnLights);
            InitSoftSwitches (RightTurnLights);
        }

        private void Update ()
        {
            bool carInBrake = Car != null && Car.CurrentBrake > 0;
            if (InBrake != carInBrake)
            {
                InBrake = carInBrake;
                SetActiveBrake (InBrake);
            }
        }

        /// <summary>
        /// Initiates soft switching of the light as needed.
        /// </summary>
        void InitSoftSwitches (List<LightObject> lights)
        {
            foreach (var light in lights)
            {
                light.TryInitSoftSwitch ();
            }
        }

        /// <summary>
        /// Reverse light switch logic.
        /// </summary>
        public void OnChangeGear (int gear)
        {
            SetActiveReverse (gear < 0);
        }

        public void SwithOffAllLights ()
        {
            SetActiveMainLights (false);
            SetActiveBrake (false);
            SetActiveReverse (false);
            TurnsEnable (TurnsStates.Off);
        }

        /// <summary>
        /// Main light switch.
        /// </summary>
        public void SwitchMainLights ()
        {
            if (MainLights.Count > 0)
            {
                MainLightsIsOn = !MainLightsIsOn;
                SetActiveMainLights (MainLightsIsOn);
            }
        }

        public void SetActiveMainLights (bool value)
        {
            MainLights.ForEach (l => l.Switch (value));

            OnSetActiveLight.SafeInvoke (CarLightType.Main, value);

            if (AdditionalLighting)
            {
                AdditionalLighting.SetActiveMainLights (value);
            }
        }

        public void SetActiveBrake (bool value)
        {
            BrakeLights.ForEach (l => l.Switch (value));

            OnSetActiveLight.SafeInvoke (CarLightType.Brake, value);

            if (AdditionalLighting)
            {
                AdditionalLighting.SetActiveBrake (value);
            }
        }

        public void SetActiveReverse (bool value)
        {
            ReverseLights.ForEach (l => l.Switch (value));

            OnSetActiveLight.SafeInvoke (CarLightType.Reverse, value);

            if (AdditionalLighting)
            {
                AdditionalLighting.SetActiveReverse (value);
            }
        }

        /// <summary>
        /// Turns lights switch logic.
        /// </summary>
        public void TurnsEnable (TurnsStates state)
        {
            TurnsDisable ();

            if (CurrentTurnsState != state)
            {
                CurrentTurnsState = state;
                TurnsCotoutine = StartCoroutine (DoTurnsEnable (CurrentTurnsState));
            }
            else
            {
                switch (CurrentTurnsState)
                {
                    case TurnsStates.Left: OnSetActiveLight.SafeInvoke (CarLightType.TurnLeft, false); break;
                    case TurnsStates.Right: OnSetActiveLight.SafeInvoke (CarLightType.TurnRight, false); break;
                    case TurnsStates.Alarm: OnSetActiveLight.SafeInvoke (CarLightType.TurnLeft | CarLightType.TurnRight, false); break;
                }

                CurrentTurnsState = TurnsStates.Off;
            }

            if (AdditionalLighting)
            {
                AdditionalLighting.TurnsEnable (state);
            }
        }

        /// <summary>
        /// Turn off blinking of turn signals.
        /// </summary>
        void TurnsDisable ()
        {
            if (TurnsCotoutine != null)
            {
                StopCoroutine (TurnsCotoutine);
            }
            ActiveTurns.ForEach (l => l.Switch (false));
        }

        /// <summary>
        /// Turn signals IEnumerator.
        /// </summary>
        IEnumerator DoTurnsEnable (TurnsStates state)
        {
            ActiveTurns = new List<LightObject> ();

            switch (state)
            {
                case TurnsStates.Left:
                ActiveTurns = LeftTurnLights;
                OnSetActiveLight.SafeInvoke (CarLightType.TurnLeft, true);
                break;

                case TurnsStates.Right:
                ActiveTurns = RightTurnLights;
                OnSetActiveLight.SafeInvoke (CarLightType.TurnRight, true);
                break;

                case TurnsStates.Alarm:
                ActiveTurns.AddRange (LeftTurnLights);
                ActiveTurns.AddRange (RightTurnLights);
                OnSetActiveLight.SafeInvoke (CarLightType.TurnLeft | CarLightType.TurnRight, true);
                break;
            }

            //Infinite cycle of switching on and off.
            while (true)
            {
                ActiveTurns.ForEach (l => l.Switch (true));
                yield return new WaitForSeconds (TurnsSwitchHalfRepeatTime);
                ActiveTurns.ForEach (l => l.Switch (false));
                yield return new WaitForSeconds (TurnsSwitchHalfRepeatTime);
            }
        }
    }

    public enum TurnsStates
    {
        Off,
        Left,
        Right,
        Alarm
    }

    public enum CarLightType
    {
        Main,
        Brake,
        TurnLeft,
        TurnRight,
        Reverse,
    }
}
