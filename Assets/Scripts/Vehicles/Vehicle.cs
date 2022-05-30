using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PG;

namespace Vehicles{
    public interface IVehicle {
        Transform Transform {get;}
        bool ForceStop {get; set;}
        IVehicleCamera VehicleCamera {get;}
        IInput Input {get;}
    }
    public interface IInput {
        void Disable();
        void Enable();
    }
    [RequireComponent(typeof(AudioSource))]
    public class Vehicle : MonoBehaviour, IVehicle, IInput {
        
        public Transform Transform {get; private set;}

        [Header("Refferences")]
        [SerializeField] VehicleMaterials materials;
        [SerializeField] VehicleAudio audios;
        [Header("UI")]
        [SerializeField] VehicleUIRefference uiRefference;
        [SerializeField] VehicleUIHighlight uiHighlight;

        [Header("Third Party Controller")] ///Third Party Workaround
        [SerializeField] VehicleController vehicleController;
        [SerializeField] protected UserInput userInput;
        [SerializeField] protected VehicleControllerCorrection vehicleControllerCorrection;

        ITurnSignal turnSignal;
        IBreakLight breakLight;
        IHeadLights headLights;
        protected IVehicleFuel vehicleFuel;
        protected IDriveDirection driveDirection;
        IVehicleAudioController vehicleAudioController;

        protected IVehicleUI vehicleUI;

        public IVehicleCamera VehicleCamera {get; private set;}
        public bool ForceStop {get; set;}

        public IInput Input {get; private set;}

        public virtual void Awake(){
            Input = this;
            VehicleCamera = GetComponentInChildren<IVehicleCamera>();
            Transform = this.transform;
            turnSignal = new TurnSignal(Transform, this, materials);
            breakLight = new BreakLight(Transform, materials);
            headLights = new HeadLights(Transform);
            driveDirection = new DriveDirection();
            
            AudioSource audioSource = GetComponent<AudioSource>();
            vehicleAudioController = new VehicleAudioController(audioSource, audios);

            vehicleUI = new VehicleUI(uiHighlight,uiRefference);
            uiRefference.SetStandardColors(uiHighlight.GetStandard);

            uiRefference.GetLeftTurnIndicator.GetComponent<Button>().onClick.AddListener(()=> turnSignal.SetIndicatorState(IndicatorDirection.Left));
            uiRefference.GetRightTurnIndicator.GetComponent<Button>().onClick.AddListener(()=> turnSignal.SetIndicatorState(IndicatorDirection.Right));
            uiRefference.GetHazardIndicator.GetComponent<Button>().onClick.AddListener(()=> turnSignal.SetIndicatorState(IndicatorDirection.All));

            uiRefference.GetHeadLight.GetComponent<Button>().onClick.AddListener(()=> headLights.Toggle());
            uiRefference.GetCameraChange.GetComponent<Button>().onClick.AddListener(() => VehicleCamera.SetNextCamera());

            turnSignal.OnStateChange += vehicleUI.SetIndicatorColor;
            turnSignal.OnStateChange += vehicleAudioController.PlayTurnSignal;
            headLights.OnStateChange += vehicleUI.SetHeadlightColor;


            ///Third Party Workaround
            userInput.OnBreakStateChange += breakLight.SetBreakMaterial;
            userInput.OnBreakStateChange += (bool state)=> {
                if(vehicleController.IsAutoPilotActive) vehicleController.IsAutoPilotActive = false;
            };

            userInput.OnAccelerationStateChange += ()=> {
                if(vehicleController.IsAutoPilotActive) vehicleController.IsAutoPilotActive = false;
            };

            vehicleController.OnAutoPilotStateChange += vehicleUI.SetAutoPilotColor;
            uiRefference.GetAutoPilot.GetComponent<Button>().onClick.AddListener(()=> vehicleController.IsAutoPilotActive = !vehicleController.IsAutoPilotActive);
            
            uiRefference.GetDriveDirection.GetComponent<Button>().onClick.AddListener(()=> driveDirection.ToggleDriveDirection(vehicleController.SpeedInHour));
            driveDirection.OnDriveDirectionStateChange += vehicleUI.SetDriveSelectorHandle;
        }

        void FixedUpdate() {
            if(ForceStop)
                vehicleController.RB.velocity = Vector3.zero;   
        }

        public void Disable(){
            vehicleController.ForceStop = true;
            userInput.enabled = false;
            uiRefference.SetActive(false);
            vehicleController.RB.isKinematic = true;
            userInput.OnBreakStateChange -= breakLight.SetBreakMaterial;
            vehicleAudioController.Disable();
            vehicleFuel.OnUpdateUI -= vehicleUI.UpdateFuelImage;
        }
        public void Enable(){
            vehicleController.ForceStop = false;
            userInput.enabled = true;
            uiRefference.SetActive(true);
            vehicleController.RB.isKinematic = false;
            userInput.OnBreakStateChange += breakLight.SetBreakMaterial;
            vehicleController.IsAutoPilotActive = false;
            vehicleAudioController.Enable();
            vehicleFuel.OnUpdateUI += vehicleUI.UpdateFuelImage;
        }
    }
}