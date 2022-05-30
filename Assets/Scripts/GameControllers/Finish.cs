using UnityEngine;
using System;
using System.Collections;
using AI;
using TMPro;
using Vehicles;
/// Câstigătorul cursei
public enum RaceWinner { Player, AI , None }
public interface IDragFinish {
    bool IsDragInitialized {get; set;}
}
[RequireComponent(typeof(BoxCollider))] /// Forțează să conține un box collider pentru trigger
public class Finish : MonoBehaviour, IDragFinish{
    [Header("Canvas")]
    [SerializeField] GameObject canvas;
    [SerializeField] TextMeshProUGUI screenText;
    [Header("Camera / Slow Motion")]
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
    RaceWinner raceWinner = RaceWinner.None;

    bool isDragInitialized;
    public bool IsDragInitialized{ 
        get{
            return isDragInitialized; 
        }
        set{
            isDragInitialized = value;
            if(isDragInitialized){
                camCoroutine = null;
                raceWinner = RaceWinner.None;
            }
        }
    }
    
    void Start() {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;    
    }
    void OnTriggerEnter(Collider other) {
        if(!IsDragInitialized) return;

        if(other.CompareTag(AI_TAG)){
            IsDragInitialized = false;

            IControllerAI controller = other.GetComponent<IControllerAI>();
            if(controller == null) return;
            controller.Accelerate = false;
            controller.Break = true;

            if(raceWinner == RaceWinner.None)
                raceWinner = RaceWinner.AI;
            
        }

        if(other.CompareTag(PLAYER_TAG)){
            if(raceWinner == RaceWinner.None){
                raceWinner = RaceWinner.Player;
                mainCam = other.GetComponent<IVehicle>().VehicleCamera.GetActiveCam();
            }
    
        }

        if(camCoroutine != null) return;
        camCoroutine = StartCoroutine(finishCamCo());
    }
    IEnumerator finishCamCo(){
        switch(raceWinner){
            case RaceWinner.Player: screenText.text = "You Won!"; break;
            case RaceWinner.AI: screenText.text = "You Lose!"; break;
            case RaceWinner.None: screenText.text = ""; break;
        }

        canvas.SetActive(true);
        
        confettiParent.SetActive(true);
        if(raceWinner == RaceWinner.Player){
            Time.timeScale = slowMotion;

            finishCamera.SetActive(true);
            if(mainCam != null)
                mainCam.SetActive(false);

            yield return new WaitForSeconds(cameraTimer);

            Time.timeScale = 1f;

            finishCamera.SetActive(false);
            if(mainCam != null)
                mainCam.SetActive(true);
        }else{
            yield return new WaitForSeconds(2f);
        }
        
        confettiParent.SetActive(false);
        mainCam = null;
        
        canvas.SetActive(false);
    }
}
