using System;
using UnityEngine;
using System.Collections;
using TMPro;
using AI;
/// Controlează drag racingul
public class DragRaceController : MonoBehaviour {
    [Header("Refferences")]
    [SerializeField] GameController gameController;

    [Header("Drag")]
    [SerializeField] GameObject canvas;
    [SerializeField] TextMeshProUGUI countDownText;
    [SerializeField] AudioSource audioSource;

    [Header("StartPositions")]
    [SerializeField] Vector3 playerPos;
    [SerializeField] Vector3 aiPos;

    IControllerAI controllerAI; //Se poate face lista pentru mai multe AI uri

    IDragFinish finish;
    int countDown;
    public static Action OnDragInitialized;

    Coroutine countdownCoroutine;
    WaitForSeconds oneSecondAwaiter;
    
    void Start() {
        oneSecondAwaiter = new WaitForSeconds(1f); 
        canvas.SetActive(false);
        finish = GetComponentInChildren<IDragFinish>();
    }
    /// Setează vehicul AI
    public void SetAiRefference(IControllerAI ai){
        controllerAI = ai;
    }
    /// Porneste cursa
    public void InitializeRace(){
        controllerAI.ForceStop = true;
        controllerAI.Transform.position = aiPos;
        controllerAI.Transform.rotation = Quaternion.identity;

        gameController.ActiveVehicle.ForceStop = true;
        gameController.ActiveVehicle.Transform.position = playerPos;
        gameController.ActiveVehicle.Transform.rotation = Quaternion.identity;

        finish.IsDragInitialized = true;

        if(countdownCoroutine != null) StopAllCoroutines();
        countdownCoroutine = StartCoroutine(countdownCo(enableEntries));
        canvas.SetActive(true);

        audioSource.Play();
    }
    /// Numărătoarea inversă a cursei + actualizează UI-ul
    IEnumerator countdownCo(Action callBack){
        countDown = 3;
        countDownText.text = countDown.ToString();
        yield return oneSecondAwaiter;
        countDown = 2;
        countDownText.text = countDown.ToString();
        yield return oneSecondAwaiter;
        countDown = 1;
        countDownText.text = countDown.ToString();
        yield return oneSecondAwaiter;
        countDown = 0;
        countDownText.text = countDown.ToString();

        callBack?.Invoke();

        countdownCoroutine = null;
        canvas.SetActive(false);
    }

    void enableEntries(){
        gameController.ActiveVehicle.ForceStop = false;

        controllerAI.Accelerate = true;
        controllerAI.Break = false;
        controllerAI.ForceStop = false;
    }
}