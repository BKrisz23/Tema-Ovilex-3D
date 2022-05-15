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

public class ShiftingSound : MonoBehaviour
{

    RealisticEngineSound res;
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    // audio mixer
    public AudioMixerGroup audioMixer;
    private AudioMixerGroup _audioMixer;

    public AudioClip shiftingSoundClip;
    private AudioSource shiftingSound;
    private int playOnce = 0;
    public bool destroyAudioSources = false;

    void Start()
    {
        res = gameObject.transform.parent.GetComponent<RealisticEngineSound>();
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
    }

    void Update()
    {
        if (res.enabled)
        {
            if (res.isCameraNear)
            {
                if (res.isShifting)
                {
                    if (playOnce == 0)
                    {
                        if (shiftingSound == null)
                            CreateShiftSound();
                        else
                            shiftingSound.PlayOneShot(shiftingSoundClip);
                        playOnce = 1;
                    }
                }
                else
                {
                    playOnce = 0;
                    if (destroyAudioSources)
                        if (shiftingSound != null)
                            if (!shiftingSound.isPlaying)
                                Destroy(shiftingSound);
                }
            }
            else
            {
                playOnce = 0;
                if (destroyAudioSources)
                    if (shiftingSound != null)
                        if (!shiftingSound.isPlaying)
                            Destroy(shiftingSound);
            }
        }
        else
        {
            if (shiftingSound != null)
                Destroy(shiftingSound);
        }
    }
    private void OnEnable() // if prefab got new audiomixer on runtime, it will use that after prefab got re-enabled
    {
        Start();
    }
    private void OnDisable()
    {
        if (shiftingSound != null)
            Destroy(shiftingSound);
    }
    void CreateShiftSound()
    {
        shiftingSound = gameObject.AddComponent<AudioSource>();
        shiftingSound.rolloffMode = res.audioRolloffMode;
        shiftingSound.minDistance = res.minDistance;
        shiftingSound.maxDistance = res.maxDistance;
        shiftingSound.spatialBlend = res.spatialBlend;
        shiftingSound.dopplerLevel = res.dopplerLevel;
        shiftingSound.volume = masterVolume;
        if (_audioMixer != null)
            shiftingSound.outputAudioMixerGroup = _audioMixer;
        shiftingSound.pitch = (Random.Range(0.9f, 1.1f));
        shiftingSound.loop = false;
        shiftingSound.clip = shiftingSoundClip;
        shiftingSound.Play();
    }
}
