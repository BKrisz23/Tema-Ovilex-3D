using System;
namespace Vehicles{
    public interface IHeadLights
    {
        Action<Headlight> OnStateChange { get; set; }
        void Reset();
        void Toggle();
    }
}