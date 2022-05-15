using UnityEngine;
using UnityEngine.Events;

/// Controleză radarul pentru speed warning
[RequireComponent(typeof(BoxCollider))]
public class Radar : MonoBehaviour {
    [Header("Delegate")]
    [SerializeField] UnityEvent onRadarTrigger; /// Face legatură cu UI și alte

    [Header("Speed Info")]
    [SerializeField] [Range(20,300)]int speedLimit = 60; /// Setează limita peste care se activează

    void OnTriggerEnter(Collider other) {
        Vehicle vehicle = other.GetComponent<Vehicle>(); /// Se uita după refference
        if(vehicle == null) return;

        if(vehicle.GetSpeed > speedLimit) /// Compară viteza
            onRadarTrigger?.Invoke(); /// Trimite event pentru UI
    }
}