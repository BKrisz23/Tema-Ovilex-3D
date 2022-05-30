
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PG
{
    /// <summary>
    /// Main car controller component. 
    /// It is partial, it also has two parts Engine, Transmission and Steering (For better code readability).
    /// </summary>
    [RequireComponent (typeof (Rigidbody))]
    public partial class CarController :VehicleController
    {
        [Header("CarController")]

        public LayerMask TrailerConnectorMask;
        public Transform TrailerConnectorPosition;

        public float SteerWheelMaxAngle;                                                //Maximum angle of steering wheel rotation (Visual only).
        public Transform SteerWheel;

        float SteerWheelStartXAngle;

        public event System.Action OnConnectTrailer;                                    //Actions to be taken when TrailerController is connected to a vehicle.
        public bool CanConnectTrailer { get; private set; }
        public TrailerController ConnectedTrailer { get; private set; }
        public TrailerController NearestTrailer { get; private set; }
        public ICarControl CarControl { get; set; }                                     //ICarControll controls the car.
        public bool BlockControl { get; protected set; }                                //Blocks input.

        protected override void Awake ()
        {
            base.Awake ();

            if (CarControl == null)
            {
                CarControl = GetComponent<ICarControl> ();
            }

            //Calling Awake in other parts of the component.
            AwakeTransmition ();
            AwakeEngine ();
            AwakeSteering ();

            if (SteerWheel)
            {
                SteerWheelStartXAngle = SteerWheel.localRotation.eulerAngles.x;
            }
        }

        protected override void FixedUpdate ()
        {
            ///Workaround
            if(ForceStop) {
                EngineRPM = Engine.MinRPM;
                return;
            }

            base.FixedUpdate ();

            //Calling FixedUpdate in other parts of the component.
            FixedUpdateEngine ();
            FixedUpdateTransmition ();
            FixedUpdateBrakeLogic ();
            FixedUpdateSteering ();

            //Steering wheel rotation.
            if (SteerWheel != null)
            {
                SteerWheel.transform.localRotation = Quaternion.AngleAxis (SteerWheelStartXAngle, Vector3.right);
                SteerWheel.transform.localRotation *= Quaternion.AngleAxis ((CurrentSteerAngle / Steer.MaxSteerAngle) * SteerWheelMaxAngle, Vector3.back);
            }
        }

        protected override void OnTriggerEnter (Collider other)
        {
            if (TrailerConnectorMask.LayerInMask (other.gameObject.layer) && other.attachedRigidbody)
            {
                CanConnectTrailer = true;
                NearestTrailer = other.attachedRigidbody.GetComponent<TrailerController> ();
            }
        }

        protected override void OnTriggerExit (Collider other)
        {
            if (ConnectedTrailer == null && TrailerConnectorMask.LayerInMask (other.gameObject.layer))
            {
                CanConnectTrailer = false;
                NearestTrailer = null;
            }
        }

        public virtual void TryConnectDisconnectTrailer ()
        {
            if (!ConnectedTrailer && !NearestTrailer)
            {
                return;
            }

            if (ConnectedTrailer)
            {
                ConnectedTrailer.ConnectVehicle (null);
                ConnectedTrailer = null;
            }
            else
            {
                ConnectedTrailer = NearestTrailer;
                ConnectedTrailer.ConnectVehicle (this);
            }

            OnConnectTrailer.SafeInvoke ();
        }

        /// <summary>
        /// Reset car logic.
        /// TODO Add a car reset on the way.
        /// </summary>
        public override void ResetVehicle ()
        {
            base.ResetVehicle ();

            EngineRPM = Engine.MinRPM;
            CurrentGear = 0;
        }
    }

    /// <summary>
    /// Car control interface. Suitable for creating AI.
    /// </summary>
    public interface ICarControl
    {
        float Acceleration { get; }
        float BrakeReverse { get; }
        float Horizontal { get; }
        float Pitch { get; }
        bool HandBrake { get; }
        bool Boost { get; }
    }
}
