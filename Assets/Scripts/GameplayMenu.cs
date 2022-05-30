using UnityEngine;
using Vehicles;

public class GameplayMenu : MonoBehaviour {
    [SerializeField] GameObject menuPanel;

    IFuelRefference fuelRefference;

    public void ToggleMenuPanel(){
        menuPanel.SetActive(!menuPanel.activeSelf);
    }

    public void SetMenuPanelState(bool state){
        menuPanel.SetActive(state);
    }

    public void SetFuelRefference(Transform vehicleT){
        fuelRefference = vehicleT.GetComponentInChildren<VehicleUIRefference>();
    }
    public void ToggleFuel(){
        fuelRefference.GetFuelPanel.SetActive(!fuelRefference.GetFuelPanel.activeSelf);
    }
}