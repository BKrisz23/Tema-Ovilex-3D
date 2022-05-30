using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vehicles{
    public interface IVehicleUI
    {
        void Reset();
        void SetIndicatorColor(IndicatorDirection dir);
        void SetHeadlightColor(Headlight headlight);
        void SetAutoPilotColor(bool state);
        void SetDriveSelectorHandle(PowerDirection powerDir);
        void UpdateFuelImage(float fillAmount);
    }

    public class VehicleUI : IVehicleUI
    {
        public VehicleUI(VehicleUIHighlight uiHighlight, VehicleUIRefference uiRefference)
        {
            this.uiHighlight = uiHighlight;
            this.uiRefference = uiRefference;
        }

        VehicleUIHighlight uiHighlight;
        VehicleUIRefference uiRefference;

        public void UpdateFuelImage(float fillAmount){
            uiRefference.GetFuelImage.fillAmount = fillAmount;
        }
        public void SetIndicatorColor(IndicatorDirection dir){

            resetIndicators();

            switch(dir){
                case IndicatorDirection.None: break;
                case IndicatorDirection.Left: uiRefference.GetLeftTurnIndicator.color = uiHighlight.GetPrimaryHighlight; break;
                case IndicatorDirection.Right: uiRefference.GetRightTurnIndicator.color = uiHighlight.GetPrimaryHighlight; break;
                case IndicatorDirection.All: uiRefference.GetHazardIndicator.color = uiHighlight.GetPrimaryHighlight; break;
            }
        }

        void resetIndicators(){
            uiRefference.GetLeftTurnIndicator.color = uiHighlight.GetStandard;
            uiRefference.GetRightTurnIndicator.color = uiHighlight.GetStandard;
            uiRefference.GetHazardIndicator.color = uiHighlight.GetStandard;
        }

        public void SetHeadlightColor(Headlight headlight){
            switch(headlight){
                case Headlight.None: resetHeadlight(); break;
                case Headlight.Low: uiRefference.GetHeadLight.sprite = uiRefference.GetHeadlight_Low; 
                                    uiRefference.GetHeadLight.color = uiHighlight.GetSecondaryHighlight; break;
                case Headlight.High: uiRefference.GetHeadLight.sprite = uiRefference.GetHeadlight_High; break;
            }
        }

        void resetHeadlight(){
            uiRefference.GetHeadLight.sprite = uiRefference.GetHeadlight_Off;
            uiRefference.GetHeadLight.color = uiHighlight.GetStandard;
        }

        public void SetAutoPilotColor(bool state){
            if(state) uiRefference.GetAutoPilot.color = uiHighlight.GetSecondaryHighlight;
            else resetAutopilot();
        }

        void resetAutopilot(){
            uiRefference.GetAutoPilot.color = uiHighlight.GetStandard;
        }

        public void SetDriveSelectorHandle(PowerDirection powerDir){
            
            switch(powerDir){
                case PowerDirection.Forward: resetDriveSelectorHandle(); break;
                case PowerDirection.Backward: uiRefference.GetDriveSelectorHandleRect.anchoredPosition = new Vector2(uiRefference.GetDriveSelectorHandleRect.anchoredPosition.x,
                                                                                                                     uiRefference.GetDriveSelectorHandle_Bot); break;
            }
        }
        void resetDriveSelectorHandle(){
            uiRefference.GetDriveSelectorHandleRect.anchoredPosition = new Vector2(uiRefference.GetDriveSelectorHandleRect.anchoredPosition.x,
                                                                                   uiRefference.GetDriveSelectorHandle_Top);
        }
        public void Reset()
        {
            resetIndicators();
            resetHeadlight();
            resetAutopilot();
            resetDriveSelectorHandle();
        }
    }
}