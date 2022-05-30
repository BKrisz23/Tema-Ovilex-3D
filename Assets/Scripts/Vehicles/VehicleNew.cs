using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using PG;
using Unity.Mathematics;
/// Clasa principală pentru vehicule
/// Acest abordare este special pentru acest proiect mini
public class VehicleNew : MonoBehaviour {
    /// Informații necesare pentru UI
    public struct UpdateUI{
        public float Fuel;
        public bool IsAutoPilotActive;
    }

    [SerializeField] Rigidbody rb;
    [Header("Steer")]
    [SerializeField] Wheel[] turningWheels;
    [SerializeField] AnimationCurve steerPercentCurve;

    [Header("BreakTorque")]
    [SerializeField] Wheel[] breakingWheels;
    [SerializeField] AnimationCurve breakingCurve;

    public enum BlinkDirection { None, Left, Right, All } /// Direcția semnalizării
    public enum BeamLight { None, Short, Long } /// Poziția farurilor
    enum DriveDirection { Forward, Backward } /// Direcția accelerației
    public Action<UpdateUI> OnUpdateUI; /// Event pentru UI

    [Header("PlayerController")]
    [SerializeField] Transform playerControllerT;
    [SerializeField] CarController carController;
    [SerializeField] BikeController bikeController;
    [SerializeField] UserInput userInput;
    public float GetSpeed {
        get{ 
            if(carController != null) 
                return carController.SpeedInHour;
            if(bikeController != null) 
                return bikeController.SpeedInHour;

            return 0;
        }
    }
    [Header("Fuel")] /// Rezervorul de benzină
    [SerializeField] [Range(10,600)] float fuelCapacity = 65; /// Capacitatea maximă
    [SerializeField] AnimationCurve fuelConsumptionMap; /// Consumul benzinei
    float fuelTank;
    float fuelConsumption;
    float fuelTimer; /// timpul pentru fuelConsumptionMap

    [Header("DriveDirection")] /// Selectarea direcției
    [SerializeField] bool canReverse = true;
    DriveDirection driveDirection = DriveDirection.Forward;

    [Header("Autopilot")] /// Pilotul automat
    [SerializeField] [Range(5,20)] int minAutopilotSpeed = 10; /// Viteza minimă pentru activarea pilotului automat
    bool isAutoPilotActive;
    float velocityMagnitude;

    #region Materials

    [Header("Materials")] /// Materiale
    [SerializeField] Material breakMaterial;
    [SerializeField] Material blinkMaterial;
    [SerializeField] Material WhiteMaterialLED;
    [SerializeField] Material defaultBreakMaterial;
    [SerializeField] Material defaultBlinkMaterial;
    [SerializeField] Material defaultReverseLightMaterial;
    #endregion

    #region Lights
    [Header("Break Lights")] /// meshul lumina de frână
    [SerializeField] MeshRenderer breakLights;

    [Header("Blink Lights")] /// meshuri lumina de semnalizare
    [SerializeField] MeshRenderer[] leftBlinkLights;
    [SerializeField] MeshRenderer[] rightBlinkLights;
    [Space]
    [SerializeField] [Range(0,1f)] float blinkPulseRate = .5f; /// rata de pulsă pentru semnalizare
    List<MeshRenderer> activeBlinkList = new List<MeshRenderer>();
    bool isBlinkActive;
    float blinkPulseTimer;
    IEnumerator updateBlinkLightsCo;
    Coroutine blinkCoroutine;
    BlinkDirection blinkDirection = BlinkDirection.None;
    BeamLight beamLight = BeamLight.None;

    [Header("Beam Lights")] /// faruri
    [SerializeField] MeshRenderer[] beamLightsCone; /// mesh de lumină
    [SerializeField] GameObject[] beamLightsStars; /// effect de lumină
    [SerializeField] MeshRenderer groundLight;

    [Header("Reverse Lights")] /// lumină de reverse
    [SerializeField] MeshRenderer reverseLights; /// mesh lumină reverse
    #endregion
    [Header("VFX")] //effecte
    [SerializeField] ParticleSystem[] tyreSmoke;
    [SerializeField] float maxSmokeSpeed = 30f;
    [SerializeField] [Range(0,1f)] float minAccelerationVFXTime;
    [SerializeField] int tyreSmokeVFXResetSpeed = 10;
    bool isTyreSmokeActive;
    float accelerationTimeElapsed;
    [Space]
    [SerializeField] ParticleSystem[] exhaustFireVFX;

