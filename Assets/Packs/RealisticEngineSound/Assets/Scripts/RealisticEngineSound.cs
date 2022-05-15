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

public class RealisticEngineSound : MonoBehaviour 
{
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    public AudioMixerGroup audioMixer;
    public bool destroyAudioSources = true; // enable or disable: destroy unused or too far away audio sources that are currently can't be heard

    public float engineCurrentRPM = 0.0f;
    public bool gasPedalPressing = false;
    [Range(0.0f, 1.0f)]
    public float gasPedalValue = 1; // (simulated or not simulated) 0 = not pressing = 0 engine volume, 0.5 = halfway pressing (half engine volume), 1 = pedal to the metal (full engine volume)
    // enum for gas pedal setting
    public enum GasPedalValue { Simulated, NotSimulated } // NotSimulated setting is recommended for joystick controlled games
    public GasPedalValue gasPedalValueSetting = new GasPedalValue();
    //
    [Range(1.0f, 15.0f)]
    public float gasPedalSimSpeed = 5.5f; // simulates how fast the player hit the gas pedal
    public float maxRPMLimit = 7000;
    [Range(0.0f, 5.0f)]
    public float dopplerLevel = 1;
    [Range(0.0f, 1.0f)]
    [HideInInspector] // remove this line if you want to set custom values for spatialBlend
    public float spatialBlend = 1f; // this value should always be 1. If you want custom value, remove spatialBlend = 1f; line from Start()
    [Range(0.1f, 2.0f)]
    public float pitchMultiplier = 1.0f; // pitch value multiplier
    public AudioReverbPreset reverbZoneSetting;
    private AudioReverbPreset reverbZoneControll;

    [Range(0.0f, 0.25f)]
    public float optimisationLevel = 0.01f; // audio source with volume level below this value will be destroyed
    public AudioRolloffMode audioRolloffMode = AudioRolloffMode.Custom;
    // play sounds within this distances
    public float minDistance = 1; // within the minimum distance the audiosources will cease to grow louder in volume
    public float maxDistance = 50; // maxDistance is the distance a sound stops attenuating at
    // other settings
    public bool isReversing = false; // is car in reverse gear
    public bool useRPMLimit = true; // enable rpm limit at maximum rpm
    public bool enableReverseGear = true; // enable wistle sound for reverse gear

    // hiden public stuff
    [HideInInspector]
    public float carCurrentSpeed = 1f; // needed for straight cut gearbox script
    [HideInInspector]
    public float carMaxSpeed = 250f; // needed for straight cut gearbox script
    [HideInInspector]
    public bool isShifting = false; // needed for shifting sounds script

    // idle clip sound
    public AudioClip idleClip;
    public AnimationCurve idleVolCurve;
    public AnimationCurve idlePitchCurve;
    // low rpm clip sounds
    public AudioClip lowOffClip;
    public AudioClip lowOnClip;
    public AnimationCurve lowVolCurve;
    public AnimationCurve lowPitchCurve;
    // medium rpm clip sounds
    public AudioClip medOffClip;
    public AudioClip medOnClip;
    public AnimationCurve medVolCurve;
    public AnimationCurve medPitchCurve;
    // high rpm clip sounds
    public AudioClip highOffClip;
    public AudioClip highOnClip;
    public AnimationCurve highVolCurve;
    public AnimationCurve highPitchCurve;
    // maximum rpm clip sound - if RPM limit is enabled
    public AudioClip maxRPMClip;
    public AnimationCurve maxRPMVolCurve;
    // reverse gear clip sound
    public AudioClip reversingClip;
    public AnimationCurve reversingVolCurve;
    public AnimationCurve reversingPitchCurve;

    // idle audio source
    private AudioSource engineIdle;

    // low rpm audio sources
    private AudioSource lowOff;
    private AudioSource lowOn;

    // medium rpm audio sources
    private AudioSource medOff;
    private AudioSource medOn;

    // high rpm audio sources
    private AudioSource highOff;
    private AudioSource highOn;

    //maximum rpm audio source
    private AudioSource maxRPM;

