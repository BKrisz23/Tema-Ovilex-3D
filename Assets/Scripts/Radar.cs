using UnityEngine;
using System.Collections;
using TMPro;
using PG;

/// Controleză radarul pentru speed warning
[RequireComponent(typeof(BoxCollider))]
public class Radar : MonoBehaviour {
    [Header("Speed Info")]
    [SerializeField] [Range(20,300)]int speedLimit = 60; /// Setează limita peste care se activează
    [SerializeField] [Range(.1f,1f)] float blitzDuration = .2f;
    [Space]
    [SerializeField] GameObject blitzGameObj;
    [Header("Canvas")]
    [SerializeField] GameObject canvas;
    [SerializeField] TextMeshProUGUI speedText;
    [SerializeField] float textDuration = 2f;

    void OnTriggerEnter(Collider other) {
        VehicleController vehicle = other.GetComponent<VehicleController>(); /// Se uita după refference
        if(vehicle == null) return;

        if(vehicle.SpeedInHour > speedLimit){/// Compară viteza
            speedText.text = Mathf.RoundToInt(vehicle.SpeedInHour).ToString();
            if(!canvas.activeSelf) StartCoroutine(textDurationCo());
            if(!blitzGameObj.activeSelf) StartCoroutine(blitzCo());
        } 
    }
    IEnumerator textDurationCo(){
        canvas.SetActive(true);
        yield return new WaitForSeconds(textDuration);
        canvas.SetActive(false);
    }
    IEnumerator blitzCo(){
        blitzGameObj.SetActive(true);
        yield return new WaitForSeconds(blitzDuration);
        blitzGameObj.SetActive(false);
    }
}