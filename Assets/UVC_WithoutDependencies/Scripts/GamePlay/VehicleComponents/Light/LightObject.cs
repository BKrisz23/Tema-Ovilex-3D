using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Component responsible for all light signals.
    /// </summary>
    public class LightObject :GlassDO
    {
        public CarLightType CarLightType;
        public Light LightGO;
        public Material OnLightMaterial;                //Material with glow, used for soft and hard switching.

        [Header("Soft Switch settings")]
        public bool IsSoftSwitch;
        public float OnSwitchSpeed = 10f;
        public float OffSwitchSpeed = 2f;
        public float Intensity = 2f;                    //Maximum glow intensity.

        [Header("Main settings")]
        public bool EnableOnStart;

        MaterialPropertyBlock MaterialBlock;
        Material MaterialForSoftSwitch;
        Animator LightsAnimator;

        //IDs for accessing properties, so as not to use the string (Optimization).
        int EmissionColorPropertyID;
        int AnimatorLightIsOnID;
        Coroutine SoftSwitchCoroutine;

        public bool LightIsOn { get; private set; }

        /// <summary>
        /// Initialize soft switch if the IsSoftSwitch flag is set.
        /// </summary>
        public void TryInitSoftSwitch ()
        {
            if (!IsSoftSwitch)
            {
                return;
            }

            if (!IsInited)
            {
                InitDamageObject ();
            }

            if (Renderer)
            {
                MaterialForSoftSwitch = OnLightMaterial;
                Materials[GlassMaterialIndex] = OnLightMaterial;
                Renderer.materials = Materials;
            }
        }

        public override void InitDamageObject ()
        {
            base.InitDamageObject ();

            LightsAnimator = GetComponent<Animator> ();

            EmissionColorPropertyID = Shader.PropertyToID ("_EmissionColor");
            AnimatorLightIsOnID = Animator.StringToHash ("LightIsOn");
            MaterialBlock = new MaterialPropertyBlock ();
        }

        void Start ()
        {
            LightIsOn = !EnableOnStart;
            Switch (EnableOnStart, forceSwitch: true);
        }

        /// <summary>
        /// Switch light LightIsOn =! LightIsOn.
        /// </summary>
        public void Switch ()
        {
            Switch (!LightIsOn, forceSwitch: true);
        }

        /// <summary>
        /// Switch with parameters.
        /// </summary>
        public void Switch (bool value, bool forceSwitch = false)
        {
            value &= !IsDead;

            if (LightIsOn == value)
            {
                return;
            }

            LightIsOn = value;

            if (Renderer)
            {
                if (IsSoftSwitch)
                {
                    if (SoftSwitchCoroutine != null)
                    {
                        StopCoroutine (SoftSwitchCoroutine);
                    }

                    if (MaterialForSoftSwitch != null)
                    {
                        SoftSwitchCoroutine = StartCoroutine (SoftSwitch (LightIsOn, forceSwitch));
                    }
                }
                else if (!IsDead)
                {
                    HardSwitch ();
                }
            }

            if (LightGO)
            {
                LightGO.SetActive (value);
            }

            //The animator is needed to turn on the headlights such as those of the PG86 or turn on the off the gameobject.
            if (LightsAnimator != null && !IsDead)
            {
                LightsAnimator.SetBool (AnimatorLightIsOnID, LightIsOn);
            }
        }

        IEnumerator SoftSwitch (bool value, bool forceSwitch = false)
        {
            //Calculation of the start and target Intensity glow.
            Color targetColor = (value? Color.white * Intensity: Color.black);
            Color startColor = (value? Color.black * Intensity: Color.white);
            var speed = value? OnSwitchSpeed: OffSwitchSpeed;
            float timer = 0;

            if (!forceSwitch)
            {
                while (timer < 1)
                {
                    var color = Color.Lerp (startColor, targetColor, timer);
                    MaterialBlock.SetColor (EmissionColorPropertyID, color);
                    Renderer.SetPropertyBlock (MaterialBlock);
                    timer += speed * Time.deltaTime;
                    yield return null;
                }
            }

            //Used MaterialBlock since all light objects can use the same material.
            MaterialBlock.SetColor (EmissionColorPropertyID, targetColor);
            Renderer.SetPropertyBlock (MaterialBlock);

            SoftSwitchCoroutine = null;
        }

        /// <summary>
        /// Just material change, switching on/off occurs in one frame.
        /// </summary>
        void HardSwitch ()
        {
            if (LightIsOn)
            {
                Materials[GlassMaterialIndex] = OnLightMaterial;
            }
            else
            {
                Materials[GlassMaterialIndex] = DefaultGlassMaterial;
            }

            Renderer.materials = Materials;
        }

        public override void DoDeath ()
        {
            base.DoDeath ();

            if (LightsAnimator)
            {
                LightsAnimator.SetBool ("IsBroken", true);
            }

            if (LightGO)
            {
                LightGO.SetActive (false);
            }
        }
    }
}
