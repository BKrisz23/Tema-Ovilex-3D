using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using AI;
/// Clasa principală pentru vehicule
/// Acest abordare este special pentru acest proiect mini
public class VehicleAIOld : MonoBehaviour {
    /// Informații necesare pentru UI
    public struct UpdateUI{
        public float Fuel;
        public float Speed;
    }

    enum BlinkDirection { None, Left, Right, All } /// Direcția semnalizării
    public enum BeamLight { None, Short, Long } /// Poziția farurilor
    enum DriveDirection { Forward, Backward } /// Direcția accelerației

    [Header("Vehicle")] /// Referință legate de Rigidbody
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform centerOfMassPosition;

    [Header("Wheels")] /// Referință legate de colliddere
    [SerializeField] WheelCollider[] wheelColliders;
    [SerializeField] Transform[] wheelTransforms;

    [Header("Acceleration")] /// Forță de acccelerație
    [SerializeField] AnimationCurve accelerationMap; /// info de accelerație
    public bool Accelerate {get; set;}
    float torqueTimer; /// timpul pentru accelerationMap
    float motorTorque; /// forța curenta din accelerationMap 
    float reverseTorqueTimeDivider = 4f; /// limită forța în reverse

    [Header("Breaking")]  /// Forță de frânare
    [SerializeField] AnimationCurve breakMap; /// Info de frânare
    [Space]
    [Tooltip("Low values = strong engine break / High values = soft engine break")]
    [SerializeField] [Range(2f,20f)] float engineBreak; /// Forța frânei de motor
    public bool Break {get;set;}
    float breakTorqueTimer; /// Timpul pentru breakMap
    float currentBreakTorque; /// Forța din breakMap

    [Header("Gears")] /// Referință de gears
    [SerializeField] AnimationCurve gearMap; /// Info de gear
    [SerializeField] AnimationCurve reverseRpm; /// Limită RPM-ul in reverse
    [SerializeField] int gear;
    [SerializeField] float currentRpm;
    [SerializeField] float[] shiftTimes; /// Timpul când schimbă viteza
    [SerializeField] float lastGear; /// Cache de viteză pentru sunete și effecte
  
    //Speed
    const float speedMultiplier = 80f; /// Multiplicare pentru afișarea vitezei pe UI
    float currentSpeed; 
    public float GetSpeed => currentSpeed;
    public Action<UpdateUI> OnUpdateUI; /// Event pentru UI

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
    [SerializeField] RealisticEngineSound engineSound;
    [SerializeField] AudioClip popSound;
    [SerializeField] AudioClip turnSignalSound;
    [SerializeField] AudioSource audioSource;
    
