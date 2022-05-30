using UnityEngine;

[CreateAssetMenu(fileName = "VehicleMaterials", menuName = "Vehicle/Materials", order = 0)]
public class VehicleMaterials : ScriptableObject {
    [SerializeField] Material defaultMaterial;
    public Material GetDefaultMaterial => defaultMaterial;
    [SerializeField] Material turnSignal;
    public Material GetTurnSignal => turnSignal;
    [SerializeField] Material breakLight;
    public Material GetBreakLight => breakLight;
}