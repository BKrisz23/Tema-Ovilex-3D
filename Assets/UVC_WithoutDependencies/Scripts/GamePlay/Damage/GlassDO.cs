using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace PG
{
    /// <summary>
    /// Damaged glass object, used for glass and light.
    /// </summary>
    public class GlassDO :DamageableObject
    {
        public Material BrokenGlassMaterial;        //Material applied to the object after complete damage, if this field is null then the object will not be visible after destruction.
        public ParticleSystem ShardsParticles;      //Particle system, reproduced at the moment of destruction.

        public int GlassMaterialIndex;              //Material index if the mesh has multiple materials.
        public AudioClip DestroyClip;               //Sound reproduced when destroyed.

        protected Renderer Renderer;
        protected Material[] Materials;
        protected Material DefaultGlassMaterial;

        public override void InitDamageObject ()
        {
            if (!IsInited)
            {
                base.InitDamageObject ();
                ShardsParticles.SetActive (false);
                Renderer = GetComponent<Renderer> ();
                if (Renderer)
                {
                    Materials = Renderer.materials;
                    DefaultGlassMaterial = Materials[GlassMaterialIndex];
                }
            }
        }

        public override void DoDeath ()
        {
            base.DoDeath ();
            if (Renderer)
            {
                if (BrokenGlassMaterial)
                {
                    Materials[GlassMaterialIndex] = BrokenGlassMaterial;
                    Renderer.materials = Materials;
                }
                else
                {
                    Renderer.enabled = false;
                }
            }

            if (ShardsParticles)
            {
                ShardsParticles.SetActive (true);
                ShardsParticles.Play ();
            }

            if (DestroyClip)
            {
                AudioSource.PlayClipAtPoint (DestroyClip, transform.position);
            }
        }
    }
}
