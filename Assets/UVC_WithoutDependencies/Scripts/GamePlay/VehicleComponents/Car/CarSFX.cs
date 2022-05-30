using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PG
{
    /// <summary>
    /// Sound effects, using FMOD.
    /// </summary>
    public class CarSFX :VehicleSFX
    {
        [Header("CarSFX")]

#pragma warning disable 0649

        [SerializeField] AudioSource EngineSource;                  //Engine source.
        [SerializeField] float MinEnginePitch = 0.5f;
        [SerializeField] float MaxEnginePitch = 1.5f;

        [SerializeField] AudioSource TurboSource;
        [SerializeField] AudioClip TurboBlowOffClip;
        [SerializeField] float MaxBlowOffVolume = 0.5f;
        [SerializeField] float MinTimeBetweenBlowOffSounds = 1;
        [SerializeField] float MaxTurboVolume = 0.5f;
        [SerializeField] float MinTurboPith = 0.5f;
        [SerializeField] float MaxTurboPith = 1.5f;

        [SerializeField] AudioSource BoostSource;

        [SerializeField] List<AudioClip> BackFireClips;

        [SerializeField] AudioSource OtherEffectsSource;

#pragma warning restore 0649

        CarController Car;
        float LastBlowOffTime;

        protected override void Start ()
        {
            base.Start ();

            Car = Vehicle as CarController;

            if (Car == null)
            {
                Debug.LogErrorFormat ("[{0}] CarSFX without CarController in parent", name);
                enabled = false;
                return;
            }

            if (!Car.Engine.EnableBoost && BoostSource)
            {
                BoostSource.Stop ();
            }

            if (!Car.Engine.EnableTurbo && TurboSource)
            {
                TurboSource.Stop ();
            }

            UpdateTurbo ();
            UpdateBoost ();

            Car.BackFireAction += OnBackFire;
        }


        protected override void Update ()
        {
            base.Update ();
            UpdateTurbo ();
            UpdateBoost ();

            // Engine sound logic.
            EngineSource.pitch = Mathf.Lerp (MinEnginePitch, MaxEnginePitch, (Car.EngineRPM - Car.MinRPM) / (Car.MaxRPM - Car.MinRPM));
        }

        //Additional turbo sound
        void UpdateTurbo ()
        {
            if (Car.Engine.EnableTurbo && TurboSource)
            {
                TurboSource.volume = Mathf.Lerp (0, MaxTurboVolume, Car.CurrentTurbo);
                TurboSource.pitch = Mathf.Lerp (MinTurboPith, MaxTurboPith, Car.CurrentTurbo);
                if (Car.CurrentTurbo > 0.2f && (Car.CurrentAcceleration < 0.2f || Car.InChangeGear) && ((Time.realtimeSinceStartup - LastBlowOffTime) > MinTimeBetweenBlowOffSounds))
                {
                    OtherEffectsSource.PlayOneShot (TurboBlowOffClip, Car.CurrentTurbo * MaxBlowOffVolume);
                    LastBlowOffTime = Time.realtimeSinceStartup;
                }
            }
        }

        //Additional boost sound
        void UpdateBoost ()
        {
            if (Car.Engine.EnableBoost && BoostSource)
            {
                if (Car.InBoost && !BoostSource.isPlaying)
                {
                    BoostSource.Play ();
                }
                if (!Car.InBoost && BoostSource.isPlaying)
                {
                    BoostSource.Stop ();
                }
            }
        }

        void OnBackFire ()
        {
            if (BackFireClips != null && BackFireClips.Count > 0)
            {
                OtherEffectsSource.PlayOneShot (BackFireClips[Random.Range (0, BackFireClips.Count - 1)]);
            }
        }
    }
}
