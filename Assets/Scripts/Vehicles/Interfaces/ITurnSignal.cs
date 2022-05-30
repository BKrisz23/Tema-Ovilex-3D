using System;

namespace Vehicles{
    public interface ITurnSignal
    {
        Action<IndicatorDirection> OnStateChange { get; set; }
        void SetIndicatorState(IndicatorDirection dir);
        void Reset();
    }
}