using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Visual effects. Tire smoke, tire marks, etc.
    /// </summary>
    public class CarVFX :VehicleVFX
    {
        [Header("CarVFX")]

        public List<ParticleSystem> ExhaustParticles = new List<ParticleSystem>();
        public List<ParticleSystem> BackFireParticles = new List<ParticleSystem>();
        public List<ParticleSystem> BoostParticles = new List<ParticleSystem>();

        [Header("Car engine")]

        public ParticleSystem EngineHealth75Particles;
        public ParticleSystem EngineHealth50Particles;
        public ParticleSystem EngineHealth25Particles;

        CarController Car;
        protected override void Awake ()
        {
            base.Awake ();

            Car = Vehicle as CarController;

            if (Car == null)
            {
                Debug.LogErrorFormat ("[{0}] VehicleVFX without VehicleController in parent", name);
                enabled = false;
                return;
            }

            if (EngineHealth75Particles)
            {
                EngineHealth75Particles.gameObject.SetActive (false);
            }
            if (EngineHealth75Particles)
            {
                EngineHealth50Particles.gameObject.SetActive (false);
            }
            if (EngineHealth75Particles)
            {
                EngineHealth25Particles.gameObject.SetActive (false);
            }

            if (Car.EngineDamageableObject)
            {
                Car.EngineDamageableObject.OnChangeHealthAction += OnChangeHealthEngine;
            }

            Car.BackFireAction += OnBackFire;
        }

        protected override void Update ()
        {
            base.Update ();

            if (ExhaustParticles.Count > 0 && Car.EngineLoad >= 0.1f)
            {
                for (int i = 0; i < ExhaustParticles.Count; i++)
                {
                    ExhaustParticles[i].Emit (1);
                }
            }
            if (Car.InBoost && Car.CurrentAcceleration > 0)
            {
                for (int i = 0; i < BoostParticles.Count; i++)
                {
                    BoostParticles[i].Emit (1);
                }
            }
        }

        void OnChangeHealthEngine (float changeValue)
        {
            if (EngineHealth75Particles)
            {
                EngineHealth75Particles.gameObject.SetActive (Car.EngineDamageableObject.HealthPercent > 0.5 &&
                    Car.EngineDamageableObject.HealthPercent <= 0.75);
            }
            if (EngineHealth50Particles)
            {
                EngineHealth50Particles.gameObject.SetActive (Car.EngineDamageableObject.HealthPercent <= 0.5);
            }
            if (EngineHealth25Particles)
            {
                EngineHealth25Particles.gameObject.SetActive (Car.EngineDamageableObject.HealthPercent <= 0.25);
            }
        }

        void OnBackFire ()
        {
            foreach (var particles in BackFireParticles)
            {
                particles.Emit (1);
            }
        }
    }
}
