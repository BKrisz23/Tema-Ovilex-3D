using UnityEngine;
using Vehicles;

namespace AI {
    [RequireComponent((typeof(Rigidbody)))]
    public class VehicleAI : MonoBehaviour, IControllerAI {
        public Transform Transform { get; private set; }
        Rigidbody rb;

        [Header("Refferences")]
        [SerializeField] VehicleMaterials materials;

        [Header("Wheels")]
        [SerializeField] WheelCollider[] wheelColliders;
        [SerializeField] Transform[] wheelTransforms;

        [Header("Acceleration")] 
        [SerializeField] AnimationCurve accelerationMap; 
        bool isAccelerating;
        float torqueTimer; 
        float motorTorque; 
        float reverseTorqueTimeDivider = 4f; 

        [Header("Breaking")] 
        [SerializeField] AnimationCurve breakMap; /// Info de frânare
        bool isBreaking;
        float breakTorqueTimer; 
        float currentBreakTorque; 
       
        IBreakLight breakLight;
        public IHeadLights HeadLights {get; private set;}

        public bool Accelerate { 
            get {
                return isAccelerating ; }
            set {
                isAccelerating = value;
            }
        }
            
        public bool Break { 
            get {
                return isBreaking ; }
            set {
                isBreaking = value;
                breakLight.SetBreakMaterial(isBreaking);
            }
        }

        public bool ForceStop {get; set;}

        void Awake() {
            Transform = this.transform;
            breakLight = new BreakLight(Transform, materials);
            HeadLights = new HeadLights(Transform);
            rb= GetComponent<Rigidbody>();
        }

        void FixedUpdate() {
            if(ForceStop){
                rb.velocity = Vector3.zero;

                Accelerate = false;
                Break = false;

                torqueTimer = 0;
                breakTorqueTimer = 0;
                currentBreakTorque = 0;
                motorTorque = 0;

                for (int i = 0; i < wheelColliders.Length; i++) /// Transmite forța către roți
                {   
                    wheelColliders[i].motorTorque = 0;
                    wheelColliders[i].brakeTorque = 0;
                }
                
                return;
            }

            if(isAccelerating){ /// Accelerație
                torqueTimer += Time.fixedDeltaTime;
            }
            
            if(isBreaking) breakTorqueTimer += Time.fixedDeltaTime; /// Timpul frânare
            else breakTorqueTimer = 0;
        
            motorTorque = accelerationMap.Evaluate(torqueTimer); /// Forța motorului 
            currentBreakTorque = breakMap.Evaluate(breakTorqueTimer) * Time.fixedDeltaTime; /// Forța frânei

            if(currentBreakTorque > 0){ /// Frânare
                torqueTimer -= currentBreakTorque;
            }

            for (int i = 0; i < wheelColliders.Length; i++) /// Transmite forța către roți
            {   
                wheelColliders[i].motorTorque = motorTorque;
                wheelColliders[i].brakeTorque = currentBreakTorque;
            }

            updateWheelRotations();
        }

        void updateWheelRotations(){ /// Actualizează rotația roțiilor
            if(wheelColliders.Length != wheelTransforms.Length) return;
            for (int i = 0; i < wheelTransforms.Length; i++)
            {
                Vector3 position = wheelTransforms[i].position;
                Quaternion rotation = wheelTransforms[i].rotation;

                wheelColliders[i].GetWorldPose(out position, out rotation);

                wheelTransforms[i].position = position;
                wheelTransforms[i].rotation = rotation;
            }
        }
    }
}