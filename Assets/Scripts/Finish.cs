using UnityEngine;
using System;
using System.Collections;

/// Câstigătorul cursei
public enum RaceWinner { Player, AI , None }

[RequireComponent(typeof(BoxCollider))] /// Forțează să conține un box collider pentru trigger
public class Finish : MonoBehaviour {

    /// Referințe + info pentru schimbarea camerei
    [SerializeField] GameObject finishCamera;
    GameObject mainCam;
    [Space]
    [SerializeField] float cameraTimer = 2f;
    [SerializeField] [Range(0f,1f)] float slowMotion = .65f;
    [Space]
    [SerializeField] GameObject confettiParent;
    Coroutine camCoroutine;

    BoxCollider boxCollider;
    const string AI_TAG = "AI";
    const string PLAYER_TAG = "Player";
    public static Action<RaceWinner> OnRaceFinished;
    RaceWinner raceWinner = RaceWinner.None;

    bool isDragInitialized;
    
    void Start() { /// Referințe
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;    
        DragRaceController.OnDragInitialized += resetValues;

        mainCam = Camera.main.gameObject;
    }

    /// Pentru detectarea obiecturilor
    void OnTriggerEnter(Collider other) {
        if(!isDragInitialized) return;

        if(other.CompareTag(AI_TAG)){
            Vehicle vehicle = other.GetComponent<Vehicle>();
            if(vehicle == null) return;
            vehicle.SetAccelerationState(false);
            vehicle.SetBreakState(true);

            if(raceWinner == RaceWinner.None){
                raceWinner = RaceWinner.AI;
                OnRaceFinished?.Invoke(raceWinner);
            }
        }

        if(other.CompareTag(PLAYER_TAG)){
            if(raceWinner == RaceWinner.None){
                raceWinner = RaceWinner.Player;
                OnRaceFinished?.Invoke(raceWinner);
            }
        }

        if(camCoroutine != null) return;
        camCoroutine = StartCoroutine(finishCamCo());
    }
    /// Schimbarea camerai și versa
    IEnumerator finishCamCo(){
        if(finishCamera == null) yield break;
        confettiParent.SetActive(true);
        finishCamera.SetActive(true);
        mainCam.SetActive(false);
        Time.timeScale = slowMotion;
        yield return new WaitForSeconds(cameraTimer);
        resetFinishDefaults();
    }
    /// Resetarea valorilori inițiale
    void resetValues(){
        isDragInitialized = true;
        raceWinner = RaceWinner.None;
        camCoroutine = null;
        StopAllCoroutines();
        resetFinishDefaults();
    }
    /// Resetarea timpului și camerelor
    void resetFinishDefaults(){
        Time.timeScale = 1f;
        finishCamera.SetActive(false);
        mainCam.SetActive(true);
        confettiParent.SetActive(false);
    }
}
