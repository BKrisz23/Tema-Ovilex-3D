using Vehicles;
using UnityEngine;
using PG;
using Unity.Mathematics;

public class Car : Vehicle {
    [SerializeField] CarController carController;
    VehicleWorkAround vehicleWorkaround;
    public override void Awake() {
        base.Awake();

        vehicleWorkaround = new VehicleWorkAround(carController,vehicleControllerCorrection,userInput,driveDirection);
        vehicleFuel = new VehicleFuel(vehicleControllerCorrection.GetFuelCapacity, vehicleControllerCorrection.GetFuelConsumption);
    }

    ///Third party controller workaround
    void Update() { 
        vehicleWorkaround.Execute();
        vehicleFuel.Consume();
    }
}