using UnityEngine;
using UniStorm;
using System;

/// Stearea zilei
public enum DayTime { Day, Night }

/// Controlează logica jocului
public class GameController : MonoBehaviour {
    /// Referinţă la texturi lightmap
    [Header("Lightmaps")]
    [SerializeField] Texture2D nightLightmap;
    [SerializeField] Texture2D dayLightmap;
    MeshRenderer[] meshRenderers;

    /// Referință la vehicule
    [Header("Vehicle Prefabs")]
    [SerializeField] GameObject carPrefab;
    [SerializeField] GameObject truckPrefab;
    [SerializeField] GameObject motoPrefab;
    [Space]
    [SerializeField] GameObject aiPrefab;

    /// Referinţă la alte clase
    [Header("Refferences")]
    [SerializeField] GameplayUIController uiController;
    [SerializeField] CameraController cameraController;
    [SerializeField] DragRaceController dragRaceController;

    /// Referinţă la vreme
    [Header("Weather")]
    [SerializeField] UniStormSystem uniStormSystem;
    [SerializeField] WeatherType rainWeather;
    [SerializeField] WeatherType partlyCloudyWeather;

    /// Poziții de start
    [Header("StartPosition")]
    [SerializeField] Vector3 startPosition;
    [SerializeField] Vector3 startPositionAi;
    /// Cache vehicule
    Vehicle car;
    Vehicle truck;
    Vehicle moto;
    /// Cache vihucul activ
    Vehicle activeVehicle;
    GameObject activeVehicleGameObj;
    int vehicleIndex = 1;
    const int maxVehiclesInScene = 2;

    /// Cache vehicul ai
    Transform aiT;
    Vehicle ai;

    /// Event la schimbarea starea zilei
    public static Action<DayTime> OnDayTimeChange;
    DayTime dayTime = DayTime.Day;

    void Start(){

        instantiateVehicles();

        setUpLightMapData();

        cacheStaticMeshRenderers();

        setAIDragRace();
    }
    /// Setează AI-ul pentru cursa drag
    void setAIDragRace(){
        if (dragRaceController != null)
        {
            dragRaceController.SetVehicle(ai);
            aiT = ai.transform;
            DragRaceController.OnDragInitialized += resetVehiclePositions;
        }
    }
    /// Referință pentru a schimba textura zi/noapte
    void cacheStaticMeshRenderers(){
        Transform staticObjects = GameObject.FindGameObjectWithTag("Static").transform;
        meshRenderers = staticObjects.GetComponentsInChildren<MeshRenderer>();
    }
    /// Instantiate vehiculele in scenă + salvează referință
    void instantiateVehicles()
    {
        car = Instantiate(carPrefab, startPosition, Quaternion.identity).GetComponent<Vehicle>();
        if (car != null)
        {
            if (uiController != null) uiController.SetVehicle(car);
            if (cameraController != null) cameraController.SetFollowTarget(car.transform);
            activeVehicleGameObj = car.gameObject;
            activeVehicle = car;
        }

        truck = Instantiate(truckPrefab, startPosition, Quaternion.identity).GetComponent<Vehicle>();
        if (truck != null) truck.gameObject.SetActive(false);

        moto = Instantiate(motoPrefab, startPosition, Quaternion.identity).GetComponent<Vehicle>();
        if (moto != null) moto.gameObject.SetActive(false);

        ai = Instantiate(aiPrefab, startPositionAi, Quaternion.identity).GetComponent<Vehicle>();
    }
    /// Schimbă vehicul cu vehicul următor
    public void SwitchVehicle(){
        if(uiController == null || cameraController == null || car == null || truck == null || activeVehicleGameObj == null || moto == null) return;

        activeVehicleGameObj.SetActive(false);
        activeVehicle.ResetVehicle();
        activeVehicleGameObj.transform.position = startPosition;

        uiController.ResetBeamLightIcon();

        switch(vehicleIndex){
            case 0: uiController.SetVehicle(car);
                    cameraController.SetFollowTarget(car.transform);
                    activeVehicle = car;
                    activeVehicleGameObj = car.gameObject;
                    cameraController.SetFollowOffsetCar(); break;
            case 1: uiController.SetVehicle(truck);
                    cameraController.SetFollowTarget(truck.transform);
                    activeVehicle = truck;
                    activeVehicleGameObj = truck.gameObject;
                    cameraController.SetFollowOffsetTruck(); break;
            case 2: uiController.SetVehicle(moto);
                    cameraController.SetFollowTarget(moto.transform); 
                    activeVehicle = moto;
                    activeVehicleGameObj = moto.gameObject; 
                    cameraController.SetFollowOffsetCar(); break;
        }

        activeVehicleGameObj.SetActive(true);

        vehicleIndex++;
        if(vehicleIndex > maxVehiclesInScene)
            vehicleIndex = 0;
    }
    /// Setează starea zilei în zi
    public void SetDayTime(){
        if(dayTime == DayTime.Day) return;

        if(uniStormSystem != null) {
            uniStormSystem.m_TimeFloat = .5f;
            dayTime = DayTime.Day;
            OnDayTimeChange?.Invoke(dayTime);
        }

        if(ai == null) return;
        for (int i = 0; i < 2; i++)
            {
                ai.ToggleBeamLights();
            }
    }
    /// Setează starea zilei in noapte
    public void SetNightTime(){
        if(dayTime == DayTime.Night) return;

        if(uniStormSystem != null){
            uniStormSystem.m_TimeFloat = .2f;
            dayTime = DayTime.Night;
            OnDayTimeChange?.Invoke(dayTime);

            if(ai == null) return;
            ai.ToggleBeamLights();
        }  
    }
    /// Setează vremea : ploaie
    public void SetRainWeather(){
         if(uniStormSystem != null && rainWeather != null) uniStormSystem.ChangeWeather(rainWeather);
    }
    /// Setează vremea : soare cu puțină nori
    public void SetPartlyCloudyWeather(){
         if(uniStormSystem != null && partlyCloudyWeather != null) uniStormSystem.ChangeWeather(partlyCloudyWeather);
    }
    /// Resetază poziția vehiculu activ
    void resetVehiclePositions(){
        if(activeVehicle == null || aiT == null) return;
        activeVehicle.transform.position = startPosition;
        aiT.position = startPositionAi;

        activeVehicle.ResetVehicle();

        if(uiController == null) return;
        uiController.ResetDriveDirection();
    }
    /// Crează lightmapul
    void setUpLightMapData(){
        LightmapData[] lightmapData = new LightmapData[2];

        lightmapData[0] = new LightmapData();
        lightmapData[0].lightmapColor = dayLightmap;
        lightmapData[0].lightmapDir = dayLightmap;

        lightmapData[1] = new LightmapData();
        lightmapData[1].lightmapColor = nightLightmap;
        lightmapData[1].lightmapDir = nightLightmap;

        LightmapSettings.lightmaps = lightmapData;
    }
    /// Schimbă indexul lightmapului pe obiectele statice pe noapte
    public void ChangeMeshLightDataIndex_Night(){
        if(meshRenderers.Length <=0 ) return;

        for (int i = 0; i < meshRenderers.Length; i++)
        {   
            meshRenderers[i].lightmapIndex = 1;
        } 
    }
    /// Schimbă indexul lightmapului pe obiectele statice pe zi
    public void ChangeMeshLightDataIndex_Day(){
        if(meshRenderers.Length <=0 ) return;

        for (int i = 0; i < meshRenderers.Length; i++)
        {   
            meshRenderers[i].lightmapIndex = 0;
        } 
    }
}