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

public class ShiftingSoundRandom : MonoBehaviour {

    RealisticEngineSound res;
    // master volume setting
    [Range(0.1f, 1.0f)]
    public float masterVolume = 1f;
    // audio mixer
    public AudioMixerGroup audioMixer;
    private AudioMixerGroup _audioMixer;
    // shift sound clips
    public AudioClip[] shiftingSoundClips;
    public bool destroyAudioSources = false;
    private AudioSource shiftingSound;
    private int playOnce = 0;
    private WaitForSeconds _playtime;

    void Start()
    {
        res = gameObject.transform.parent.GetComponent<RealisticEngineSound>();
        _playtime = new WaitForSeconds(0.05f);
        // audio mixer settings
        if (audioMixer != null) // user is using a seperate audio mixer for this prefab
        {
            _audioMixer = audioMixer;
        }
        if (audioMixer == null)
        {
            if (res.audioMixer != null) // use engine sound's audio mixer for this prefab
            {
                _audioMixer = res.audioMixer;
                audioMixer = _audioMixer;
            }
        }
        // create and set audio source for shifting sounds
        shiftingSound = gameObject.AddComponent<AudioSource>();
        shiftingSound.rolloffMode = res.audioRolloffMode;
        shiftingSound.minDistance = res.minDistance;
        shiftingSound.maxDistance = res.maxDistance;
        shiftingSound.spatialBlend = res.spatialBlend;
        shiftingSound.dopplerLevel = res.dopplerLevel;
        shiftingSound.volume = masterVolume;
        if (_audioMixer != null)
            shiftingSound.outputAudioMixerGroup = _audioMixer;
    }
    void Update()
    {
        if (res.enabled)
        {
            if (res.isCameraNear)
            {
                if (res.isShifting)
                {
                    // play shift sound only once
                    if (playOnce == 0)
                    {
                        PlayShiftSound();
                        playOnce = 1;
                    }
                }
                else
                {
                    playOnce = 0; // waiting for next gear shifting
                }
            }
            else
            {
                if (shiftingSound != null)
                {
                    if (destroyAudioSources)
                        Destroy(shiftingSound);
                    else
                        shiftingSound.Stop();
                }
            }
        }
        else
        {
            Destroy(shiftingSound);
        }
    }
    private void OnDisable() // destroy audio sources if disabled
    {
        if (shiftingSound != null)
            Destroy(shiftingSound);
    }
    private void OnEnable() // recreate all audio sources if Realistic Engine Sound's script is reEnabled
    {
        StartCoroutine(WaitForStart());
    }
    IEnumerator WaitForStart()
    {
        while (true)
        {
            yield return _playtime; // this is needed to avoid duplicate audio sources
            if (shiftingSound == null)
                Start();
            break;
        }
    }
    // choose and set a random shifting sound
    void PlayShiftSound()
    {
        if (shiftingSound != null)
        {
            shiftingSound.clip = shiftingSoundClips[Random.Range(0, shiftingSoundClips.Length)]; // random clip
            shiftingSound.pitch = Random.Range(0.9f, 1.1f);
            shiftingSound.loop = false;
            shiftingSound.Play();
        }
        else
        {
            StartCoroutine(WaitForStart());
        }
    }
}
