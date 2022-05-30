using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    [System.Serializable]
    public class GroundConfig
    {
        public string Caption;                  //Field for easy editing

        public AudioClip IdleAudioClip;
        public AudioClip SlipAudioClip;

        public ParticleSystem IdleParticles;    //Particle system works by simply driving on the surface.
        public ParticleSystem SlipParticles;    //Particle system that works when sliding on a surface.
        public bool TemperatureDependent;       //Dependence of particle system operation on tire temperature.
        public bool SpeedDependent;             //Dependence of particle system operation on the speed of the car.

        public float WheelStiffness;            //Wheel friction multiplier.
    }

    /// <summary>
    /// Abstract class for implementing the GetGroundConfig method
    /// </summary>
    public abstract class IGroundEntity :MonoBehaviour
    {
        public abstract GroundConfig GetGroundConfig (Vector3 position);
    }
}