    UpdateUI updateUI;
    void Start() {
        rb.centerOfMass = centerOfMassPosition.position;
        fuelTank = fuelCapacity;
        updateUI = new UpdateUI();
    }
    void FixedUpdate() {
        if(wheelColliders.Length <=0 ) return; /// Exception handling

        int driveDirectionMultiplier = driveDirection == DriveDirection.Forward ? 1 : -1; /// Direcția convertată in număr

        if(isAutoPilotActive) /// Dezactivarea autopilotului in timpul accelerației sau frânei
            if(Accelerate || Break) isAutoPilotActive = false;
        
        if(Accelerate){ /// Accelerație
            torqueTimer += Time.fixedDeltaTime;
            accelerationTimeElapsed += Time.fixedDeltaTime;
        }
        else if(!isAutoPilotActive) { /// Dacă nu e autopilotul activat, frânare de motor
            torqueTimer -= Time.fixedDeltaTime / engineBreak;
        }
        
        fuelTimer = torqueTimer; /// Pentru consumul de benzină 

        if(Break) breakTorqueTimer += Time.fixedDeltaTime; /// Timpul frânare
        else breakTorqueTimer = 0;
        
        motorTorque = accelerationMap.Evaluate(torqueTimer) * driveDirectionMultiplier; /// Forța motorului 
        currentBreakTorque = breakMap.Evaluate(breakTorqueTimer) * Time.fixedDeltaTime; /// Forța frânei

        if(currentBreakTorque > 0){ /// Frânare
            torqueTimer -= currentBreakTorque;
        }

        breakTorqueTimer = Mathf.Clamp(breakTorqueTimer,0,breakMap.keys[breakMap.length-1].time); /// Limite de timp franare
        
        switch(driveDirection){ /// Având in vedere direcție vehiculului
            case DriveDirection.Forward: /*motorTorque = Mathf.Clamp(motorTorque, 0f, accelerationMap[accelerationMap.length-1].value); /// Limită de forță
                                         torqueTimer = Mathf.Clamp(torqueTimer,0,accelerationMap.keys[accelerationMap.length-1].time);*/ /// Limită de timp
                                         currentRpm = gearMap.Evaluate(torqueTimer) * 1000f; break; /// Setarea RPM-ului
            case DriveDirection.Backward: motorTorque = Mathf.Clamp(motorTorque, -accelerationMap[accelerationMap.length-1].value / reverseTorqueTimeDivider, 0f);  /// Limită de forță
                                          torqueTimer = Mathf.Clamp(torqueTimer,0f, accelerationMap[accelerationMap.length-1].time / reverseTorqueTimeDivider -1f); /// Limită de timp
                                          currentRpm = reverseRpm.Evaluate(torqueTimer) * 1000f; break; /// Setarea RPM-ului 
        }

        currentSpeed = rb.velocity.magnitude * speedMultiplier; /// Calcularea vitezei din viteza rigidbody
        if(currentSpeed <= 1 && torqueTimer > 1) /// Resetează viteza la accident
            torqueTimer = 0;

        for (int i = 0; i < wheelColliders.Length; i++) /// Transmite forța către roți
        {   
            wheelColliders[i].motorTorque = motorTorque;
            // wheelColliders[i].brakeTorque = currentBreakTorque;
        }

        fuelConsumption = fuelConsumptionMap.Evaluate(fuelTimer) * Time.fixedDeltaTime; /// Calculează consumul benzinei
        fuelTank -= fuelConsumption; /// Scade din rezervor

        updateUI.Speed = currentSpeed; /// Setază viteza de UI
        updateUI.Fuel = (1f / fuelCapacity) * fuelTank; /// Setează starea rezervorului

        OnUpdateUI?.Invoke(updateUI); /// Trimite actualizarea către UI

        updateWheelRotations(); /// Actualizează rotația roțiilor

        wheelSmokeEffect(); /// Effect de fum la roți

        if(engineSound != null){ /// Seteză RPM-ul la sunetul third party
            engineSound.engineCurrentRPM = currentRpm;
        }

        if(driveDirection == DriveDirection.Forward){ /// setează gearul
            if(shiftTimes.Length > 0){
                gear = 1;
                for (int i = 0; i < shiftTimes.Length; i++)
                {
                    if(torqueTimer > shiftTimes[i])
                        gear = i + 1;   
                }
            }

            if(gear > lastGear){ /// Actualizează sunetul POP la schimbarea vitezei
                if(audioSource != null){
                    if(popSound != null ){
                        if(audioSource.clip != popSound) audioSource.clip = popSound;
                        if(audioSource.loop) audioSource.loop = false;

                        audioSource.Play();
                    }
                }
                if(exhaustFireVFX.Length > 0){ /// Effect de foc
                    for (int i = 0; i < exhaustFireVFX.Length; i++)
                    {
                        exhaustFireVFX[i].Play();
                    }
                }
            }
            lastGear = gear;
        }
    }
    void wheelSmokeEffect(){ 
        isTyreSmokeActive = currentSpeed <= tyreSmokeVFXResetSpeed; /// Viteza minimă pentru resetarea effectului
        if(isTyreSmokeActive && tyreSmoke.Length > 0 && Accelerate && accelerationTimeElapsed > minAccelerationVFXTime){ /// Verfifică daca este o accelerație brusc
            for (int i = 0; i < tyreSmoke.Length; i++)
            {
                tyreSmoke[i].Play();
            }
        }
    }
    void updateWheelRotations(){ /// Actualizează rotația roțiilor
        if(wheelColliders.Length != wheelTransforms.Length) return;
        for (int i = 0; i < wheelTransforms.Length; i++)
        {
            Vector3 position = wheelTransforms[i].position;
            Quaternion rotation = wheelTransforms[i].rotation;

            wheelColliders[i].GetWorldPose(out position, out rotation);

            wheelTransforms[i].position = position;
            wheelTransforms[i].rotation = rotation;
        }
    }
    public void ToggleAutoPilot(){ /// Comută starea autopilotului 
        if(!isAutoPilotActive && currentSpeed < minAutopilotSpeed || driveDirection != DriveDirection.Forward) return;
        isAutoPilotActive = !isAutoPilotActive;
    }
    public void SetAccelerationState(bool state){ /// Setază starea accelerației 
        Accelerate = state;

        if(!state){
            accelerationTimeElapsed = 0;
        }
    }
    public void SetBreakState(bool state){ /// Setează starea accelerației 
        Break = state;
        if(Break && rb != null) rb.drag = .2f;
        toggleBreakLight(state);
    }
    public void ToggleDriveDirection(){ /// Comută starea direcția vehicului
        if(driveDirection == DriveDirection.Forward) driveDirection = DriveDirection.Backward;
        else if(driveDirection == DriveDirection.Backward) driveDirection = DriveDirection.Forward;

        toggleReverseLight(); /// activează lumine din spate
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
            groundLight.enabled = false;
             break;
        }   

