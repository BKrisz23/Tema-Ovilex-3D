using UnityEngine;
namespace Vehicles{

    [CreateAssetMenu(fileName = "VehicleUIHighlight", menuName = "Vehicle/UIHighlights", order = 0)]
    public class VehicleUIHighlight : ScriptableObject {
        [SerializeField] Color standard;
        public Color GetStandard => standard;
        [SerializeField] Color primaryHighlight;
        public Color GetPrimaryHighlight => primaryHighlight;
        [SerializeField] Color secondaryHighlight;
        public Color GetSecondaryHighlight => secondaryHighlight;
    }
}