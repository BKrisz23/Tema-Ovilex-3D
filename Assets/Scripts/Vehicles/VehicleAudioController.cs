using UnityEngine;

namespace Vehicles{
    public interface IVehicleAudioController
    {
        void PlayTurnSignal(IndicatorDirection direction);
        void Enable();
        void Disable();
    }

    public class VehicleAudioController : IVehicleAudioController
    {
        public VehicleAudioController(AudioSource audioSource, VehicleAudio vehicleAudio)
        {
            this.audioSource = audioSource;
            this.vehicleAudio = vehicleAudio;
            audioSource.playOnAwake = true;
        }

        AudioSource audioSource;
        VehicleAudio vehicleAudio;

        public void PlayTurnSignal(IndicatorDirection direction){
            audioSource.loop = true;
            if(audioSource.isPlaying && direction != IndicatorDirection.None) return;
            else if(audioSource.isPlaying && direction == IndicatorDirection.None){
                audioSource.Stop();
                audioSource.clip = null;
                return;
            }

            audioSource.clip = vehicleAudio.GetTurnSignal;
            audioSource.Play();
        }

        public void Enable(){
            audioSource.enabled = true;
        }

        public void Disable(){
            audioSource.enabled = false;
        }
    }
}