    [Header("Sounds")] /// sunete
    [SerializeField] AudioClip turnSignalSound;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioSource[] controllerAudioSources;
    
    UpdateUI updateUI;
    void Start() {
        fuelTank = fuelCapacity;
        updateUI = new UpdateUI();
        if(playerControllerT != null) playerControllerT.parent = null;

        // DragRaceController.OnDragInitialized += ResetVehicle;
    }
    void Update() {
        if(userInput != null && carController != null){

            if(turningWheels.Length > 0){
                for (int i = 0; i < turningWheels.Length; i++)
                {
                    turningWheels[i].SteerPercent = steerPercentCurve.Evaluate(carController.SpeedInHour);
                }
            }

            if(userInput.BrakeReverse > 0){

                if(breakingWheels.Length > 0){
                    for (int i = 0; i < breakingWheels.Length; i++)
                    {
                        breakingWheels[i].MaxBrakeTorque = breakingCurve.Evaluate(carController.SpeedInHour);
                    }
                }

                SetBreakLightMaterial(true);
                
                if(carController.CurrentGear > 0 && !carController.Gearbox.AutomaticGearBox) carController.Gearbox.AutomaticGearBox = true;
                else if(carController.CurrentGear == 0 && carController.Gearbox.AutomaticGearBox) carController.Gearbox.AutomaticGearBox = false;
                else if(carController.CurrentGear < 0 && carController.Gearbox.AutomaticGearBox) carController.Gearbox.AutomaticGearBox = false;
                
                if(driveDirection == DriveDirection.Forward)
                    carController.CurrentGear = math.clamp(carController.CurrentGear,0,100);
                else
                    carController.CurrentGear = math.clamp(carController.CurrentGear,-1,-1);
                
                //Autopilot
                if(carController.IsAutoPilotActive) carController.IsAutoPilotActive = false;
            }
            else{
                SetBreakLightMaterial(false);
                if(carController.CurrentGear >= 0 && !carController.Gearbox.AutomaticGearBox) carController.Gearbox.AutomaticGearBox = true;
            }

            if(userInput.Acceleration > 0 && carController.IsAutoPilotActive) carController.IsAutoPilotActive = false;

            fuelConsumption = fuelConsumptionMap.Evaluate(carController.SpeedInHour) * Time.deltaTime; /// Calculează consumul benzinei

            updateUI.IsAutoPilotActive = carController.IsAutoPilotActive;
        }

        if(userInput != null && bikeController != null){

            if(turningWheels.Length > 0){
                for (int i = 0; i < turningWheels.Length; i++)
                {
                    turningWheels[i].SteerPercent = steerPercentCurve.Evaluate(bikeController.SpeedInHour);
                }
            }

            if(userInput.BrakeReverse > 0){

                if(breakingWheels.Length > 0){
                    for (int i = 0; i < breakingWheels.Length; i++)
                    {
                        breakingWheels[i].MaxBrakeTorque = breakingCurve.Evaluate(bikeController.SpeedInHour);
                    }
                }

                SetBreakLightMaterial(true);
                
                if(bikeController.CurrentGear > 0 && !bikeController.Gearbox.AutomaticGearBox) bikeController.Gearbox.AutomaticGearBox = true;
                else if(bikeController.CurrentGear == 0 && bikeController.Gearbox.AutomaticGearBox) bikeController.Gearbox.AutomaticGearBox = false;
                else if(bikeController.CurrentGear < 0 && bikeController.Gearbox.AutomaticGearBox) bikeController.Gearbox.AutomaticGearBox = false;
                
                if(driveDirection == DriveDirection.Forward)
                    bikeController.CurrentGear = math.clamp(bikeController.CurrentGear,0,100);
                else
                    bikeController.CurrentGear = math.clamp(bikeController.CurrentGear,-1,-1);
                
                //Autopilot
                if(bikeController.IsAutoPilotActive) bikeController.IsAutoPilotActive = false;
            }
            else{
                SetBreakLightMaterial(false);
                if(bikeController.CurrentGear >= 0 && !bikeController.Gearbox.AutomaticGearBox) bikeController.Gearbox.AutomaticGearBox = true;
            }

            if(userInput.Acceleration > 0 && bikeController.IsAutoPilotActive) bikeController.IsAutoPilotActive = false;

            fuelConsumption = fuelConsumptionMap.Evaluate(bikeController.SpeedInHour) * Time.deltaTime; /// Calculează consumul benzinei
            
            updateUI.IsAutoPilotActive = bikeController.IsAutoPilotActive;
        }


        fuelTank -= fuelConsumption; /// Scade din rezervor
        updateUI.Fuel = (1f / fuelCapacity) * fuelTank; /// Setează starea rezervorului
        OnUpdateUI?.Invoke(updateUI); /// Trimite actualizarea către UI

    }
    public bool ToggleAutoPilot(){
        if(carController != null){
            if(carController.SpeedInHour < minAutopilotSpeed || carController.CurrentGear < 0) return false;
            carController.AutoPilotSpeed = carController.RB.velocity.z;
            carController.IsAutoPilotActive = !carController.IsAutoPilotActive;
            return carController.IsAutoPilotActive;
        }

        if(bikeController != null){
            if(bikeController.SpeedInHour < minAutopilotSpeed || bikeController.CurrentGear < 0) return false;
            bikeController.AutoPilotSpeed = bikeController.RB.velocity.z;
            bikeController.IsAutoPilotActive = !bikeController.IsAutoPilotActive;
            return bikeController.IsAutoPilotActive;
        }

        return false;
    }
    public void SetBreakState(bool state){ /// Setează starea accelerației 
        SetBreakLightMaterial(state);
    }
    public void ToggleDriveDirection(){ /// Comută starea direcția vehicului
        if(driveDirection == DriveDirection.Forward) driveDirection = DriveDirection.Backward;
        else if(driveDirection == DriveDirection.Backward) driveDirection = DriveDirection.Forward;

        toggleReverseLight(); /// activează lumine din spate

        if(carController != null){
            switch(driveDirection){
                case DriveDirection.Forward: carController.CurrentGear = 0; 
                carController.Gearbox.AutomaticGearBox = true; break;
                case DriveDirection.Backward: carController.CurrentGear = -1; 
                carController.Gearbox.AutomaticGearBox = false; break;
            }
        }

         if(bikeController != null){
            switch(driveDirection){
                case DriveDirection.Forward: bikeController.CurrentGear = 0; 
                bikeController.Gearbox.AutomaticGearBox = true; break;
                case DriveDirection.Backward: bikeController.CurrentGear = -1; 
                bikeController.Gearbox.AutomaticGearBox = false; break;
            }
        }


    }
    public BeamLight ToggleBeamLights(){ /// Comută starea farurilor
        if(beamLightsCone.Length <= 0 || beamLightsStars.Length <= 1 || groundLight == null) return BeamLight.None;

        if(beamLight == BeamLight.None) beamLight = BeamLight.Short;
        else if(beamLight == BeamLight.Short) beamLight = BeamLight.Long;
        else if(beamLight == BeamLight.Long) beamLight = BeamLight.None;

        for (int i = 0; i < beamLightsCone.Length; i++)
        {
            if(beamLight == BeamLight.None){
                beamLightsCone[i].enabled = false;
            }else if(beamLight != BeamLight.None && !beamLightsCone[i].enabled){
                beamLightsCone[i].enabled = true;
            }
        }

        switch(beamLight){
            case BeamLight.Short: beamLightsStars[0].SetActive(true); 
                                  groundLight.enabled = true; break;
            case BeamLight.Long: beamLightsStars[1].SetActive(true);break;
            case BeamLight.None: beamLightsStars[0].SetActive(false); 
                                 beamLightsStars[1].SetActive(false);
                                 groundLight.enabled = false; break;
        }   

        return beamLight;
    }
    void disableBeamLights(){
        beamLight = BeamLight.None;
        if(beamLightsCone.Length <= 0 || beamLightsStars.Length <= 1 || groundLight == null) return;
        for (int i = 0; i < beamLightsCone.Length; i++)
        {
            beamLightsCone[i].enabled = false;
        }
        for (int i = 0; i < beamLightsStars.Length; i++)
        {
            beamLightsStars[i].SetActive(false);
        }
        groundLight.enabled = false;
    }
    public BlinkDirection ToggleBlinkLights_Left(){ /// Comută starea semnalizației stânga

        if(defaultBlinkMaterial == null || blinkMaterial == null) return default; /// Exception handling

        toggleTurnSignalSound(true); /// Sunetul semnalizării 

        if(leftBlinkLights.Length > 0){ /// /// Exception handling

            blinkDirection = blinkDirection == BlinkDirection.None || 
                             blinkDirection == BlinkDirection.Right ||
                             blinkDirection == BlinkDirection.All ? BlinkDirection.Left : BlinkDirection.None;

            if(blinkDirection != BlinkDirection.None){
                resetBlinkList(); /// Resetează daca semnalizarea este pornită și direcția schimbată

                activeBlinkList = leftBlinkLights.ToList<MeshRenderer>();
                resetBlinkState(); /// Resetază starea semnalizației + pulsul

                startBlinking(); /// Porneste semnalizarea
            }
        }

        return blinkDirection;
    }
    public BlinkDirection ToggleBlinkLights_Right(){ /// Comută starea semnalizației dreapta

        if(defaultBlinkMaterial == null || blinkMaterial == null) return default; /// Exception handling

        toggleTurnSignalSound(true); /// Sunetul semnalizării 

        if(rightBlinkLights.Length > 0){ /// Exception handling
            blinkDirection = blinkDirection == BlinkDirection.None ||
                             blinkDirection == BlinkDirection.Left ||
                             blinkDirection == BlinkDirection.All ? BlinkDirection.Right : BlinkDirection.None;

            if(blinkDirection != BlinkDirection.None){
                resetBlinkList(); /// Resetează daca semnalizarea este pornită și direcția schimbată

                activeBlinkList = rightBlinkLights.ToList<MeshRenderer>();
                resetBlinkState(); /// Resetază starea semnalizației + pulsul

                startBlinking(); /// Porneste semnalizarea
            }
        }

         return blinkDirection;
    }
    public BlinkDirection ToggleBlinkLights_Hazard(){ /// /// Comută starea avariei

        if(defaultBlinkMaterial == null || blinkMaterial == null) return default; /// Exception handling

        toggleTurnSignalSound(true); /// Sunetul semnalizării 

        if(rightBlinkLights.Length > 0 && leftBlinkLights.Length > 0){ /// Exception handling
            blinkDirection = blinkDirection == BlinkDirection.None ||
                             blinkDirection == BlinkDirection.Left ||
                             blinkDirection == BlinkDirection.Right ? BlinkDirection.All : BlinkDirection.None;

            if(blinkDirection != BlinkDirection.None){
                resetBlinkList(); /// Resetează daca semnalizarea este pornită și direcția schimbată

                activeBlinkList = leftBlinkLights.Concat(rightBlinkLights).ToList<MeshRenderer>();
                resetBlinkState(); /// Resetază starea semnalizației + pulsul

                startBlinking(); /// Porneste semnalizarea
            }
        }

         return blinkDirection;
    }
    void startBlinking(){ /// Porneste semnalizarea
        if(blinkCoroutine != null) return; /// Daca este pornită nu face nimica
        updateBlinkLightsCo = updateBlinkLights();
        blinkCoroutine = StartCoroutine(updateBlinkLightsCo);
    }
    void resetBlinkList(){ /// Resetează daca semnalizarea este pornită și direcția schimbată
        if(activeBlinkList.Count > 0){
            setDefaultBlinkMaterials(); /// Resetează materialul default
            activeBlinkList.Clear();
        }
    }
    void resetBlinkState(){  /// Resetază starea semnalizației + pulsul
        blinkPulseTimer = 0;
        isBlinkActive = false;
    }
    IEnumerator updateBlinkLights(){ /// Schimbă materialul de semnalizare + timerul de pulse

        while(blinkDirection != BlinkDirection.None){

            if(blinkPulseTimer <= 0){
                blinkPulseTimer = blinkPulseRate;
                
                for (int i = 0; i < activeBlinkList.Count; i++)
                {   
                    if(!isBlinkActive){
                        activeBlinkList[i].material = blinkMaterial;
                    }else{
                        activeBlinkList[i].material = defaultBlinkMaterial;
                    }
                }

                isBlinkActive = !isBlinkActive;
            }else{
                blinkPulseTimer -= Time.deltaTime;
            }

            yield return null;

            if(audioSource.clip != turnSignalSound && !audioSource.isPlaying) /// Se asigură că sunetul nu e schimbată cu alt sunet
                toggleTurnSignalSound(true);
        }   

        stopBlinking(); /// Oprește semnalizația
    }
    void stopBlinking(){ /// Oprește semnalizația
        setDefaultBlinkMaterials(); /// Resetează materialul
        activeBlinkList.Clear();
        blinkCoroutine = null;

        toggleTurnSignalSound(false); /// Oprește sunetul
    }
    void setDefaultBlinkMaterials(){ /// Resetează materialul
        for (int i = 0; i < activeBlinkList.Count; i++)
        {
            activeBlinkList[i].material = defaultBlinkMaterial;
        }
    }
    void SetBreakLightMaterial(bool isActive){ /// Comută materialul de frână lumină in spate
        if(breakLights == null || breakMaterial == null || defaultBreakMaterial == null) return;

        if(isActive && breakLights.material != breakMaterial) breakLights.material = breakMaterial;
        else if(!isActive && breakLights.material != defaultBreakMaterial) breakLights.material = defaultBreakMaterial;
    }
    public void toggleReverseLight(){ /// Comută materialul de lumină reverse
        if(reverseLights == null || WhiteMaterialLED == null) return;

        switch(driveDirection){
            case DriveDirection.Forward: reverseLights.material = defaultReverseLightMaterial; break;
            case DriveDirection.Backward: reverseLights.material = WhiteMaterialLED; break;
        }
    }
    void toggleTurnSignalSound(bool isPlaying){ /// Comută sunetul de semnalizare
        if(audioSource == null || turnSignalSound == null) return;

        if(isPlaying){
            if(audioSource.isPlaying && audioSource.clip == turnSignalSound) return;
            audioSource.loop = true;
            audioSource.clip = turnSignalSound;
            audioSource.Play();
        }else{
            audioSource.loop = false;
            audioSource.clip = null;
            audioSource.Stop();
        }
    }
    public void ResetVehicle(bool initializeInputReset){ /// Resetază vehicul la valorile inițiale
        blinkDirection = BlinkDirection.None;
        stopBlinking();

        driveDirection = DriveDirection.Forward;

        if(breakLights != null && defaultBreakMaterial != null)
        breakLights.material = defaultBreakMaterial;
        if(reverseLights != null && defaultReverseLightMaterial != null)
        reverseLights.material = defaultReverseLightMaterial;
        
        accelerationTimeElapsed = 0;

        SetBreakState(false);
        disableBeamLights();


        if(carController != null) {
            carController.IsAutoPilotActive = false;
            carController.StopVehicle();
            if(initializeInputReset)
            if(userInput != null) StartCoroutine(resetInput());
        }

        if(bikeController != null) {
            bikeController.IsAutoPilotActive = false;
            bikeController.StopVehicle();
            if(initializeInputReset)
            if(userInput != null) StartCoroutine(resetInput());
        }
        
    }

