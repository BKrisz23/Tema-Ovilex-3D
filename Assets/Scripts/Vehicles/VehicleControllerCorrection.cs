using UnityEngine;

[CreateAssetMenu(fileName = "VehicleControllerCorrection", menuName = "Vehicle/VehicleControllerCorrection", order = 0)]
public class VehicleControllerCorrection : ScriptableObject {
    [SerializeField] AnimationCurve steerPercentCurve;
    [SerializeField] AnimationCurve breakingCurve;

    [Header("Fuel")]
    [SerializeField] float fuelCapacity;
    public float GetFuelCapacity => fuelCapacity;
    [SerializeField] float fuelConsumption;
    public float GetFuelConsumption => fuelConsumption;

    public float GetSteerPercent(float time){
        return steerPercentCurve.Evaluate(time);
    }
    public float GetBreakPower(float time){
        return breakingCurve.Evaluate(time);
    }
}