        return beamLight;
    }
    public void ToggleBlinkLights_Left(){ /// Comută starea semnalizației stânga

        if(defaultBlinkMaterial == null || blinkMaterial == null) return; /// Exception handling

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
    }
    public void ToggleBlinkLights_Right(){ /// Comută starea semnalizației dreapta

        if(defaultBlinkMaterial == null || blinkMaterial == null) return; /// Exception handling

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
    }
    public void ToggleBlinkLights_Hazard(){ /// /// Comută starea avariei

        if(defaultBlinkMaterial == null || blinkMaterial == null) return; /// Exception handling

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
    void toggleBreakLight(bool isActive){ /// Comută materialul de frână lumină in spate
        if(breakLights == null || breakMaterial == null || defaultBreakMaterial == null) return;

        if(isActive) breakLights.material = breakMaterial;
        else breakLights.material = defaultBreakMaterial;
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
    public bool IsVehicleStopped(){ /// Returnează daca vehicul e oprit
        return motorTorque == 0;
    }
    public void ResetVehicle(){ /// Resetază vehicul la calorile inițiale
        blinkDirection = BlinkDirection.None;
        stopBlinking();

        driveDirection = DriveDirection.Forward;

        if(breakLights != null && defaultBreakMaterial != null)
        breakLights.material = defaultBreakMaterial;
        if(reverseLights != null && defaultReverseLightMaterial != null)
        reverseLights.material = defaultReverseLightMaterial;
        
        SetAccelerationState(false);
        SetBreakState(false);

        torqueTimer = 0;
        motorTorque = 0;
        breakTorqueTimer = 0;
        accelerationTimeElapsed = 0;
        currentSpeed = 0;
        currentRpm = 0;


        if(wheelColliders.Length > 0)
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                wheelColliders[i].brakeTorque = 0;
                wheelColliders[i].motorTorque = 0;
            }

        if(rb != null){
            rb.velocity = Vector3.zero;
            rb.drag = 0.1f;
        } 
    }
    public bool CanVehicleReverse(){ /// Returnează daca vehicul are marșarier
        return canReverse;
    }
    public void SetKinematicState(bool state){
        if(rb == null) return;
        rb.isKinematic = state;
    }
}