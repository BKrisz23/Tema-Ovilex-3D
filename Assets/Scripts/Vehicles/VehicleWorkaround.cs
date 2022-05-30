using Vehicles;
using PG;
using Unity.Mathematics;

public interface IVehicleWorkaround{
    void Execute();
}
public class VehicleWorkAround : IVehicleWorkaround
{   
    public VehicleWorkAround(CarController carController,VehicleControllerCorrection vehicleControllerCorrection, UserInput userInput, IDriveDirection driveDirection)
    {
        this.carController = carController;
        this.vehicleControllerCorrection = vehicleControllerCorrection;
        this.userInput = userInput;
        this.driveDirection = driveDirection;
    }

    CarController carController;
    VehicleControllerCorrection vehicleControllerCorrection;
    UserInput userInput;
    IDriveDirection driveDirection;

    public void Execute(){
        for (int i = 0; i < carController.Wheels.Length; i++){
            carController.Wheels[i].MaxBrakeTorque = vehicleControllerCorrection.GetBreakPower(carController.SpeedInHour); // Break smoothing workaround
            if(carController.Wheels[i].IsSteeringWheel){
                carController.Wheels[i].SteerPercent = vehicleControllerCorrection.GetSteerPercent(carController.SpeedInHour); // Steer smoothing workaround
            }
        }

        if(userInput.BrakeReverse > 0){ // disable breakReverse 
                if(carController.CurrentGear > 0 && !carController.Gearbox.AutomaticGearBox) carController.Gearbox.AutomaticGearBox = true;
                else if(carController.CurrentGear <= 0 && carController.Gearbox.AutomaticGearBox) carController.Gearbox.AutomaticGearBox = false;
        }
        
        if(driveDirection.GetPowerDirection == PowerDirection.Forward){
            if(!carController.Gearbox.AutomaticGearBox && carController.CurrentAcceleration > 0) carController.Gearbox.AutomaticGearBox = true;
            carController.CurrentGear = math.clamp(carController.CurrentGear,0,100);
        }
        else{
            if(carController.Gearbox.AutomaticGearBox ) carController.Gearbox.AutomaticGearBox = false;
            carController.CurrentGear = math.clamp(carController.CurrentGear,-1,-1);
        }
    }
}

public class MotoWorkAround : IVehicleWorkaround
{
    public MotoWorkAround(BikeController bikeController,VehicleControllerCorrection vehicleControllerCorrection, UserInput userInput, IDriveDirection driveDirection)
    {
        this.bikeController = bikeController;
        this.vehicleControllerCorrection = vehicleControllerCorrection;
        this.userInput = userInput;
        this.driveDirection = driveDirection;
    }

    BikeController bikeController;
    VehicleControllerCorrection vehicleControllerCorrection;
    UserInput userInput;
    IDriveDirection driveDirection;

    public void Execute(){
        for (int i = 0; i < bikeController.Wheels.Length; i++){
            bikeController.Wheels[i].MaxBrakeTorque = vehicleControllerCorrection.GetBreakPower(bikeController.SpeedInHour); // Break smoothing workaround
            if(bikeController.Wheels[i].IsSteeringWheel){
                bikeController.Wheels[i].SteerPercent = vehicleControllerCorrection.GetSteerPercent(bikeController.SpeedInHour); // Steer smoothing workaround
            }
        }

        if(userInput.BrakeReverse > 0){ // disable breakReverse 
                if(bikeController.CurrentGear > 0 && !bikeController.Gearbox.AutomaticGearBox) bikeController.Gearbox.AutomaticGearBox = true;
                else if(bikeController.CurrentGear <= 0 && bikeController.Gearbox.AutomaticGearBox) bikeController.Gearbox.AutomaticGearBox = false;
        }
        
        if(driveDirection.GetPowerDirection == PowerDirection.Forward){
            if(!bikeController.Gearbox.AutomaticGearBox && bikeController.CurrentAcceleration > 0) bikeController.Gearbox.AutomaticGearBox = true;
            bikeController.CurrentGear = math.clamp(bikeController.CurrentGear,0,100);
        }
        else{
            if(bikeController.Gearbox.AutomaticGearBox ) bikeController.Gearbox.AutomaticGearBox = false;
            bikeController.CurrentGear = math.clamp(bikeController.CurrentGear,-1,-1);
        }
    }
}