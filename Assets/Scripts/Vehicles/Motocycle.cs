using Vehicles;
using UnityEngine;
using PG;
public class Motocycle : Vehicle {
    [SerializeField] BikeController bikeController;
    MotoWorkAround motoWorkaround;
    void Start() {
        motoWorkaround = new MotoWorkAround(bikeController,vehicleControllerCorrection,userInput,driveDirection);
        vehicleFuel = new VehicleFuel(vehicleControllerCorrection.GetFuelCapacity, vehicleControllerCorrection.GetFuelConsumption);
    }

    ///Third party controller workaround
    void Update() { 
        motoWorkaround.Execute();
        vehicleFuel.Consume();
    }
}