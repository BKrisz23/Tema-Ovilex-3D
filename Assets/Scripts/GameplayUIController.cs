using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// Controlează UI-ul din scena Gameplay
public class GameplayUIController : MonoBehaviour {
    /// Referinţă și attributes legate de Drag Race
    [Header("Drag")]
    [SerializeField] TextMeshProUGUI dragCountdownText;
    [SerializeField] GameObject jumpStartedGameObj;
    Coroutine dragRaceCo;
    bool isAccelerating;

    /// Referinţă și attributes legate de faza scurtă și lungă
    [Header("BeamLight")]
    [SerializeField] Image beamLightImage;
    [SerializeField] Sprite[] beamLightSprites;
    const int beamLightsLenght = 3;

    /// Referinţă și attributes legate de viteză
    [Header("Speed Text")]
    [SerializeField] TextMeshProUGUI speedText;

    /// Referinţă și attributes legate de benzină
    [Header("Fuel")]
    [SerializeField] Image fuelImage;
    [SerializeField] Image fuelIconImage;
    [SerializeField] Image toggleFuelImage;

    /// Referinţă și attributes legate de meniul Gameplay
    [Header("GameplayMenu")]
    [SerializeField] GameObject menu;

    /// Referinţă și attributes legate de warningul radar
    [Header("Radar Warning")]
    [SerializeField] GameObject radarWarning;
    [SerializeField] float warningDuration;
    float warningDurationTimer;
    IEnumerator radarWarningIEnum;
    Coroutine radarWarningCo;
    
     /// Referinţă și attributes legate de direcția vehicului
    [Header("Drive Selector")]
    [SerializeField] RectTransform driveSelectorHandle;
    [SerializeField] float handlePosTop = 175f;
    [SerializeField] float handlePosBot = 25f;
    bool isDrivingBackwards;
    Vehicle vehicle;

    void Start() {
        DragRaceController.OnDragInitialized += onDragInitialized;
        Finish.OnRaceFinished += displayDragWinner;
    }

    /// Setează vehicul achiv
    public void SetVehicle(Vehicle v){
        if(v == null) return;

        if(vehicle != null && vehicle != v) vehicle.OnUpdateUI -= onUpdateUI;

        vehicle = v;
        vehicle.OnUpdateUI += onUpdateUI;
    }

    /// Comută starea semnalizarea stânga
    public void ToggleBlinkLights_Left(){
        if(vehicle == null) return;
        vehicle.ToggleBlinkLights_Left();
    }
    /// Comută starea semnalizarea dreapta
    public void ToggleBlinkLights_Right(){
        if(vehicle == null) return;
        vehicle.ToggleBlinkLights_Right();
    }
    /// Comută starea avariei
    public void ToggleBlinkLights_Hazard(){
        if(vehicle == null) return;
        vehicle.ToggleBlinkLights_Hazard();
    }
    /// Comută starea farurilor
    public void ToggleBeamLights(){
        Vehicle.BeamLight beamLight = vehicle.ToggleBeamLights();

        if(beamLightSprites.Length < beamLightsLenght || beamLightImage == null) return;
        switch(beamLight){
            case Vehicle.BeamLight.None: beamLightImage.sprite = beamLightSprites[0]; break;
            case Vehicle.BeamLight.Short: beamLightImage.sprite = beamLightSprites[1]; break;
            case Vehicle.BeamLight.Long: beamLightImage.sprite = beamLightSprites[2]; break;
        }
    }
    /// Resetează icoana farului pe UI
    public void ResetBeamLightIcon(){
        if(beamLightImage == null) return;
        beamLightImage.sprite = beamLightSprites[0];
    }
    /// Comută starea accelerației
    public void Accelerate(bool state){
        if(vehicle == null) return;
        vehicle.SetAccelerationState(state);
        isAccelerating = state;
    }
    /// Comută starea frânei
    public void Break(bool state){
        if(vehicle == null) return;
        vehicle.SetBreakState(state);
    }
    /// Comută direcția vehicului
    public void ToggleDriveDirection(){
        if(vehicle == null) return;
        if(!vehicle.CanVehicleReverse()) return;
        if(!vehicle.IsVehicleStopped()) return;

        vehicle.ToggleDriveDirection();
    
        if(driveSelectorHandle == null) return;

        /// Schimbă poziția pe UI
        Vector3 currentPos = driveSelectorHandle.anchoredPosition;
        if(currentPos.y == handlePosTop) currentPos.y = handlePosBot;
        else if(currentPos.y == handlePosBot) currentPos.y = handlePosTop;
        driveSelectorHandle.anchoredPosition = currentPos; 
    }
    /// Resetează direcția pe UI
    public void ResetDriveDirection(){
        if(driveSelectorHandle == null) return;
        Vector3 currentPos = driveSelectorHandle.anchoredPosition;
        driveSelectorHandle.anchoredPosition = currentPos;
    }
    /// Comută starea pilotului automat
    public void ToggleAutoPilot(){
        if(vehicle == null) return;
            vehicle.ToggleAutoPilot();

    }

