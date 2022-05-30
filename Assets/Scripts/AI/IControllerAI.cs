using Vehicles;
using UnityEngine;
namespace AI
{
    public interface IControllerAI
    {
        Transform Transform {get;}

        bool Accelerate {get;set;}
        bool Break {get;set;}
        
        bool ForceStop {get; set;}

        IHeadLights HeadLights {get;}

    }
}