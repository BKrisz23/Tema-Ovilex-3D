using System;
using UnityEngine;
using System.Collections;

/// Controlează drag racingul
public class DragRaceController : MonoBehaviour {
    /// Referințe
    [SerializeField] GameplayUIController uIController;
    [Space]
    [SerializeField] AudioSource audioSource;
    Vehicle ai;

    bool isRaceOn;
    int countDown;
    public static Action OnDragInitialized;

    Coroutine countdownCoroutine;
    WaitForSeconds oneSecondAwaiter;
    
    void Start() {
        oneSecondAwaiter = new WaitForSeconds(1f);    
    }
    /// Setează vehicul AI
    public void SetVehicle(Vehicle v){
        if(v == null) return;
        ai = v;
    }
    /// Porneste cursa
    public void InitializeRace(){
        isRaceOn = true;
        OnDragInitialized?.Invoke();
        if(ai == null) return;
        ai.ResetVehicle();
        ai.SetBreakState(false);
        ai.SetAccelerationState(false);
        if(countdownCoroutine != null) StopAllCoroutines();
        countdownCoroutine = StartCoroutine(countdownCo());
        if(audioSource == null) return;
        audioSource.Play();
    }
    /// Numărătoarea inversă a cursei + actualizează UI-ul
    IEnumerator countdownCo(){
        if(uIController == null) yield break;
        countDown = 3;
        uIController.UpdateDragCountdown(countDown);
        yield return oneSecondAwaiter;
        countDown = 2;
        uIController.UpdateDragCountdown(countDown);
        yield return oneSecondAwaiter;
        countDown = 1;
        uIController.UpdateDragCountdown(countDown);
        yield return oneSecondAwaiter;
         countDown = 0;
        uIController.UpdateDragCountdown(countDown);

        if(ai == null) yield break;
        ai.SetAccelerationState(true);

        countdownCoroutine = null;
    }
}