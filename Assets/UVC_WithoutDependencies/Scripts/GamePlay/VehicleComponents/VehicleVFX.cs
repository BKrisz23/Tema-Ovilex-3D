using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace PG
{
    /// <summary>
    /// Visual effects. Tire smoke, tire marks, etc.
    /// </summary>
    public class VehicleVFX :MonoBehaviour
    {
        [Header("VehicleVFX")]
#pragma warning disable 0649

        [SerializeField] float MinTimeBetweenCollisions = 0.1f;
        [SerializeField] ParticleSystem DefaultCollisionParticles;
        [SerializeField] List<CollissionParticles> CollisionParticlesList = new List<CollissionParticles>();

        [SerializeField] TrailRenderer TrailRef;                    //Trail ref, The lifetime of the tracks is configured in it.

#pragma warning restore 0649

        protected VehicleController Vehicle;
        public Dictionary<Wheel, TrailRenderer> ActiveTrails { get; private set; }

        Queue<TrailRenderer> FreeTrails = new Queue<TrailRenderer>(); //Free trail pool

        const float OffsetHitHeightForTrail = 0.05f;

        Transform ParentForEffects;         //Parent object for effects, created in the scene without a parent (Does not move).

        float LastCollisionTime;

        protected virtual void Awake ()
        {
            TrailRef.gameObject.SetActive (false);
            Vehicle = GetComponentInParent<VehicleController> ();

            if (Vehicle == null)
            {
                Debug.LogErrorFormat ("[{0}] VehicleVFX without VehicleController in parent", name);
                enabled = false;
                return;
            }

            Vehicle.ResetVehicleAction += ResetAllTrails;
            Vehicle.CollisionAction += PlayCollisionParticles;
            Vehicle.CollisionStayAction += CollisionStay;

            ParentForEffects = new GameObject (string.Format ("Effects for {0}", Vehicle.name)).transform;

            ActiveTrails = new Dictionary<Wheel, TrailRenderer> ();
            foreach (var wheel in Vehicle.Wheels)
            {
                ActiveTrails.Add (wheel, null);
            }
        }

        protected virtual void Update ()
        {
            EmitParams emitParams;
            float rndValue = UnityEngine.Random.Range(0, 1f);
            for (int i = 0; i < Vehicle.Wheels.Length; i++)
            {
                var wheel = Vehicle.Wheels[i];
                var groundConfig = wheel.CurrentGroundConfig;
                var hasSlip = wheel.HasForwardSlip || wheel.HasSideSlip;

                //Emit particle.
                if (!wheel.IsDead && Vehicle.VehicleIsVisible && groundConfig != null)
                {
                    var particles = hasSlip? groundConfig.SlipParticles: groundConfig.IdleParticles;
                    if (particles)
                    {
                        float sizeAndLifeTimeMultiplier =
                            (groundConfig.TemperatureDependent? wheel.WheelTemperature: 1) *
                            (groundConfig.SpeedDependent && !hasSlip? (Vehicle.CurrentSpeed / 30).Clamp(): 1) * rndValue;

                        var point = wheel.transform.position;
                        point.y = wheel.GetHit.point.y;

                        var particleVelocity = -wheel.GetHit.forwardDir * wheel.GetHit.forwardSlip;
                        particleVelocity += wheel.GetHit.sidewaysDir * wheel.GetHit.sidewaysSlip;

                        emitParams = new EmitParams ();

                        emitParams.position = point;
                        emitParams.velocity = particleVelocity;
                        emitParams.startSize = Mathf.Max (1f, particles.main.startSize.constant * sizeAndLifeTimeMultiplier);
                        emitParams.startLifetime = particles.main.startLifetime.constant * sizeAndLifeTimeMultiplier;
                        emitParams.startColor = particles.main.startColor.color;

                        particles.Emit (emitParams, 1);
                    }
                }

                //Emit trail
                UpdateTrail (wheel, !wheel.IsDead && !wheel.StopEmitFX && wheel.IsGrounded && hasSlip);
            }
        }

        #region Trails
        public void UpdateTrail (Wheel wheel, bool hasSlip)
        {
            var trail = ActiveTrails[wheel];

            if (hasSlip)
            {
                if (trail == null)
                {
                    //Get free or create trail.

                    trail = GetTrail (wheel.WheelView.position + (wheel.transform.up * (-wheel.Radius + OffsetHitHeightForTrail)));
                    trail.transform.SetParent (wheel.transform);
                    ActiveTrails[wheel] = trail;
                }
                else
                {
                    //Move the trail to the desired position
                    trail.transform.position = wheel.WheelView.position + (wheel.transform.up * (-wheel.Radius + OffsetHitHeightForTrail));
                }
            }
            else if (ActiveTrails[wheel] != null)
            {
                //Set trail as free.
                SetTrailAsFree (trail);
                trail = null;
                ActiveTrails[wheel] = trail;
            }
        }

        void ResetAllTrails ()
        {
            TrailRenderer trail;
            for (int i = 0; i < Vehicle.Wheels.Length; i++)
            {
                trail = ActiveTrails[Vehicle.Wheels[i]];
                if (trail)
                {
                    SetTrailAsFree (trail);
                    trail = null;
                    ActiveTrails[Vehicle.Wheels[i]] = trail;
                }
            }
        }

        /// <summary>
        /// Get first free trail and set start position.
        /// </summary>
        public TrailRenderer GetTrail (Vector3 startPos)
        {
            TrailRenderer trail = null;
            if (FreeTrails.Count > 0)
            {
                trail = FreeTrails.Dequeue ();
            }
            else
            {
                trail = Instantiate (TrailRef, ParentForEffects);
            }

            trail.transform.position = startPos;
            trail.gameObject.SetActive (true);
            trail.Clear ();

            return trail;
        }

        /// <summary>
        /// Set trail as free and wait life time.
        /// </summary>
        public void SetTrailAsFree (TrailRenderer trail)
        {
            StartCoroutine (WaitVisibleTrail (trail));
        }

        /// <summary>
        /// The trail is considered busy until it disappeared.
        /// </summary>
        private IEnumerator WaitVisibleTrail (TrailRenderer trail)
        {
            trail.transform.SetParent (ParentForEffects);
            yield return new WaitForSeconds (trail.time);
            trail.Clear ();
            trail.gameObject.SetActive (false);
            FreeTrails.Enqueue (trail);
        }

        #endregion //Trails

        #region Collisions

        private void CollisionStay (VehicleController vehicle, Collision collision)
        {
            if (Vehicle.CurrentSpeed >= 1)
            {
                PlayCollisionParticles (vehicle, collision);
            }
        }

        public void PlayCollisionParticles (VehicleController vehicle, Collision collision)
        {
            if (!vehicle.VehicleIsVisible || collision == null || Time.time - LastCollisionTime < MinTimeBetweenCollisions)
            {
                return;
            }

            LastCollisionTime = Time.time;
            var magnitude = collision.relativeVelocity.magnitude * Vector3.Dot (collision.relativeVelocity.normalized, collision.contacts[0].normal).Abs();
            var particles = GetParticlesForCollision(collision.gameObject.layer, magnitude);

            for (int i = 0; i < collision.contacts.Length; i++)
            {
                particles.transform.position = collision.contacts[i].point;
                particles.Play (withChildren: true);
            }
        }

        public ParticleSystem GetParticlesForCollision (int layer, float collisionMagnitude)
        {
            for (int i = 0; i < CollisionParticlesList.Count; i++)
            {
                if (CollisionParticlesList[i].CollisionLayer.LayerInMask (layer) && collisionMagnitude >= CollisionParticlesList[i].MinMagnitudeCollision && collisionMagnitude < CollisionParticlesList[i].MaxMagnitudeCollision)
                {
                    return CollisionParticlesList[i].Particles;
                }
            }

            return DefaultCollisionParticles;
        }

        [System.Serializable]
        public struct CollissionParticles
        {
            public ParticleSystem Particles;
            public LayerMask CollisionLayer;
            public float MinMagnitudeCollision;
            public float MaxMagnitudeCollision;
        }

        #endregion //Collisions
    }
}
