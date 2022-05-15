//______________________________________________//
//___________Realistic Engine Sounds____________//
//______________________________________________//
//_______Copyright © 2021 Yugel Mobile__________//
//______________________________________________//
//_________ http://mobile.yugel.net/ ___________//
//______________________________________________//
//________ http://fb.com/yugelmobile/ __________//
//______________________________________________//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class Muffler_mob : MonoBehaviour {

    RealisticEngineSound_mobile res;
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    //
    public bool playDuringShifting = true;
    public bool destroyAudioSources = false;
    private bool _destroyAudioSources = false;
    // audio mixer
    public AudioMixerGroup audioMixer;
    private AudioMixerGroup _audioMixer;
    // pitch multiplier
    [Range(0.5f, 2.0f)]
    public float pitchMultiplier = 1;
    // play time
    [Range(0.5f, 4)]
    public float playTime = 2;
    private float playTime_;
    // audio clips
    public AudioClip offClip;
    public AudioClip onClip;
    // audio sources
    private AudioSource offLoop;
    private AudioSource onLoop;
    // curve settings
    public AnimationCurve mufflerOffVolCurve;
    public AnimationCurve mufflerOnVolCurve;
    // private
    private float clipsValue;
    private int oneShotController = 0;
    private WaitForSeconds _playtime;

    void Start()
    {
        res = gameObject.transform.parent.GetComponent<RealisticEngineSound_mobile>();
        // audio mixer settings
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
        playTime_ = playTime;
        UpdateWaitTime();
    }
    void Update()
    {
        if (_destroyAudioSources != destroyAudioSources)
        {
            _destroyAudioSources = destroyAudioSources;
            if (destroyAudioSources) // destroy audip sources just got enabled
            {
                if (onLoop != null)
                    Destroy(onLoop);
                if (offLoop != null)
                    Destroy(offLoop);
            }
        }
        if (res.enabled)
        {
            clipsValue = res.engineCurrentRPM / res.maxRPMLimit; // calculate % percentage of rpm
            if (res.isCameraNear)
            {
                if (res.gasPedalPressing)
                {
                    if (oneShotController != 2)
                        oneShotController = 1; // prepare for one shoot
                }
                else
                {
                    // off loop
                    if (mufflerOffVolCurve.Evaluate(clipsValue) * masterVolume > 0.09f)
                    {
                        if (!playDuringShifting)
                        {
                            if (oneShotController == 2)
                            {
                                if (offLoop == null)
                                {
                                    if (!res.isShifting)
                                    {
                                        CreateOff();
                                    }
                                }
                                else
                                {
                                    if (!res.isShifting) // muffler crackle is disabled during shifting
                                    {
                                        if (!destroyAudioSources)
                                        {
                                            if (!offLoop.isPlaying)
                                            {
                                                offLoop.Play();
                                                StartCoroutine(WaitForOffLoop());
                                            }
                                        }
                                        offLoop.pitch = pitchMultiplier;
                                        offLoop.volume = mufflerOffVolCurve.Evaluate(clipsValue) * masterVolume;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (oneShotController == 2)
                            {
                                if (offLoop == null)
                                {
                                    CreateOff();
                                }
                                else
                                {
                                    if (!_destroyAudioSources)
                                    {
                                        if (!offLoop.isPlaying)
                                        {
                                            offLoop.Play();
                                            StartCoroutine(WaitForOffLoop());
                                        }
                                        offLoop.pitch = res.medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                                        offLoop.volume = mufflerOffVolCurve.Evaluate(clipsValue) * masterVolume;
                                    }
                                }
                            }
                        }
                    }
                }
                if (res.isShifting) // play the sound even if the car has an automatic transmission
                {
                    if (playDuringShifting)
                    {
                        if (offLoop == null)
                        {
                            CreateOff();
                        }
                        else
                        {
                            if (!destroyAudioSources)
                            {
                                if (!offLoop.isPlaying)
                                {
                                    offLoop.Play();
                                    StartCoroutine(WaitForOffLoop());
                                }
                            }
                            offLoop.pitch = pitchMultiplier;
                            offLoop.volume = mufflerOffVolCurve.Evaluate(clipsValue) * masterVolume;
                        }
                    }
                }
                // on loop
                if (mufflerOnVolCurve.Evaluate(clipsValue) * masterVolume > 0.09f)
                {
                    if (!playDuringShifting)
                    {
                        if (oneShotController == 1)
                        {
                            if (onLoop == null)
                            {
                                if (!res.isShifting)
                                {
                                    CreateOn();
                                    oneShotController = 2; // one shot is played, do not play more
                                }
                            }
                            else
                            {
                                if (!_destroyAudioSources)
                                {
                                    if (!onLoop.isPlaying)
                                    {
                                        onLoop.Play();
                                        oneShotController = 2;
                                        StartCoroutine(WaitForOnLoop());
                                    }
                                }
                                if (!res.isShifting) // muffler crackle is disabled during shifting
                                {
                                    onLoop.pitch = pitchMultiplier;
                                    onLoop.volume = mufflerOnVolCurve.Evaluate(clipsValue) * masterVolume;
                                }
                                oneShotController = 2; // one shot is played, do not play more
                            }
                        }
                    }
                    else
                    {
                        if (oneShotController == 1)
                        {
                            if (onLoop == null)
                            {
                                CreateOn();
                                oneShotController = 2; // one shot is played, do not play more
                            }
                            else
                            {
                                if (!_destroyAudioSources)
                                {
                                    if (!onLoop.isPlaying)
                                    {
                                        onLoop.Play();
                                        oneShotController = 2; // one shot is played, do not play more
                                        StartCoroutine(WaitForOnLoop());
                                    }
                                    onLoop.pitch = res.medPitchCurve.Evaluate(clipsValue) * pitchMultiplier;
                                    onLoop.volume = mufflerOnVolCurve.Evaluate(clipsValue) * masterVolume;
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (onLoop != null)
                Destroy(onLoop);
            if (offLoop != null)
                Destroy(offLoop);
        }
        if (playTime_ != playTime) // playTime value is got changed on runtime
            UpdateWaitTime();
    }
    private void OnEnable() // if prefab got new audiomixer on runtime, it will use that after prefab got re-enabled
    {
        Start();
    }
    private void OnDisable()
    {
        if (onLoop != null)
            Destroy(onLoop);
        if (offLoop != null)
            Destroy(offLoop);
    }
    // create off loop
    void CreateOff()
    {
        if (offClip != null)
        {
            offLoop = gameObject.AddComponent<AudioSource>();
            offLoop.rolloffMode = res.audioRolloffMode;
            offLoop.minDistance = res.minDistance;
            offLoop.maxDistance = res.maxDistance;
            offLoop.spatialBlend = res.spatialBlend;
            offLoop.dopplerLevel = res.dopplerLevel;
            offLoop.volume = mufflerOffVolCurve.Evaluate(clipsValue) * masterVolume;
            offLoop.pitch = pitchMultiplier;
            offLoop.clip = offClip;
            offLoop.loop = true;
            if (_audioMixer != null)
                offLoop.outputAudioMixerGroup = _audioMixer;
            offLoop.Play();
            StartCoroutine(WaitForOffLoop());
        }
    }
    //  create on loop
    void CreateOn()
    {
        if (onClip != null)
        {
            onLoop = gameObject.AddComponent<AudioSource>();
            onLoop.rolloffMode = res.audioRolloffMode;
            onLoop.minDistance = res.minDistance;
            onLoop.maxDistance = res.maxDistance;
            onLoop.spatialBlend = res.spatialBlend;
            onLoop.dopplerLevel = res.dopplerLevel;
            onLoop.volume = mufflerOnVolCurve.Evaluate(clipsValue) * masterVolume;
            onLoop.pitch = pitchMultiplier;
            onLoop.clip = onClip;
            onLoop.loop = true;
            if (_audioMixer != null)
                onLoop.outputAudioMixerGroup = _audioMixer;
            onLoop.Play();
            StartCoroutine(WaitForOnLoop());
        }
    }
    private void UpdateWaitTime()
    {
        _playtime = new WaitForSeconds(playTime);
        playTime_ = playTime;
    }
    IEnumerator WaitForOnLoop()
    {
        while (true)
        {
            yield return _playtime; // destroy audio playtime secconds later
            if (onLoop != null)
            {
                if (_destroyAudioSources)
                    Destroy(onLoop);
                else
                    onLoop.Stop();
            }
            break;
        }
    }
    IEnumerator WaitForOffLoop()
    {
        while (true)
        {
            yield return _playtime; // destroy audio playtime secconds later
            oneShotController = 0;
            if (offLoop != null)
            {
                if (_destroyAudioSources)
                    Destroy(offLoop);
                else
                    offLoop.Stop();
            }
            break;
        }
    }
}