    // reverse gear audio source
    private AudioSource reversing;
    //private settings
    private float clipsValue;
    private float clipsValue2;
    // get camera for optimisation
    public Camera mainCamera;
    [HideInInspector]
    public bool isCameraNear; // tells is the camera near

    // shake engine sound settings
    public enum EngineShake { Off, Random, AllwaysOn }
    public EngineShake engineShakeSetting = new EngineShake();

    public enum ShakeLenghtType { Fix, Random }
    [HideInInspector]
    public ShakeLenghtType shakeLenghtSetting = new ShakeLenghtType();

    [HideInInspector] //[Range(10,100)]
    public float shakeLength = 50f;
    [HideInInspector] //[Range(0.3f, 0.9f)]
    public float shakeVolumeChange = 0.35f;
    [HideInInspector] //[Range(0.1f, 0.9f)]
    public float randomChance = 0.5f;

    private float _endRange = 1;
    private float shakeVolumeChangeDetect; // detect value change on runtime
    private float _oscillateRange;
    private float _oscillateOffset;
    private float lenght = 0; // shakingOn time
    private float randomShakingValue = 0;
    private float randomShakingValue2 = 0;
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
                reverbZoneControll = reverbZoneSetting;
                SetReverbZone();
            }
            else
            {
                isCameraNear = false;
            }
        }
        UpdateStartRange();
    }
    private void Update()
    {
        if (isCameraNear)
        {
            if (gasPedalValueSetting == GasPedalValue.Simulated)
            {
                if (shakeVolumeChangeDetect != shakeVolumeChange)
                    UpdateStartRange();
                if (engineShakeSetting == EngineShake.Off)
                {
                    clipsValue = engineCurrentRPM / maxRPMLimit;
                    if (gasPedalPressing)
                        gasPedalValue = Mathf.Lerp(gasPedalValue, 1, Time.deltaTime * gasPedalSimSpeed);
                    else
                        gasPedalValue = Mathf.Lerp(gasPedalValue, 0, Time.deltaTime * gasPedalSimSpeed);
                }
                if (engineShakeSetting == EngineShake.AllwaysOn)
                {
                    if (gasPedalPressing)
                    {
                        if (lenght < 1) // shaking sound for short time
                        {
                            if (shakeLenghtSetting == ShakeLenghtType.Fix)
                            {
                                gasPedalValue = _oscillateOffset + Mathf.Sin(Time.time * (shakeLength * clipsValue)) * _oscillateRange;
                                clipsValue2 = (engineCurrentRPM / maxRPMLimit) + Mathf.Sin(Time.time * shakeLength) * (_oscillateRange / 10);
                            }
                            if (shakeLenghtSetting == ShakeLenghtType.Random)
                            {
                                gasPedalValue = _oscillateOffset + Mathf.Sin(Time.time * (Random.Range(10, 100) * clipsValue)) * _oscillateRange;
                                clipsValue2 = (engineCurrentRPM / maxRPMLimit) + Mathf.Sin(Time.time * Random.Range(10, 100)) * (_oscillateRange / 10);
                            }
                            lenght = lenght + Random.Range(0.01f, 0.12f);

                            clipsValue = clipsValue2;
                        }
                        else // end shaking
                        {
                            gasPedalValue = Mathf.Lerp(gasPedalValue, 1, Time.deltaTime * gasPedalSimSpeed);
                            clipsValue = engineCurrentRPM / maxRPMLimit;
                        }
                    }
                    else
                    {
                        gasPedalValue = Mathf.Lerp(gasPedalValue, 0, Time.deltaTime * gasPedalSimSpeed);
                        clipsValue = engineCurrentRPM / maxRPMLimit;
                        lenght = 0;
                    }
                }
                if (engineShakeSetting == EngineShake.Random)
                {
                    if (gasPedalPressing)
                    {
                        randomShakingValue2 = 0;
                        if (randomShakingValue == 0)
                            randomShakingValue = Random.Range(0.1f, 1f);
                        if (randomShakingValue < randomChance)
                        {
                            if (lenght < 1) // shaking sound for short time
                            {
                                if (shakeLenghtSetting == ShakeLenghtType.Fix)
                                {
                                    gasPedalValue = _oscillateOffset + Mathf.Sin(Time.time * (shakeLength * clipsValue)) * _oscillateRange;
                                    clipsValue2 = (engineCurrentRPM / maxRPMLimit) + Mathf.Sin(Time.time * shakeLength) * (_oscillateRange / 10);
                                }
                                if (shakeLenghtSetting == ShakeLenghtType.Random)
                                {
                                    gasPedalValue = _oscillateOffset + Mathf.Sin(Time.time * (Random.Range(10, 100) * clipsValue)) * _oscillateRange;
                                    clipsValue2 = (engineCurrentRPM / maxRPMLimit) + Mathf.Sin(Time.time * Random.Range(10, 100)) * (_oscillateRange / 10);
                                }
                                lenght = lenght + Random.Range(0.01f, 0.12f);

                                clipsValue = clipsValue2;
                            }
                            else // end shaking
                            {
                                gasPedalValue = Mathf.Lerp(gasPedalValue, 1, Time.deltaTime * gasPedalSimSpeed);
                                clipsValue = engineCurrentRPM / maxRPMLimit;
                            }
                        }
                        else
                        {
                            gasPedalValue = Mathf.Lerp(gasPedalValue, 1, Time.deltaTime * gasPedalSimSpeed);
                            clipsValue = engineCurrentRPM / maxRPMLimit;
                        }
                    }
                    else
                    {
                        clipsValue = engineCurrentRPM / maxRPMLimit;
                        randomShakingValue = 0;
                        if (randomShakingValue2 == 0)
                            randomShakingValue2 = Random.Range(0.1f, 1f);
                        lenght = 0;
                        gasPedalValue = Mathf.Lerp(gasPedalValue, 0, Time.deltaTime * gasPedalSimSpeed);
                    }
                }
            }
            else
            {
                clipsValue = engineCurrentRPM / maxRPMLimit;
            }
            //
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
            // on load
            if (lowOnClip != null) // check is lowOn clip exists
            {
                if (lowOn == null)
                    if (lowVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateLowOn();
                if (lowOn != null)
                {
                    lowOn.volume = lowVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
                    lowOn.pitch = lowPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
            }
            if (lowOn != null) // destroy lowOn audio source when needed and destroying is enabled
            {
                if (destroyAudioSources)
                    if (lowOn.volume < optimisationLevel)
                        Destroy(lowOn);
            }
            // off load
            if (lowOffClip != null) // check is lowOff clip exists
            {
                if (lowOff == null)
                    if (lowVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateLowOff();
                if (lowOff != null)
                {
                    lowOff.volume = lowVolCurve.Evaluate(clipsValue) * masterVolume * (1 - gasPedalValue);
                    lowOff.pitch = lowPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
            }
            if (lowOff != null) // destroy lowOff audio source when needed and destroying is enabled
            {
                if (destroyAudioSources)
                    if (lowOff.volume < optimisationLevel)
                        Destroy(lowOff);
            }
            //
            // medium rpm
            // on load
            if (medOnClip != null) // check is medOn clip exists
            {
                if (medOn == null)
                    if (medVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateMedOn();
                if (medOn != null)
                {
                    medOn.volume = medVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
                    medOn.pitch = medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
                if (medOn != null) // destroy medOn audio source when needed and destroying is enabled
                {
                    if (destroyAudioSources)
                        if (medOn.volume < optimisationLevel)
                            Destroy(medOn);
                }
            }
            // off load
            if (medOffClip != null) // check is medOff clip exists
            {
                if (medOff == null)
                    if (medVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateMedOff();
                if (medOff != null)
                {
                    medOff.volume = medVolCurve.Evaluate(clipsValue) * masterVolume * (1 - gasPedalValue);
                    medOff.pitch = medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }
                if (medOff != null) // destroy medOff audio source when needed and destroying is enabled
                {
                    if (destroyAudioSources)
                        if (medOff.volume < optimisationLevel)
                            Destroy(medOff);
                }
            }
            //
            // high rpm
            // on load
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
                            highOn.volume = highVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
                        else
                            highOn.volume = (highVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue) / 3.3f; // max rpm is playing
                    }
                    else
                    {
                        highOn.volume = highVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
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
            // off load
            if (highOffClip != null) // check is highOff clip exists
            {
                if (highOff == null)
                    if (highVolCurve.Evaluate(clipsValue) * masterVolume > optimisationLevel)
                        CreateHighOff();
                if (highOff != null)
                {
                    highOff.volume = highVolCurve.Evaluate(clipsValue) * masterVolume * (1 - gasPedalValue);
                    highOff.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                }

                if (highOff != null) // destroy highOff audio source when needed and destroying is enabled
                {
                    if (destroyAudioSources)
                        if (highOff.volume < optimisationLevel)
                            Destroy(highOff);
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
                            if (maxRPM.volume > maxRPMVolCurve.Evaluate(clipsValue) * masterVolume)
                                maxRPM.volume = maxRPMVolCurve.Evaluate(clipsValue) * masterVolume; // update maxRPM volume with current rpm's value

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
                if (destroyAudioSources)
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
                    if (destroyAudioSources)
                        Destroy(reversing); // looks like someone disabled reversing on runtime, destroy this audio source
                }
            }
            // rpm limiting
            if (!useRPMLimit) // rpm limit is disabled in runtime, destroy it's audio source
            {
                    if (maxRPM != null)
                    Destroy(maxRPM);
            }

            // reverb setting is changed
            if (reverbZoneControll != reverbZoneSetting)
                SetReverbZone();
        }
        else // missing main camera
        {
            isCameraNear = false;
        }
    }
    private void OnEnable() // recreate all audio sources if Realistic Engine Sound's script is reEnabled
    {
        StartCoroutine(WaitForStart());
        SetReverbZone();
    }
    private void OnDisable() // destroy audio sources if Realistic Engine Sound's script is disabled
    {
        DestroyAll();
    }
    private void DestroyAll() // destroy audio sources if Realistic Engine Sound's script is disabled or too far from camera
    {
        if (engineIdle != null)
            Destroy(engineIdle);
        if (lowOn != null)
            Destroy(lowOn);
        if (lowOff != null)
            Destroy(lowOff);
        if (medOn != null)
            Destroy(medOn);
        if (medOff != null)
            Destroy(medOff);
        if (highOn != null)
            Destroy(highOn);
        if (highOff != null)
            Destroy(highOff);
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
        if (gameObject.GetComponent<AudioReverbZone>() != null)
            Destroy(gameObject.GetComponent<AudioReverbZone>());
    }
    private void UpdateStartRange()
    {
        _oscillateRange = (_endRange - (1-shakeVolumeChange)) / 2;
        _oscillateOffset = _oscillateRange + (1-shakeVolumeChange);
        shakeVolumeChangeDetect = shakeVolumeChange; // detect value change on runtime
    }
    void SetReverbZone()
    {
        if (reverbZoneSetting == AudioReverbPreset.Off)
        {
            if (gameObject.GetComponent<AudioReverbZone>() != null)
                Destroy(gameObject.GetComponent<AudioReverbZone>());
        }
        else
        {
            if (gameObject.GetComponent<AudioReverbZone>() == null)
            {
                gameObject.AddComponent<AudioReverbZone>();
                gameObject.GetComponent<AudioReverbZone>().reverbPreset = reverbZoneSetting;
            }
            else
            {
                gameObject.GetComponent<AudioReverbZone>().reverbPreset = reverbZoneSetting;
            }
        }
        reverbZoneControll = reverbZoneSetting;
    }
    IEnumerator WaitForStart()
    {
        while (true)
        {
            yield return _wait; // this is needed to avoid duplicate audio sources when gameobject is just enabled
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
            engineIdle.clip = idleClip;
            engineIdle.loop = true;
            engineIdle.Play();
            if (audioMixer != null)
                engineIdle.outputAudioMixerGroup = audioMixer;
        }
    }
    // low
    void CreateLowOff()
    {
        if (lowOffClip != null)
        {
            lowOff = gameObject.AddComponent<AudioSource>();
            lowOff.spatialBlend = spatialBlend;
            lowOff.rolloffMode = audioRolloffMode;
            lowOff.dopplerLevel = dopplerLevel;
            lowOff.volume = lowVolCurve.Evaluate(clipsValue) * masterVolume * (1 - gasPedalValue);
            lowOff.pitch = lowPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            lowOff.minDistance = minDistance;
            lowOff.maxDistance = maxDistance;
            lowOff.clip = lowOffClip;
            lowOff.loop = true;
            lowOff.Play();
            if (audioMixer != null)
                lowOff.outputAudioMixerGroup = audioMixer;
        }
    }
    void CreateLowOn()
    {
        if (lowOnClip != null)
        {
            lowOn = gameObject.AddComponent<AudioSource>();
            lowOn.spatialBlend = spatialBlend;
            lowOn.rolloffMode = audioRolloffMode;
            lowOn.dopplerLevel = dopplerLevel;
            lowOn.volume = lowVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
            lowOn.pitch = lowPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            lowOn.minDistance = minDistance;
            lowOn.maxDistance = maxDistance;
            lowOn.clip = lowOnClip;
            lowOn.loop = true;
            lowOn.Play();
            if (audioMixer != null)
                lowOn.outputAudioMixerGroup = audioMixer;
        }
    }
    // medium
    void CreateMedOff()
    {
        if (medOffClip != null)
        {
            medOff = gameObject.AddComponent<AudioSource>();
            medOff.spatialBlend = spatialBlend;
            medOff.rolloffMode = audioRolloffMode;
            medOff.dopplerLevel = dopplerLevel;
            medOff.volume = medVolCurve.Evaluate(clipsValue) * masterVolume * (1 - gasPedalValue);
            medOff.pitch = medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            medOff.minDistance = minDistance;
            medOff.maxDistance = maxDistance;
            medOff.clip = medOffClip;
            medOff.loop = true;
            medOff.Play();
            if (audioMixer != null)
                medOff.outputAudioMixerGroup = audioMixer;
        }
    }
    void CreateMedOn()
    {
        if (medOnClip != null)
        {
            medOn = gameObject.AddComponent<AudioSource>();
            medOn.spatialBlend = spatialBlend;
            medOn.rolloffMode = audioRolloffMode;
            medOn.dopplerLevel = dopplerLevel;
            medOn.volume = medVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
            medOn.pitch = medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            medOn.minDistance = minDistance;
            medOn.maxDistance = maxDistance;
            medOn.clip = medOnClip;
            medOn.loop = true;
            medOn.Play();
            if (audioMixer != null)
                medOn.outputAudioMixerGroup = audioMixer;
        }
    }
    // high
    void CreateHighOff()
    {
        if (highOffClip != null)
        {
            highOff = gameObject.AddComponent<AudioSource>();
            highOff.spatialBlend = spatialBlend;
            highOff.rolloffMode = audioRolloffMode;
            highOff.dopplerLevel = dopplerLevel;
            highOff.volume = highVolCurve.Evaluate(clipsValue) * masterVolume * (1 - gasPedalValue);
            highOff.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            highOff.minDistance = minDistance;
            highOff.maxDistance = maxDistance;
            highOff.clip = highOffClip;
            highOff.loop = true;
            highOff.Play();
            if (audioMixer != null)
                highOff.outputAudioMixerGroup = audioMixer;
        }
    }
    void CreateHighOn()
    {
        if (highOnClip != null)
        {
            highOn = gameObject.AddComponent<AudioSource>();
            highOn.spatialBlend = spatialBlend;
            highOn.rolloffMode = audioRolloffMode;
            highOn.dopplerLevel = dopplerLevel;
            highOn.volume = highVolCurve.Evaluate(clipsValue) * masterVolume * gasPedalValue;
            highOn.pitch = highPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
            highOn.minDistance = minDistance;
            highOn.maxDistance = maxDistance;
            highOn.clip = highOnClip;
            highOn.loop = true;
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
            maxRPM.clip = maxRPMClip;
            maxRPM.loop = true;
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
            reversing.clip = reversingClip;
            reversing.loop = true;
            reversing.Play();
            if(audioMixer != null)
            reversing.outputAudioMixerGroup = audioMixer;
        }
    }
}
