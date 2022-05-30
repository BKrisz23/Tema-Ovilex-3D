using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG 
{
    public class TrailerController :VehicleController
    {
        [Header("TrailerController")]
#pragma warning disable 0649

        [SerializeField] Transform TrailerConnectorPosition;
        [SerializeField] GameObject TrailerSupportObject;
        [SerializeField] Vector3 ConnectedSupportPosition;
        [SerializeField] Vector3 DisconnectedSupportPosition;
        [SerializeField] float ConnectSpeed = 2f;

        [SerializeField]
        ConfigurableJointConfig JointConfig = new ConfigurableJointConfig()
        {
            Axis = Vector3.up,
            XMotion = ConfigurableJointMotion.Locked,
            YMotion = ConfigurableJointMotion.Limited,
            ZMotion = ConfigurableJointMotion.Locked,
            AngularXMotion = ConfigurableJointMotion.Limited,
            AngularYMotion = ConfigurableJointMotion.Limited,
            AngularZMotion = ConfigurableJointMotion.Limited,
            HighAngularXLimit = 100,
            LowAngularXLimit = -100,
            AngularYLimit = 20,
            AngularZLimit = 13,
            BrakeForce = 5000000,
            BreakTorque = 5000000
        };

#pragma warning restore 0649

        CarLighting CarLighting;
        CarController ConnectedToCar;
        ConfigurableJoint ConfigurableJoint;
        Vector3 SupportTargetPos;

        protected override void Awake ()
        {
            base.Awake ();

            if (!TrailerSupportObject)
            {
                Debug.LogErrorFormat ("[{0}] trailer without TrailerSupportObject", name);
            }

            SupportTargetPos = DisconnectedSupportPosition;
            if (!CarLighting)
            {
                CarLighting = GetComponent<CarLighting> ();
            }
        }

        protected override void FixedUpdate ()
        {
            base.FixedUpdate ();

            if (TrailerSupportObject && TrailerSupportObject.transform.localPosition != SupportTargetPos)
            {
                TrailerSupportObject.transform.localPosition = 
                    Vector3.MoveTowards (TrailerSupportObject.transform.localPosition, SupportTargetPos, Time.fixedDeltaTime);
            }

            if (!ConfigurableJoint && ConnectedToCar)
            {
                ConnectedToCar.TryConnectDisconnectTrailer ();
            }
            if (ConfigurableJoint && !ConnectedToCar)
            {
                Destroy (ConfigurableJoint);
            }

            float motorTorqueForWheels = 0;
            float currentBrake = 0;

            if (ConfigurableJoint && ConfigurableJoint.linearLimit.limit > 0)
            {
                var limit = ConfigurableJoint.linearLimit;
                if (limit.limit < 0.01f)
                {
                    limit.limit = 0;
                }
                else
                {
                    limit.limit = Mathf.Lerp (limit.limit, 0, Time.fixedDeltaTime * ConnectSpeed);
                }

                ConfigurableJoint.linearLimit = limit;
                motorTorqueForWheels = 0.0000001f;
            }
            else if (ConnectedToCar != null)
            {
                motorTorqueForWheels = ConnectedToCar.CurrentGear != 0 ? 0.0000001f : 0;
                currentBrake = ConnectedToCar.CurrentBrake;
            }

            for (int i = 0; i < Wheels.Length; i++)
            {
                Wheels[i].SetMotorTorque (motorTorqueForWheels, forceSetTroque: true);
                Wheels[i].SetBrakeTorque (currentBrake);
            }
        }

        public void ConnectVehicle (CarController car = null)
        {
            var prevCar = ConnectedToCar;
            ConnectedToCar = car;

            if (prevCar)
            {
                var carLighting = prevCar.GetComponent<CarLighting>();
                carLighting.AdditionalLighting = null;
                CarLighting.SwithOffAllLights ();
            }

            if (ConnectedToCar)
            {
                CreateJoint ();

                var carLighting = ConnectedToCar.GetComponent<CarLighting>();
                carLighting.AdditionalLighting = CarLighting;
            }

            SupportTargetPos = ConnectedToCar? ConnectedSupportPosition: DisconnectedSupportPosition;
        }

        public void CreateJoint ()
        {
            var rotation = transform.rotation;
            transform.rotation = ConnectedToCar.transform.rotation;
            ConfigurableJoint = gameObject.AddComponent<ConfigurableJoint>();
            ConfigurableJoint.configuredInWorldSpace = false;
            ConfigurableJoint.connectedBody = ConnectedToCar.RB;

            ConfigurableJoint.autoConfigureConnectedAnchor = false;
            ConfigurableJoint.connectedAnchor = ConnectedToCar.TrailerConnectorPosition.localPosition;

            ConfigurableJoint.anchor = TrailerConnectorPosition.localPosition;
            ConfigurableJoint.axis = JointConfig.Axis;

            ConfigurableJoint.xMotion = JointConfig.XMotion;
            ConfigurableJoint.yMotion = JointConfig.YMotion;
            ConfigurableJoint.zMotion = JointConfig.ZMotion;
            ConfigurableJoint.angularXMotion = JointConfig.AngularXMotion;
            ConfigurableJoint.angularYMotion = JointConfig.AngularYMotion;
            ConfigurableJoint.angularZMotion = JointConfig.AngularZMotion;

            ConfigurableJoint.highAngularXLimit = new SoftJointLimit () { limit = JointConfig.HighAngularXLimit };
            ConfigurableJoint.lowAngularXLimit = new SoftJointLimit () { limit = JointConfig.LowAngularXLimit };
            ConfigurableJoint.angularYLimit = new SoftJointLimit () { limit = JointConfig.AngularYLimit };
            ConfigurableJoint.angularZLimit = new SoftJointLimit () { limit = JointConfig.AngularZLimit,  };

            transform.rotation = rotation;

            ConfigurableJoint.linearLimit = new SoftJointLimit () { limit = Vector3.Distance(ConnectedToCar.TrailerConnectorPosition.position, TrailerConnectorPosition.position )};
            ConfigurableJoint.linearLimitSpring = new SoftJointLimitSpring () { spring = 10000000f };
            ConfigurableJoint.breakForce = JointConfig.BrakeForce;
            ConfigurableJoint.breakTorque = JointConfig.BreakTorque;

            ConfigurableJoint.enableCollision = true;
            ConfigurableJoint.enableCollision = false;
        }
    }

    [System.Serializable]
    public struct ConfigurableJointConfig
    {
        public Vector3 Axis;
        public ConfigurableJointMotion XMotion;
        public ConfigurableJointMotion YMotion;
        public ConfigurableJointMotion ZMotion;
        public ConfigurableJointMotion AngularXMotion;
        public ConfigurableJointMotion AngularYMotion;
        public ConfigurableJointMotion AngularZMotion;

        public float HighAngularXLimit;
        public float LowAngularXLimit;
        public float AngularYLimit;
        public float AngularZLimit;

        public float BrakeForce;
        public float BreakTorque;
    }
}
