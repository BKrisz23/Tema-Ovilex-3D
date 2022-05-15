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

public class SuperCharger : MonoBehaviour {

    RealisticEngineSound res;
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    // audio mixer
    public AudioMixerGroup audioMixer;
    private AudioMixerGroup _audioMixer;
    // audio clips
    public AudioClip chargerOnLoopClip;
    public AudioClip chargerOffLoopClip;
    public AnimationCurve chargerVolCurve;
    public AnimationCurve chargerPitchCurve;
    //
    public bool destroyAudioSources;
    // curve settings
    private AudioSource chargerOnLoop;
    private AudioSource chargerOffLoop;
    private float clipsValue;

    void Start ()
    {
        res = gameObject.transform.parent.GetComponent<RealisticEngineSound>();
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
    }
    void Update ()
    {
        if (res.enabled)
        {
            clipsValue = res.engineCurrentRPM / res.maxRPMLimit; // calculate % percentage of rpm
            if (res.isCameraNear)
            {
                if (res.gasPedalPressing) // gas pedal is pressing
                {
                    if (chargerOnLoop == null)
                    {
                        CreateChargerOn();
                    }
                    else
                    {
                        if (!chargerOnLoop.isPlaying)
                            chargerOnLoop.Play();
                    }
                    chargerOnLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume * res.gasPedalValue;
                    chargerOnLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
                    if (chargerOffLoop != null)
                    {
                        if (destroyAudioSources)
                            Destroy(chargerOffLoop);
                        else
                            chargerOffLoop.Stop();
                    }
                }
                else // gas pedal is released
                {
                    if (chargerOffLoop == null)
                    {
                        CreateChargerOff();
                    }
                    else
                    {
                        if (!chargerOffLoop.isPlaying)
                            chargerOffLoop.Play();
                    }
                    chargerOffLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume * (1 - res.gasPedalValue);
                    chargerOffLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
                    if (chargerOnLoop != null)
                    {
                        if (destroyAudioSources)
                            Destroy(chargerOnLoop);
                        else
                            chargerOnLoop.Stop();
                    }
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
    private void OnEnable() // if prefab got new audiomixer on runtime, it will use that after prefab got re-enabled
    {
        Start();
    }
    private void OnDisable() // destroy audio sources if disabled
    {
        DestroyAll();
    }
    private void DestroyAll()
    {
        if (chargerOnLoop != null)
            Destroy(chargerOnLoop);
        if (chargerOffLoop != null)
            Destroy(chargerOffLoop);
    }
    private void StopAll()
    {
        if (chargerOnLoop != null)
            chargerOnLoop.Stop();
        if (chargerOffLoop != null)
            chargerOffLoop.Stop();
    }
    // create audio sources
    void CreateChargerOn()
    {
        if (chargerOnLoopClip != null)
        {
            chargerOnLoop = gameObject.AddComponent<AudioSource>();
            chargerOnLoop.rolloffMode = res.audioRolloffMode;
            chargerOnLoop.dopplerLevel = res.dopplerLevel;
            chargerOnLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume;
            chargerOnLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
            chargerOnLoop.minDistance = res.minDistance;
            chargerOnLoop.maxDistance = res.maxDistance;
            chargerOnLoop.spatialBlend = res.spatialBlend;
            chargerOnLoop.loop = true;
            if (_audioMixer != null)
                chargerOnLoop.outputAudioMixerGroup = _audioMixer;
            chargerOnLoop.clip = chargerOnLoopClip;
            chargerOnLoop.Play();
        }
    }
    void CreateChargerOff()
    {
        if (chargerOffLoopClip != null)
        {
            chargerOffLoop = gameObject.AddComponent<AudioSource>();
            chargerOffLoop.rolloffMode = res.audioRolloffMode;
            chargerOffLoop.dopplerLevel = res.dopplerLevel;
            chargerOffLoop.volume = chargerVolCurve.Evaluate(clipsValue) * masterVolume;
            chargerOffLoop.pitch = chargerPitchCurve.Evaluate(clipsValue);
            chargerOffLoop.minDistance = res.minDistance;
            chargerOffLoop.maxDistance = res.maxDistance;
            chargerOffLoop.spatialBlend = res.spatialBlend;
            chargerOffLoop.loop = true;
            if (_audioMixer != null)
                chargerOffLoop.outputAudioMixerGroup = _audioMixer;
            chargerOffLoop.clip = chargerOffLoopClip;
            chargerOffLoop.Play();
        }
    }
}
