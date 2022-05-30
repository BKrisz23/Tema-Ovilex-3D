using UnityEngine;
using System;

namespace Vehicles
{
    public interface IVehicleFuel
    {
        float GetFuelCapacity {get;}
        void Consume();
        Action<float> OnUpdateUI {get; set;}
    }

    public class VehicleFuel : IVehicleFuel
    {
        public VehicleFuel(float fuelCapacity, float consumption)
        {
            this.fuelCapacity = fuelCapacity;
            this.consumption = consumption;
            tank = fuelCapacity;
        }

        float tank;
        float consumption;
        float fuelCapacity;
        public float GetFuelCapacity => fuelCapacity;
        public Action<float> OnUpdateUI {get; set;}
        public void Consume()
        {
            tank -= consumption * Time.deltaTime;
            OnUpdateUI?.Invoke((1f / fuelCapacity) * tank);
        }
    }
}