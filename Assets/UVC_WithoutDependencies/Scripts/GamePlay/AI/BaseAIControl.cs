using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG 
{
    /// <summary>
    /// Base class AI control.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class BaseAIControl :MonoBehaviour, ICarControl
    {
        public GameBalance.BaseAIConfigAsset AIConfigAsset;                 //Asset with AI config.

        protected BaseAIConfig BaseAIConfig;

        protected float MaxSpeed { get { return BaseAIConfig.MaxSpeed; } }
        protected float MinSpeed { get { return BaseAIConfig.MinSpeed; } }
        protected float ReverceWaitTime { get { return BaseAIConfig.ReverceWaitTime; } }
        protected float ReverceTime { get { return BaseAIConfig.ReverceTime; } }
        protected float BetweenReverceTimeForReset { get { return BaseAIConfig.BetweenReverceTimeForReset; } }

        protected float OffsetToTargetPoint { get { return BaseAIConfig.OffsetToTargetPoint; } }
        protected float SpeedFactorToTargetPoint { get { return BaseAIConfig.SpeedFactorToTargetPoint; } }
        protected float OffsetTurnPrediction { get { return BaseAIConfig.OffsetTurnPrediction; } }
        protected float SpeedFactorToTurnPrediction { get { return BaseAIConfig.SpeedFactorToTurnPrediction; } }
        protected float LookAngleSppedFactor { get { return BaseAIConfig.LookAngleSppedFactor; } }
        protected float SetSteerAngleMultiplayer { get { return BaseAIConfig.SetSteerAngleMultiplayer; } }

        public float Acceleration { get; protected set; }
        public float BrakeReverse { get; protected set; }
        public float Horizontal { get; protected set; }
        public float Pitch { get; protected set; }
        public bool HandBrake { get; protected set; }
        public bool Boost { get; protected set; }

        public CarController Car { get; protected set; }

        protected AITrigger ActiveTrigger;                      //The current trigger the AI is in.

        /// <summary>
        /// The property that changes the Acceleration and BrakeReverse of the car: (1) Acceleration, (-1) Braking / Reverse
        /// </summary>
        protected float Vertical 
        { 
            get 
            { 
                return Acceleration + BrakeReverse; 
            } 
            set 
            {
                Acceleration = Mathf.Max (0, value);
                BrakeReverse = Mathf.Max (0, -value);
            } 
        }

        public virtual void Start ()
        {
            Car = GetComponent<CarController> ();
            Car.CarControl = this;

            if (AIConfigAsset)
            {
                BaseAIConfig = AIConfigAsset.AIConfig;
            }
            else
            {
                Debug.LogError ("AIConfig not found");
                BaseAIConfig = new BaseAIConfig ();
            }
        }

        protected virtual void FixedUpdate ()
        {
        }

        private void OnTriggerEnter (Collider other)
        {
            var trigger = other.GetComponent<AITrigger>();
            if (trigger && trigger != ActiveTrigger)
            {
                SetActiveTrigger (trigger);
            }
        }

        private void OnTriggerExit (Collider other)
        {
            if (ActiveTrigger != null && other.gameObject == ActiveTrigger.gameObject)
            {
                SetActiveTrigger (null);
            }
        }

        //AI Triggers logic, when entering a trigger, it exits from the previous trigger.
        void SetActiveTrigger (AITrigger trigger)
        {
            if (ActiveTrigger)
            {
                if (Boost && ActiveTrigger.Boost)
                {
                    Boost = false;
                }
            }

            ActiveTrigger = trigger;

            if (ActiveTrigger)
            {
                if (ActiveTrigger.Boost && Random.Range(0f, 1f) < ActiveTrigger.BoostProbability)
                {
                    Boost = true;
                }
            }
        }
    }

    [System.Serializable]
    public class BaseAIConfig
    {
        public float MaxSpeed = 80;                                 //Max speed for AI.
        public float MinSpeed = 6;                                  //Min speed for AI. AI adhere to speed in a given range.
        public float SetSteerAngleMultiplayer = 2f;                 //Steer angle multiplier.

        public float OffsetToTargetPoint = 5;                       //Offset to the target point in the direction of the path.
        public float SpeedFactorToTargetPoint = 0.5f;               //A multiplier adding distance to OffsetToTargetPoint, depending on the speed.

        public float OffsetTurnPrediction = 11;                     //Offset to the turn prediction point in the direction of the path.
        public float SpeedFactorToTurnPrediction = 0.6f;            //A multiplier adding distance to OffsetTurnPrediction, depending on the speed.

        public float LookAngleSppedFactor = 30f;                    //Maximum angle to the TurnPredictionPoint, if the current angle is greater than the specified value, then the car will brake.

        public float ReverceWaitTime = 2;                           //If the car does not move directly at a specified time, then it starts to move back.
        public float ReverceTime = 2;                               //Reversing time.
        public float BetweenReverceTimeForReset = 6;                //To reset the position of the AI car.
    }
}
