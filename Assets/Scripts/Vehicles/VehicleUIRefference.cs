using UnityEngine;
using UnityEngine.UI;

namespace Vehicles{
    public interface IFuelRefference {
        GameObject GetFuelPanel {get;}
    }
    public class VehicleUIRefference : MonoBehaviour, IFuelRefference {
        [Header("TurnIndicators")]
        [SerializeField] Image leftTurnIndicator;
        public Image GetLeftTurnIndicator => leftTurnIndicator;
        [SerializeField] Image rightTurnIndicator;
        public Image GetRightTurnIndicator => rightTurnIndicator;
        [SerializeField] Image hazardIndicator;
        public Image GetHazardIndicator => hazardIndicator;

        [Header("Headlight")]
        [SerializeField] Image headlight;
        public Image GetHeadLight => headlight;
        [SerializeField] Sprite headLight_Off;
        public Sprite GetHeadlight_Off => headLight_Off;
        [SerializeField] Sprite headlight_Low;
        public Sprite GetHeadlight_Low => headlight_Low;
        [SerializeField] Sprite headlight_High;
        public Sprite GetHeadlight_High => headlight_High;

        [Header("AutoPilot")]
        [SerializeField] Image autoPilot;
        public Image GetAutoPilot => autoPilot;

        [Header("DriveDirection")]
        [SerializeField] RectTransform driveSelectorHandleRect;
        public RectTransform GetDriveSelectorHandleRect => driveSelectorHandleRect;
        [SerializeField] Image driveDirection;
        public Image GetDriveDirection => driveDirection;
        [Space]
        [SerializeField] float driveSelectorHandle_Top;
        public float GetDriveSelectorHandle_Top => driveSelectorHandle_Top;
        [SerializeField] float driveSelectorHandle_Bot;
        public float GetDriveSelectorHandle_Bot => driveSelectorHandle_Bot;

        [Header("Camera Change")]
        [SerializeField] Image cameraChange;
        public Image GetCameraChange => cameraChange;
        [Header("Fuel")]
        [SerializeField] GameObject fuelPanel;
        public GameObject GetFuelPanel => fuelPanel;
        [SerializeField] Image fuelImage;
        public Image GetFuelImage => fuelImage;

        public void SetStandardColors(Color color){
            leftTurnIndicator.color = color;
            rightTurnIndicator.color = color;
            hazardIndicator.color = color;
            headlight.color = color;
            autoPilot.color = color;
        }
    }
}