    IEnumerator resetInput(){
        if(carController != null)
            carController.SetKinematicState(true);

        if(bikeController != null)
            bikeController.SetKinematicState(true);

        if(breakingWheels.Length > 0){
            for (int i = 0; i < breakingWheels.Length; i++)
            {
                breakingWheels[i].MaxBrakeTorque = 5000;
            }
        }

        userInput.BrakeReverse = 1;
        userInput.Acceleration = 0;
        yield return new WaitForSeconds(1f);
        userInput.BrakeReverse = 0;

        if(carController != null)
            carController.SetKinematicState(false);

        if(bikeController != null)
            bikeController.SetKinematicState(false);

    }

    public bool CanVehicleReverse(){ /// Returnează daca vehicul are marșarier
        return canReverse;
    }
    public bool IsVehicleStopped(){
        if(carController != null)
            if(carController.SpeedInHour > 1f) return false;

        if(bikeController != null)
            if(bikeController.SpeedInHour > 1f) return false;

        return true;
    }
    public void SetPlayerControllerState(bool state){
        if(playerControllerT == null) return;

        playerControllerT.SetActive(state);
    }
    public void SetCarControllerState(bool state){
        if(carController != null)
            carController.enabled = state;

        if(bikeController != null)
            bikeController.enabled = state;
    }

    public void SetControllerAudioSourceStates(bool state){
        if(controllerAudioSources.Length <= 0) return;

        for (int i = 0; i < controllerAudioSources.Length; i++)
        {
            controllerAudioSources[i].enabled = state;
        }
    }

    public void SetKinematicState(bool state){
        if(rb == null) return;
        rb.isKinematic = state;
    }
}
