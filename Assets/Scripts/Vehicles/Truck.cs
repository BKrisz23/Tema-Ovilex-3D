using Vehicles;
using UnityEngine;
using PG;
public class Truck : Vehicle {
    [SerializeField] CarController carController;
    VehicleWorkAround vehicleWorkaround;
    void Start() {
        vehicleWorkaround = new VehicleWorkAround(carController,vehicleControllerCorrection,userInput,driveDirection);
        vehicleFuel = new VehicleFuel(vehicleControllerCorrection.GetFuelCapacity, vehicleControllerCorrection.GetFuelConsumption);
    }

    ///Third party controller workaround
    void Update() { 
        vehicleWorkaround.Execute();
        vehicleFuel.Consume();
    }
}