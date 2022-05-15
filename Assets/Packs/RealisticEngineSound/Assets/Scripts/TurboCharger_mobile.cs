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

public class TurboCharger_mobile : MonoBehaviour {

    RealisticEngineSound_mobile res;
    private float clipsValue;
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    // audio mixer
    public AudioMixerGroup audioMixer;
    private AudioMixerGroup _audioMixer;
    // turbo loop
    public AudioClip turboLoopClip; // ssssssSSSS
    public AudioClip maxRpmLoopClip; // played at max rpm
    public AnimationCurve chargerVolCurve;
    public AnimationCurve chargerPitchCurve;
    // long shoot settings
    [Range(0.4f, 1.0f)]
    public float longShotTreshold = 0.8f; // after this % of current rpm, long shots are played when gas pedal is released
    // one shoot ssshutututus
    public AudioClip longShotClip; // SssHuuTuTuTututu
    public AudioClip shortShotClip; // SssHuuTu
    public AnimationCurve oneShotVolCurve;
    public AnimationCurve oneShotPitchCurve;
    //
    public bool destroyAudioSources = false;
    //
    private AudioSource turboLoop;
    private AudioSource oneShot;
    private AudioSource maxTurboLoop;

    private int oneShotController = 0;
    private WaitForSeconds _playtime;

    void Start()
    {
        res = gameObject.transform.parent.GetComponent<RealisticEngineSound_mobile>();
        if (audioMixer != null) // user is using a seperate audio mixer for this prefab
        {
            _audioMixer = audioMixer;
        }
        else
        {
            if (res.audioMixer != null) // use engine sound's audio mixer for this prefab
            {
                _audioMixer = res.audioMixer;
                audioMixer = _audioMixer;
            }
        }
        _playtime = new WaitForSeconds(0.15f);
    }
    void Update()
    {
        if (res.enabled)
        {
            if (res.isCameraNear)
            {
                clipsValue = res.engineCurrentRPM / res.maxRPMLimit; // calculate % percentage of rpm
                                                                     // if gas pedal on play turbo loop sound
                if (res.gasPedalPressing)
                {
                    oneShotController = 1; // prepare for one shoot
                    if (res.maxRPMVolCurve.Evaluate(clipsValue) * masterVolume < 0.5f)
                    {
                        if (turboLoop == null)
                        {
                            CreateTurboLoop();
                        }
                        else
                        {
                            if (!turboLoop.isPlaying)
                                turboLoop.Play();
                        }
                        turboLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume;
                        turboLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
                        if (maxTurboLoop != null)
                        {
                            if (destroyAudioSources)
                                Destroy(maxTurboLoop);
                            else
                                maxTurboLoop.Stop();
                        }
                    }
                    else // play max rpm turbo loop
                    {
                        if (res.useRPMLimit)
                        {
                            if (maxTurboLoop == null)
                                CreateMaxTurboLoop();
                            else
                            {
                                maxTurboLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume;
                                maxTurboLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
                            }
                            if (turboLoop != null)
                            {
                                if (destroyAudioSources)
                                    Destroy(turboLoop);
                                else
                                    turboLoop.Stop();
                            }
                        }
                    }
                }
                else // if gas released play one shoot
                {
                    // destroy turbo loops
                    if (turboLoop != null)
                        Destroy(turboLoop);
                    if (maxTurboLoop != null)
                        Destroy(maxTurboLoop);
                    // play one shot
                    if (oneShotController == 1)
                    {
                        if (oneShot == null)
                            CreateOneShot();
                        else
                            PlayOneShot();
                        oneShotController = 0; // one shot is played, do not play more
                    }
                }
                if (oneShot != null)
                {
                    if (!oneShot.isPlaying)
                        if (destroyAudioSources)
                            Destroy(oneShot);
                }
            }
            else
            {
                if (destroyAudioSources)
                    DestroyAll();
                else
                    StopAll();
            }
        }
        else
        {
            DestroyAll();
        }
    }
    private void OnDisable() // destroy audio sources if disabled
    {
        DestroyAll();
    }
    private void DestroyAll()
    {
        if (turboLoop != null)
            Destroy(turboLoop);
        if (oneShot != null)
            Destroy(oneShot);
        if (maxTurboLoop != null)
            Destroy(maxTurboLoop);
    }
    private void StopAll()
    {
        if (turboLoop != null)
            turboLoop.Stop();
        if (oneShot != null)
            oneShot.Stop();
        if (maxTurboLoop != null)
            maxTurboLoop.Stop();
    }
    private void OnEnable() // recreate audio sources if reEnabled
    {
        StartCoroutine(WaitForStart());
    }
    IEnumerator WaitForStart()
    {
        while (true)
        {
            yield return _playtime; // this is needed to avoid duplicate audio sources at scene start
            if (oneShot == null)
                Start();
            break;
        }
    }
    // create audio sources
    void CreateTurboLoop()
    {
        if (turboLoopClip != null)
        {
            turboLoop = gameObject.AddComponent<AudioSource>();
            turboLoop.rolloffMode = res.audioRolloffMode;
            turboLoop.dopplerLevel = res.dopplerLevel;
            turboLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume;
            turboLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
            turboLoop.minDistance = res.minDistance;
            turboLoop.maxDistance = res.maxDistance;
            turboLoop.spatialBlend = res.spatialBlend;
            turboLoop.loop = true;
            if (_audioMixer != null)
                turboLoop.outputAudioMixerGroup = _audioMixer;
            turboLoop.clip = turboLoopClip;
            turboLoop.Play();
        }
    }
    void CreateOneShot()
    {
        oneShot = gameObject.AddComponent<AudioSource>();
        oneShot.rolloffMode = res.audioRolloffMode;
        oneShot.spatialBlend = res.spatialBlend;
        oneShot.volume = oneShotVolCurve.Evaluate(clipsValue) * masterVolume;
        oneShot.pitch = oneShotPitchCurve.Evaluate(clipsValue) * Random.Range(0.85f, 1.2f);
        oneShot.minDistance = res.minDistance;
        oneShot.maxDistance = res.maxDistance;
        oneShot.loop = false;
        if (_audioMixer != null)
            oneShot.outputAudioMixerGroup = _audioMixer;
        oneShot.Stop();
        if (clipsValue > longShotTreshold)
        {
            oneShot.clip = longShotClip;
        }
        else
        {
            oneShot.clip = shortShotClip;
        }
        oneShot.Play();
    }
    void PlayOneShot()
    {
        if (clipsValue > longShotTreshold)
        {
            oneShot.clip = longShotClip;
        }
        else
        {
            oneShot.clip = shortShotClip;
        }
        oneShot.Play();
    }
    void CreateMaxTurboLoop()
    {
        if (maxRpmLoopClip != null)
        {
            maxTurboLoop = gameObject.AddComponent<AudioSource>();
            maxTurboLoop.rolloffMode = res.audioRolloffMode;
            maxTurboLoop.dopplerLevel = res.dopplerLevel;
            maxTurboLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume;
            maxTurboLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
            maxTurboLoop.minDistance = res.minDistance;
            maxTurboLoop.maxDistance = res.maxDistance;
            maxTurboLoop.spatialBlend = res.spatialBlend;
            maxTurboLoop.loop = true;
            if (_audioMixer != null)
                maxTurboLoop.outputAudioMixerGroup = _audioMixer;
            maxTurboLoop.clip = maxRpmLoopClip;
            maxTurboLoop.Play();
        }
    }
}