    /// Actualizează viteza și benzina pe UI
    void onUpdateUI(Vehicle.UpdateUI update){
        if(vehicle == null) return;

        if(speedText != null){
            int speed = Mathf.RoundToInt(update.Speed);
            updateSpeedText(speed);
        }

        if(fuelImage != null && fuelImage.enabled){
            updateFuelImage(update.Fuel);
        }
    }
    /// Setează valoarea vitezei
    void updateSpeedText(int speed){
        speedText.text = speed.ToString();
    }
    /// Setează valoarea benzinei
    void updateFuelImage(float value){
        fuelImage.fillAmount = value;
    }
    /// Comută starea benzinei afisat pe UI - ON/OFF
    public void ToggleFuelImage(){
        if(fuelImage == null || fuelIconImage == null) return;
        fuelImage.enabled = !fuelImage.enabled;
        fuelIconImage.enabled = !fuelIconImage.enabled;
    }
    /// Comută starea meniului în Gameplay
    public void ToggleMenu(){
        if(menu == null) return;
        menu.SetActive(!menu.activeSelf);
    }
    /// Activează warningul radar
    public void EnableRadarWarning(){
        if(radarWarning == null) return;
        radarWarning.SetActive(true);

        if(radarWarningCo != null){
            warningDurationTimer = warningDuration;
            return;
        }

        radarWarningIEnum = radarWarningCounter();
        radarWarningCo = StartCoroutine(radarWarningIEnum);
    }
    /// Numărătoarea inversă a warningului
    IEnumerator radarWarningCounter(){
        warningDurationTimer = warningDuration;

        while(warningDurationTimer > 0){
            warningDurationTimer -= Time.deltaTime;
            yield return null;
        }
        
        radarWarning.SetActive(false);
        radarWarningCo = null;
    }
    /// Activeză textul numărătoarea inversă a cursei drag
    void onDragInitialized(){
        if(dragCountdownText != null) dragCountdownText.gameObject.SetActive(true);
    }
    /// Actualizează numărătoarea inversă a cursei drag
    public void UpdateDragCountdown(int timer){
        if(dragCountdownText == null || !dragCountdownText.gameObject.activeSelf) return;

        if(timer >= 1){
            dragCountdownText.text = timer.ToString();
            if(isAccelerating && jumpStartedGameObj != null && !jumpStartedGameObj.activeSelf)
                StartCoroutine(jumpStartedCo());
        }
        else if(dragRaceCo == null)
            dragRaceCo = StartCoroutine(startDragRaceTextCo());

    }
    /// numărătoarea inversă a textului start in cursa drag
    IEnumerator startDragRaceTextCo(){
       dragCountdownText.text = "START!";
       yield return new WaitForSeconds(.5f);
       dragCountdownText.gameObject.SetActive(false);
       dragRaceCo = null;
    }
    /// Afișă câstigătorul cursei
    void displayDragWinner(RaceWinner raceWinner){
        if(dragCountdownText == null) return;
        StartCoroutine(startDragRaceTextCo(raceWinner));
    }
    /// Afișă playerul dacă a câstigat sau nu + numărătoarea inversă a textului afișat
    IEnumerator startDragRaceTextCo(RaceWinner raceWinner){
        string winner = "";
        switch(raceWinner){
            case RaceWinner.Player: winner = "You Won"; break;
            case RaceWinner.AI: winner = "You Lost"; break;
        }
        dragCountdownText.text = winner;
        dragCountdownText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        dragCountdownText.gameObject.SetActive(false);
        dragCountdownText.text = "";
        dragRaceCo = null;
    }

    /// Afișă un text funny daca playerul pornește mai repede, decât cursa să fie pornită
     IEnumerator jumpStartedCo(){
        jumpStartedGameObj.SetActive(true);
        yield return new WaitForSeconds(1f);
        jumpStartedGameObj.SetActive(false);
    }
}