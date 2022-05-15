//______________________________________________//
//___________Realistic Engine Sounds____________//
//______________________________________________//
//_______Copyright © 2019 Yugel Mobile__________//
//______________________________________________//
//_________ http://mobile.yugel.net/ ___________//
//______________________________________________//
//________ http://fb.com/yugelmobile/ __________//
//______________________________________________//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class RealisticEngineSound_mobile : MonoBehaviour
{
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    public AudioMixerGroup audioMixer;
    public bool destroyAudioSources = true; // enable or disable: destroy unused or too far away audio sources that are currently can't be heard

    public float engineCurrentRPM = 0.0f;
    public float maxRPMLimit = 7000;
    [Range(0.0f, 5.0f)]
    public float dopplerLevel = 1;
    [Range(0.0f, 1.0f)]
    [HideInInspector] // remove this line if you want to set custom values for spatialBlend
    public float spatialBlend = 1f; // this value should always be 1. If you want custom value, remove spatialBlend = 1f; line from Start()
    [Range(0.1f, 2.0f)]
    public float pitchMultiplier = 1.0f; // pitch value multiplier
    [Range(0.0f, 0.25f)]
    public float optimisationLevel = 0.01f; // audio source with volume level below this value will be destroyed
    public AudioRolloffMode audioRolloffMode = AudioRolloffMode.Custom;
    // play sounds within this distances
    public float minDistance = 1; // within the minimum distance the audiosources will cease to grow louder in volume
    public float maxDistance = 50; // maxDistance is the distance a sound stops attenuating at
    // other settings
    public bool isReversing = false; // is car in reverse gear - only if reverse gear is enabled
    public bool useRPMLimit = true; // enable rpm limit at maximum rpm
    public bool enableReverseGear = false; // enable this if you would like to use reverse sound in reverse gears

    // hiden public stuff
    [HideInInspector]
    public float carCurrentSpeed; // needed for straight cut gearbox script
    [HideInInspector]
    public float carMaxSpeed; // needed for straight cut gearbox script
    [HideInInspector]
    public bool isShifting = false; // needed for shifting sounds script
    //[HideInInspector]
    public bool gasPedalPressing = false; // needed for turbo script

    // idle clip sound
    public AudioClip idleClip;
    public AnimationCurve idleVolCurve;
    public AnimationCurve idlePitchCurve;
    // low rpm clip sounds
    public AudioClip lowOnClip;
    public AnimationCurve lowVolCurve;
    public AnimationCurve lowPitchCurve;
    // medium rpm clip sounds
    public AudioClip medOnClip;
    public AnimationCurve medVolCurve;
    public AnimationCurve medPitchCurve;
    // high rpm clip sounds
    public AudioClip highOnClip;
    public AnimationCurve highVolCurve;
    public AnimationCurve highPitchCurve;
    // maximum rpm clip sound - if RPM limit is enabled
    public AudioClip maxRPMClip;
    public AnimationCurve maxRPMVolCurve;
    // reverse gear clip sound - if reverse gear is enabled
    public AudioClip reversingClip;
    public AnimationCurve reversingVolCurve;
    public AnimationCurve reversingPitchCurve;

    // idle audio source
    private AudioSource engineIdle;

    // low rpm audio sources
    private AudioSource lowOn;

    // medium rpm audio sources
    private AudioSource medOn;

    // high rpm audio sources
    private AudioSource highOn;

    //maximum rpm audio source
    private AudioSource maxRPM;

    // reverse gear audio source
    private AudioSource reversing;

    //private settings
    private float clipsValue;

    // get camera for optimisation
    public Camera mainCamera;
    [HideInInspector]
    public bool isCameraNear; // tells is the camera near

    private WaitForSeconds _wait;
    private bool alreadyDestroyed = false; // prevent asking to destroy already destroyed audio sources when camera is far away

    private void Start()
    {
        spatialBlend = 1f; // remove this line if you want to set custom values for spatialBlend
        _wait = new WaitForSeconds(0.15f); // setup wait for secconds
        // if res scipts are far away from camera do not need to create audio sources, this audio sources already won't be heard because of the distance
        if (mainCamera == null)
            mainCamera = Camera.main;

        clipsValue = engineCurrentRPM / maxRPMLimit; // calculate % percentage of rpm
        if (mainCamera != null)
        {
            if (Vector3.Distance(mainCamera.transform.position, gameObject.transform.position) <= maxDistance)
            {
                isCameraNear = true;
            }
        }
    }
    private void Update()
    {
        if (isCameraNear)
        {
            clipsValue = engineCurrentRPM / maxRPMLimit; // calculate % percentage of rpm

            // idle
            if (idleClip != null) // check is idle clip exists
            {
                if (engineIdle == null)
                    if (idleVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateIdle();
                if (engineIdle != null)
                {
                    engineIdle.volume = idleVolCurve.Evaluate(clipsValue) * masterVolume;
                    engineIdle.pitch = idlePitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
                if (engineIdle != null)
                    if (destroyAudioSources)
                        if (engineIdle.volume < optimisationLevel)
                            Destroy(engineIdle);
            }
            //
            // low rpm
            if (lowOnClip != null) // check is lowOn clip exists
            {
                if (lowOn == null)
                    if (lowVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateLowOn();
                if (lowOn != null)
                {
                    lowOn.volume = lowVolCurve.Evaluate(clipsValue) * masterVolume;
                    lowOn.pitch = lowPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
            }
            if (lowOn != null) // destroy lowOn audio source when needed and destroying is enabled
            {
                if (destroyAudioSources)
                    if (lowOn.volume < optimisationLevel)
                        Destroy(lowOn);
            }
            //
            // medium rpm
            if (medOnClip != null) // check is medOn clip exists
            {
                if (medOn == null)
                    if (medVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateMedOn();
                if (medOn != null)
                {
                    medOn.volume = medVolCurve.Evaluate(clipsValue) * masterVolume;
                    medOn.pitch = medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
                if (medOn != null) // destroy medOn audio source when needed and destroying is enabled
                {
                    if (destroyAudioSources)
                        if (medOn.volume < optimisationLevel)
                            Destroy(medOn);
                }
            }
            //
            // high rpm
            if (highOnClip != null) // check is highOn clip exists
            {
                if (highOn == null)
                    if (highVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateHighOn();
                if (highOn != null)
                {
                    if (maxRPM != null)
                    {
                        if (maxRPM.volume < 0.95f)
                            highOn.volume = highVolCurve.Evaluate(clipsValue) * masterVolume;
                        else
                            highOn.volume = (highVolCurve.Evaluate(clipsValue) * masterVolume) / 3.3f; // max rpm is playing
                    }
                    else
                    {
                        highOn.volume = highVolCurve.Evaluate(clipsValue) * masterVolume;
                    }
                    highOn.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
                if (highOn != null) // destroy highOn audio source when needed and destroying is enabled
                {
                    if (destroyAudioSources)
                        if (highOn.volume < optimisationLevel)
                            Destroy(highOn);
                }
            }
            //
            // rpm limiting
            if (maxRPMClip != null) // check is maxRPM clip exists
            {
                if (useRPMLimit) // if rpm limit is enabled, create audio source for it
                {
                    if (maxRPMVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                    {
                        if (maxRPM == null)
                            CreateRPMLimit();
                        else
                        {
                            maxRPM.volume = maxRPMVolCurve.Evaluate(clipsValue) * masterVolume;
                            maxRPM.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                        }
                    }
                    else
                    {
                        if (maxRPM != null)
                            if (maxRPM.volume > maxRPMVolCurve.Evaluate(clipsValue))
                                maxRPM.volume = maxRPMVolCurve.Evaluate(clipsValue); // update maxRPM volume with current rpm's value

                        if (destroyAudioSources) // destroy maxRPM audio source when needed and destroying is enabled
                            if (maxRPM != null)
                                Destroy(maxRPM);
                    }
                }
                else // rev limiter is disabled, remove it's audio source
                {
                    if (maxRPM != null)
                        Destroy(maxRPM);
                }
            }
            else // maxRPM audio clip is missing 
            {
                useRPMLimit = false;
            }
            //
            // reversing gear sound
            if (enableReverseGear)
            {
                if (reversingClip != null) // check is reversingClip exists
                {
                    if (isReversing)
                    {
                        if (reversingVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        {
                            if (reversing == null)
                                CreateReverse();
                            else
                            {
                                // set reversing sound to setted settings
                                reversing.volume = reversingVolCurve.Evaluate(carCurrentSpeed / 55) * masterVolume; // max speed in reverse is around 55
                                reversing.pitch = reversingPitchCurve.Evaluate(carCurrentSpeed / 55);
                            }
                        }
                        else
                        {
                            if (destroyAudioSources) // destroy reversing fx audio source when needed and destroying is enabled
                                if (reversing != null)
                                    Destroy(reversing);
                        }
                    }
                    else
                    {
                        if (reversing != null) // destroy reversing fx audio source because it's not needed any more
                            Destroy(reversing);
                    }
                }
                else
                {
                    isReversing = false;
                    enableReverseGear = false; // disable reversing sound because there is no audio clip for it
                }
            }
            else
            {
                if (isReversing != false) // reversing sound fx is being disabled
                    isReversing = false;
                if (reversing != null) // destroy reversing fx audio source because it's not needed any more
                    Destroy(reversing);
            }
        }
        else
        {
            if (!alreadyDestroyed) // stop asking for destroy if it already done
            {
                DestroyAll(); // camera is far away, destroy all audio sources
                alreadyDestroyed = true; // destroy is done stop asking for destroy
            }
        }
    }
    private void FixedUpdate()
    {
        if (mainCamera != null)
        {
            if (Vector3.Distance(mainCamera.transform.position, gameObject.transform.position) > maxDistance)
            {
                isCameraNear = false;
            }
            else
            {
                isCameraNear = true;
                if (alreadyDestroyed) // reset stop asking for destroy if it already done
                    alreadyDestroyed = false;
            }

            if (!enableReverseGear)
            {
                if (reversing != null)
                {
                    Destroy(reversing); // reversing sound disabled on runtime, destroy it's audio source
                }
            }
            // rpm limiting
            if (!useRPMLimit) // rpm limit is disabled on runtime, destroy it's audio source
            {
                if (maxRPM != null)
                    Destroy(maxRPM);
            }
        }
    }
    private void OnEnable() // recreate all audio sources if Realistic Engine Sound's script is reEnabled
    {
        StartCoroutine(WaitForStart());
    }
    private void OnDisable() // destroy audio sources if Realistic Engine Sound's script is disabled
    {
        DestroyAll();
    }
    private void DestroyAll()
    {
        if (engineIdle != null)
            Destroy(engineIdle);
        if (lowOn != null)
            Destroy(lowOn);
        if (medOn != null)
            Destroy(medOn);
        if (highOn != null)
            Destroy(highOn);

        if (useRPMLimit)
        {
            if (maxRPM != null)
                Destroy(maxRPM);
        }
        if (enableReverseGear)
        {
            if (reversing != null)
                Destroy(reversing);
        }
    }
    IEnumerator WaitForStart()
    {
        while (true)
        {
            yield return _wait; // this is needed to avoid duplicate audio sources at gameobject on enable
            if (engineIdle == null)
                Start();
            break;
        }
    }
    // create audio sources
    // idle
    void CreateIdle()
    {
        if (idleClip != null)
        {
            engineIdle = gameObject.AddComponent<AudioSource>();
            engineIdle.spatialBlend = spatialBlend;
            engineIdle.rolloffMode = audioRolloffMode;
            engineIdle.dopplerLevel = dopplerLevel;
            engineIdle.volume = idleVolCurve.Evaluate(clipsValue) * masterVolume;
            engineIdle.pitch = idlePitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            engineIdle.minDistance = minDistance;
            engineIdle.maxDistance = maxDistance;
            engineIdle.loop = true;
            engineIdle.clip = idleClip;
            engineIdle.Play();
            if (audioMixer != null)
                engineIdle.outputAudioMixerGroup = audioMixer;
        }
    }
    // low
    void CreateLowOn()
    {
        if (lowOnClip != null)
        {
            lowOn = gameObject.AddComponent<AudioSource>();
            lowOn.spatialBlend = spatialBlend;
            lowOn.rolloffMode = audioRolloffMode;
            lowOn.dopplerLevel = dopplerLevel;
            lowOn.volume = lowVolCurve.Evaluate(clipsValue) * masterVolume;
            lowOn.pitch = lowPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            lowOn.minDistance = minDistance;
            lowOn.maxDistance = maxDistance;
            lowOn.loop = true;
            lowOn.clip = lowOnClip;
            lowOn.Play();
            if (audioMixer != null)
                lowOn.outputAudioMixerGroup = audioMixer;
        }
    }
    // medium
    void CreateMedOn()
    {
        if (medOnClip != null)
        {
            medOn = gameObject.AddComponent<AudioSource>();
            medOn.spatialBlend = spatialBlend;
            medOn.rolloffMode = audioRolloffMode;
            medOn.dopplerLevel = dopplerLevel;
            medOn.volume = medVolCurve.Evaluate(clipsValue) * masterVolume;
            medOn.pitch = medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            medOn.minDistance = minDistance;
            medOn.maxDistance = maxDistance;
            medOn.loop = true;
            medOn.clip = medOnClip;
            medOn.Play();
            if (audioMixer != null)
                medOn.outputAudioMixerGroup = audioMixer;
        }
    }
    // high
    void CreateHighOn()
    {
        if (highOnClip != null)
        {
            highOn = gameObject.AddComponent<AudioSource>();
            highOn.spatialBlend = spatialBlend;
            highOn.rolloffMode = audioRolloffMode;
            highOn.dopplerLevel = dopplerLevel;
            highOn.volume = highVolCurve.Evaluate(clipsValue) * masterVolume;
            highOn.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            highOn.minDistance = minDistance;
            highOn.maxDistance = maxDistance;
            highOn.loop = true;
            highOn.clip = highOnClip;
            highOn.Play();
            if (audioMixer != null)
                highOn.outputAudioMixerGroup = audioMixer;
        }
    }
    // rpm limit
    void CreateRPMLimit()
    {
        if (maxRPMClip != null)
        {
            maxRPM = gameObject.AddComponent<AudioSource>();
            maxRPM.spatialBlend = spatialBlend;
            maxRPM.rolloffMode = audioRolloffMode;
            maxRPM.dopplerLevel = dopplerLevel;
            maxRPM.volume = maxRPMVolCurve.Evaluate(clipsValue) * masterVolume;
            maxRPM.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            maxRPM.minDistance = minDistance;
            maxRPM.maxDistance = maxDistance;
            maxRPM.loop = true;
            maxRPM.clip = maxRPMClip;
            maxRPM.Play();
            if (audioMixer != null)
                maxRPM.outputAudioMixerGroup = audioMixer;
        }
    }
    // reversing
    void CreateReverse()
    {
        if (reversingClip != null)
        {
            reversing = gameObject.AddComponent<AudioSource>();
            reversing.spatialBlend = spatialBlend;
            reversing.rolloffMode = audioRolloffMode;
            reversing.dopplerLevel = dopplerLevel;
            reversing.volume = reversingVolCurve.Evaluate(clipsValue) * masterVolume;
            reversing.pitch = reversingPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            reversing.minDistance = minDistance;
            reversing.maxDistance = maxDistance;
            reversing.loop = true;
            reversing.clip = reversingClip;
            reversing.Play();
            if (audioMixer != null)
                reversing.outputAudioMixerGroup = audioMixer;
        }
    }
}
