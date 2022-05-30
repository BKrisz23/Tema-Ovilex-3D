using UnityEngine;
using System;
using Vehicles;
using AI;
using System.Collections;
/// Stearea zilei
// public enum DayTime { Day, Night }

/// Controlează logica jocului
public class GameController : MonoBehaviour {

    const string RESOURCE_PATH = "Vehicle/";
    const string CAR = "Vehicle_Car";
    const string TRUCK = "Vehicle_Truck";
    const string MOTO = "Vehicle_Moto";
    const string AI_CAR = "Vehicle_Car_AI";

    /// Referinţă la alte clase
    [Header("Refferences")]
    [SerializeField] GameplayMenu gameplayMenu;
    [SerializeField] DragRaceController dragRaceController;
    [SerializeField] WeatherController weatherController;

    /// Poziții de start
    [Header("StartPosition")]
    [SerializeField] Vector3 startPositionAi;
    [SerializeField] Vector3 parkPositionCar;
    [SerializeField] Vector3 parkPositionTruck;
    [SerializeField] Vector3 parkPositionMoto;
    [SerializeField] Vector3 parkRotation;

    int vehicleIndex = 1;
    const int maxVehiclesInScene = 2;

    /// Cache vehicul ai
    IVehicleCamera vehicleCamera;
    
    IVehicle iCar;
    IVehicle iTruck;
    IVehicle iMoto;
    public IVehicle ActiveVehicle {get; private set;}
    public IControllerAI AI {get; private set;}

    /// Event la schimbarea starea zilei
    public Action<DayTime> OnDayTimeChange;

    IEnumerator Start(){

        GameObject prefab = Resources.Load<GameObject>(RESOURCE_PATH + CAR);
        iCar = Instantiate(prefab, parkPositionCar,Quaternion.Euler(parkRotation)).GetComponent<IVehicle>();
        ActiveVehicle = iCar;
        ActiveVehicle.VehicleCamera.EnableCameraSystem();
        gameplayMenu.SetFuelRefference(ActiveVehicle.Transform);

        prefab = Resources.Load<GameObject>(RESOURCE_PATH + TRUCK);
        iTruck = Instantiate(prefab, parkPositionTruck, Quaternion.Euler(parkRotation)).GetComponent<IVehicle>();

        prefab = Resources.Load<GameObject>(RESOURCE_PATH + MOTO);
        iMoto = Instantiate(prefab, parkPositionMoto, Quaternion.Euler(parkRotation)).GetComponent<IVehicle>();

        prefab = Resources.Load<GameObject>(RESOURCE_PATH + AI_CAR);
        AI = Instantiate(prefab, startPositionAi, Quaternion.identity).GetComponent<IControllerAI>();
        dragRaceController.SetAiRefference(AI);

        weatherController.OnDayTimeChange += toggleAIHeadlight;

        yield return null;
        iCar.Input.Enable();
        iTruck.Input.Disable();
        iMoto.Input.Disable();
    }
    public void SetNextVehicle(){
        
        ActiveVehicle.VehicleCamera.DisableCameraSystem();
        ActiveVehicle.Input.Disable();

        resetWorldPositions(ActiveVehicle);

        switch(vehicleIndex){
            case 0: ActiveVehicle = iCar; break;
            case 1: ActiveVehicle = iTruck; break;
            case 2: ActiveVehicle = iMoto; break;
        }

        ActiveVehicle.VehicleCamera.EnableCameraSystem();
        ActiveVehicle.Input.Enable();
        gameplayMenu.SetFuelRefference(ActiveVehicle.Transform);

        vehicleIndex++;
        if(vehicleIndex > maxVehiclesInScene)
            vehicleIndex = 0;
    }

    void resetWorldPositions(IVehicle vehicle){
        if(vehicle == iCar) vehicle.Transform.position = parkPositionCar;
        if(vehicle == iTruck) vehicle.Transform.position = parkPositionTruck;
        if(vehicle == iMoto) vehicle.Transform.position = parkPositionMoto;

        vehicle.Transform.rotation = Quaternion.Euler(parkRotation);
    }

    void toggleAIHeadlight(DayTime dayTime){
        switch(dayTime){
            case DayTime.Day: AI.HeadLights.Reset();break;
            case DayTime.Night: AI.HeadLights.Toggle(); break;
        }
    }
}