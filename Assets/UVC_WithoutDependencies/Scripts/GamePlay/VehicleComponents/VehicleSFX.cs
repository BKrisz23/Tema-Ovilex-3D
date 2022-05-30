using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Sound effects, using FMOD.
    /// </summary>
    public class VehicleSFX :MonoBehaviour
    {
        [Header("VehicleSFX")]

#pragma warning disable 0649

        [SerializeField] AudioSource WheelsEffectSourceRef;                                //Wheel emitter, for playing slip sounds.

        [SerializeField] float MinTimeBetweenCollisions = 0.1f;
        [SerializeField] float DefaultMagnitudeDivider = 20;                                //default divider to calculate collision volume.
        [SerializeField] AudioClip DefaultCollisionClip;                                    //Event playable if the desired one was not found.
        [SerializeField] List<ColissionEvent> CollisionEvents = new List<ColissionEvent>();

        [Space(10)]
        [SerializeField] AudioSource FrictionEffectSourceRef;
        [SerializeField] float PlayFrictionTime = 0.5f;
        [SerializeField] AudioClip DefaultFrictionClip;                             //Event playable if the desired one was not found.
        [SerializeField] List<ColissionEvent> FrictionEvents = new List<ColissionEvent>();

#pragma warning restore 0649

        Dictionary<GroundConfig, WheelSoundData> WheelSounds = new Dictionary<GroundConfig, WheelSoundData>();              //Dictionary for playing multiple wheel sounds at the same time.\
        Dictionary<AudioClip, FrictionSoundData> FrictionSounds = new Dictionary<AudioClip, FrictionSoundData>();     //Dictionary for playing multiple friction sounds at the same time.

        protected VehicleController Vehicle;
        AudioClip CurrentFrictionClip;
        float LastCollisionTime;

        protected virtual void Start ()
        {
            Vehicle = GetComponentInParent<VehicleController> ();

            if (Vehicle == null)
            {
                Debug.LogErrorFormat ("[{0}] VehicleSFX without VehicleController in parent", name);
                enabled = false;
                return;
            }

            //Subscribe to collisions.
            Vehicle.CollisionAction += PlayCollisionSound;
            Vehicle.CollisionStayAction += PlayCollisionStayAction;

            //Setting default values.
            WheelsEffectSourceRef.volume = 0;
            FrictionEffectSourceRef.volume = 0;

            FrictionSounds.Add (FrictionEffectSourceRef.clip, new FrictionSoundData () { Source = FrictionEffectSourceRef, LastFrictionTime = Time.time });
            FrictionEffectSourceRef.Stop ();
        }

        protected virtual void Update ()
        {
            //Wheels sounds logic.
            //Find the sound for each wheel.
            foreach (var wheel in Vehicle.Wheels)
            {
                if (wheel.IsDead)
                {
                    continue;
                }

                WheelSoundData sound = null;

                if (!WheelSounds.TryGetValue (wheel.CurrentGroundConfig, out sound))
                {
                    var source = WheelsEffectSourceRef.gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = WheelsEffectSourceRef.playOnAwake;
                    source.spatialBlend = WheelsEffectSourceRef.spatialBlend;
                    source.clip = wheel.CurrentGroundConfig.IdleAudioClip;
                    source.Stop ();
                    source.volume = 0;
                    sound = new WheelSoundData ()
                    {
                        Source = source
                    };
                    WheelSounds.Add (wheel.CurrentGroundConfig, sound);
                }

                sound.WheelsCount++;

                //Find the maximum slip for each sound.
                if (wheel.SlipNormalized > sound.Slip)
                {
                    sound.Slip = wheel.SlipNormalized;
                }
            }

            var speedNormalized = (Vehicle.CurrentSpeed / 30).Clamp();

            foreach (var sound in WheelSounds)
            {
                AudioClip clip;
                float targetVolume;

                if (sound.Value.Slip >= 0.4f)
                {
                    clip = sound.Key.SlipAudioClip;
                    targetVolume = sound.Value.Slip.Clamp();
                }
                else
                {
                    clip = sound.Key.IdleAudioClip;
                    targetVolume = (Vehicle.CurrentSpeed / 30).Clamp();
                }
                
                if (sound.Value.Source.clip != clip && clip != null)
                {
                    sound.Value.Source.clip = clip;
                }

                if (sound.Value.WheelsCount == 0 || speedNormalized == 0 || clip == null)
                {
                    targetVolume = 0;
                }

                //Passing parameters to sources.
                sound.Value.Source.volume = Mathf.Lerp (sound.Value.Source.volume, targetVolume, 10 * Time.deltaTime);
                sound.Value.Source.pitch = Mathf.Lerp(0.7f, 1.2f, sound.Value.Source.volume);
                
                sound.Value.Slip = 0;
                sound.Value.WheelsCount = 0;

                if (Mathf.Approximately (0, sound.Value.Source.volume) && sound.Value.Source.isPlaying)
                {
                    sound.Value.Source.Stop ();
                }
                else if (!Mathf.Approximately (0, sound.Value.Source.volume) && !sound.Value.Source.isPlaying)
                {
                    sound.Value.Source.Play ();
                }
            }

            FrictionSoundData soundData;
            foreach (var sound in FrictionSounds)
            {
                soundData = sound.Value;
                if (soundData.Source.isPlaying)
                {
                    var time = Time.time - soundData.LastFrictionTime;

                    if (time > PlayFrictionTime)
                    {
                        sound.Value.Source.pitch = 0;
                        sound.Value.Source.volume = 0;
                        soundData.Source.Stop ();
                    }
                    else
                    {
                        sound.Value.Source.pitch = Mathf.Lerp(0.4f, 1.2f, speedNormalized);
                        soundData.Source.volume = speedNormalized  * (1 - (time / soundData.LastFrictionTime));
                    }
                }
            }
        }

        private void OnDestroy ()
        {
            foreach(var soundKV in WheelSounds)
            {
                if (soundKV.Value.Source)
                {
                    soundKV.Value.Source.Stop ();
                }
            }

            foreach (var soundKV in FrictionSounds)
            {
                if (soundKV.Value.Source)
                {
                    soundKV.Value.Source.Stop ();
                }
            }
        }

        #region Collisions

        /// <summary>
        /// Play collision stay sound.
        /// </summary>
        public void PlayCollisionStayAction (VehicleController vehicle, Collision collision)
        {
            PlayFrictionSound (collision, collision.relativeVelocity.magnitude);
        }

        /// <summary>
        /// Play collision sound.
        /// </summary>
        public void PlayCollisionSound (VehicleController vehicle, Collision collision)
        {
            if (!vehicle.VehicleIsVisible || collision == null)
                return;

            var collisionLayer = collision.gameObject.layer;

            if (Time.time - LastCollisionTime < MinTimeBetweenCollisions)
            {
                return;
            }

            LastCollisionTime = Time.time;
            var collisionMagnitude = collision.relativeVelocity.magnitude;
            float magnitudeDivider;

            if (Vector3.Dot (collision.relativeVelocity.normalized, collision.contacts[0].normal) < 0.2f)
            {
                PlayFrictionSound (collision, collisionMagnitude);
                return;
            }

            var audioClip = GetClipForCollision (collisionLayer, collisionMagnitude, out magnitudeDivider);

            var volume = Mathf.Clamp01 (collisionMagnitude / magnitudeDivider.Clamp(0, 40));

            AudioSource.PlayClipAtPoint (audioClip, collision.contacts[0].point, volume);
        }

        void PlayFrictionSound (Collision collision, float magnitude)
        {
            if (Vehicle.CurrentSpeed >= 1)
            {
                CurrentFrictionClip = GetClipForFriction (collision.collider.gameObject.layer, magnitude);

                FrictionSoundData soundData;
                if (!FrictionSounds.TryGetValue(CurrentFrictionClip, out soundData))
                {
                    var source = FrictionEffectSourceRef.gameObject.AddComponent<AudioSource>();
                    source.clip = CurrentFrictionClip;

                    soundData = new FrictionSoundData () { Source = source };
                    FrictionSounds.Add (CurrentFrictionClip, soundData);
                }
                
                if (!soundData.Source.isPlaying)
                {
                    soundData.Source.Play ();
                }

                soundData.LastFrictionTime = Time.time;
            }
        }

        /// <summary>
        /// Search for the desired event based on the collision magnitude and the collision layer.
        /// </summary>
        /// <param name="layer">Collision layer.</param>
        /// <param name="collisionMagnitude">Collision magnitude.</param>
        /// <param name="magnitudeDivider">Divider to calculate collision volume.</param>
        AudioClip GetClipForCollision (int layer, float collisionMagnitude, out float magnitudeDivider)
        {
            for (int i = 0; i < CollisionEvents.Count; i++)
            {
                if (CollisionEvents[i].CollisionMask.LayerInMask(layer) && collisionMagnitude >= CollisionEvents[i].MinMagnitudeCollision && collisionMagnitude < CollisionEvents[i].MaxMagnitudeCollision)
                {
                    if (CollisionEvents[i].MaxMagnitudeCollision == float.PositiveInfinity)
                    {
                        magnitudeDivider = DefaultMagnitudeDivider;
                    }
                    else
                    {
                        magnitudeDivider = CollisionEvents[i].MaxMagnitudeCollision;
                    }
                    
                    return CollisionEvents[i].AudioClip;
                }
            }

            magnitudeDivider = DefaultMagnitudeDivider;
            return DefaultCollisionClip;
        }

        /// <summary>
        /// Search for the desired event based on the friction magnitude and the collision layer.
        /// </summary>
        /// <param name="layer">Collision layer.</param>
        /// <param name="collisionMagnitude">Collision magnitude.</param>
        AudioClip GetClipForFriction (int layer, float collisionMagnitude)
        {
            for (int i = 0; i < FrictionEvents.Count; i++)
            {
                if (FrictionEvents[i].CollisionMask.LayerInMask(layer) && collisionMagnitude >= FrictionEvents[i].MinMagnitudeCollision && collisionMagnitude < FrictionEvents[i].MaxMagnitudeCollision)
                {
                    return FrictionEvents[i].AudioClip;
                }
            }

            return DefaultFrictionClip;
        }

        #endregion //Collisions

        [System.Serializable]
        public struct ColissionEvent
        {
            public AudioClip AudioClip;
            public LayerMask CollisionMask;
            public float MinMagnitudeCollision;
            public float MaxMagnitudeCollision;
        }

        public class FrictionSoundData
        {
            public AudioSource Source;
            public float LastFrictionTime;
        }

        public class WheelSoundData
        {
            public AudioSource Source;
            public float Slip;
            public int WheelsCount;
        }
    }
}
