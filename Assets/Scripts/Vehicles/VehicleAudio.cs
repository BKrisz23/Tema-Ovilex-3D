using UnityEngine;
namespace Vehicles{
    [CreateAssetMenu(fileName = "VehicleAudio", menuName = "Vehicle/VehicleAudio", order = 0)]
    public class VehicleAudio : ScriptableObject {
        [SerializeField] AudioClip turnSignal;
        public AudioClip GetTurnSignal => turnSignal;
    }
}
