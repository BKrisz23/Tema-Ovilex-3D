//______________________________________________//
//___________Realistic Engine Sounds____________//
//______________________________________________//
//_______Copyright © 2020 Yugel Mobile__________//
//______________________________________________//
//_________ http://mobile.yugel.net/ ___________//
//______________________________________________//
//________ http://fb.com/yugelmobile/ __________//
//______________________________________________//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DemoController : MonoBehaviour 
{
    /* 
       "Slider" demo scene's script is only used for demonstration purposes and for testing, do not use it in live products, because it's not designed for that.
       Instead use the included .unitypackages for the supported vehicle physics controller assets. This "DemoController.cs" scipt is designed to work only in "Slider" demo scenes to connect RES prefabs with UI buttons and with the simulated vehicle engine values, it will not work in other scenes.
       For your own scenes import the right *.unitypackage from: "./RealisticEngineSound/Assets_For_Vehicle_Controllers" folder for your car controller or ask me in email to add support for your custom or not yet supported car controller.
    */
    [HideInInspector]
    public RealisticEngineSound[] res;
    [HideInInspector]
    public RealisticEngineSound_mobile[] resmob;
    public GameObject gasPedalButton; // UI button
    public Slider rpmSlider; // UI slider to set RPM
    public Slider pitchSlider; // UI sliter to set maximum pitch
    public Text pitchText; // UI text to show pitch multiplier value
    public Text rpmText; // UI text to show current RPM
    public Toggle isReversing; // UI checkbox for is reversing
    public Toggle gasPedalPressing;
    public GameObject accelerationSpeed; // UI slider for acceleration speed
    public bool simulated = true; // is rpm simulated with gaspedal button or with rpm slider by hand
    private bool isMobileDemoScene = false; // for mobile RES slider demo scene
    CarSimulator carSimulator;
    private void Start()
    {
        // check which slider demo scene is opened
        if (SceneManager.GetActiveScene().name == "_slider_demo_scene") // regular slider demo scene
            isMobileDemoScene = false;
        if (SceneManager.GetActiveScene().name == "mobile_slider_demo_scene") // mobile slider demo scene
            isMobileDemoScene = true;
        carSimulator = gasPedalButton.GetComponent<CarSimulator>();

        if (isMobileDemoScene)
        {
            resmob = GetComponentsInChildren<RealisticEngineSound_mobile>();
            for (int i = 0; i < resmob.Length; i++)
            {
                resmob[i].carMaxSpeed = 7000;
                // turn off all interior prefabs
                if (i % 2 == 1)
                    resmob[i].gameObject.SetActive(false);
            }
        }
        else
        {
            res = GetComponentsInChildren<RealisticEngineSound>();
            for (int i = 0; i < res.Length; i++)
            {
                res[i].carMaxSpeed = 7000;
                // turn off all interior prefabs
                if (i % 2 == 1)
                    res[i].gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        rpmText.text = "Engine RPM: " + (int)rpmSlider.value; // show current RPM - this creates garbage
        pitchText.text = "" + pitchSlider.value; // set pitch multiplier value for ui text
        // rpm values
        if (isMobileDemoScene)
        {
            for (int i = 0; i < resmob.Length; i++)
            {
                resmob[i].pitchMultiplier = pitchSlider.value;
                if (simulated)
                {
                    resmob[i].engineCurrentRPM = carSimulator.rpm;
                    rpmSlider.value = carSimulator.rpm; // set ui sliders value to rpm
                }
                else
                {
                    resmob[i].engineCurrentRPM = rpmSlider.value;
                    carSimulator.rpm = rpmSlider.value;
                }
                resmob[i].carCurrentSpeed = rpmSlider.value/127; // for reverse gear fx
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                res[i].pitchMultiplier = pitchSlider.value;
                if (simulated)
                {
                    res[i].engineCurrentRPM = carSimulator.rpm;
                    rpmSlider.value = carSimulator.rpm; // set ui sliders value to rpm
                }
                else
                {
                    res[i].engineCurrentRPM = rpmSlider.value;
                    carSimulator.rpm = rpmSlider.value;
                }
                res[i].carCurrentSpeed = rpmSlider.value/127; // simulate car's current speed for reverse gear sound fx
            }
        }
        if (simulated) // update is gas pedal pressing toggle
            if (gasPedalPressing != null)
                gasPedalPressing.isOn = carSimulator.gasPedalPressing;
    }

    // enable/disable rev limiter
    public void UpdateRPM(Toggle togl)
    {
        if (isMobileDemoScene)
        {
            for (int i = 0; i < resmob.Length; i++)
            {
                resmob[i].useRPMLimit = togl.isOn;
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                res[i].useRPMLimit = togl.isOn;
            }
        }
    }
    // enable/disable reverse gear sound fx
    public void UpdateReverseGear(Toggle togl)
    {
        if (isMobileDemoScene)
        {
            for (int i = 0; i < resmob.Length; i++)
            {
                resmob[i].enableReverseGear = togl.isOn;
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                res[i].enableReverseGear = togl.isOn;
            }
        }
        // show/hide isReversing checkbox
        if (togl.isOn == false)
        {
            isReversing.isOn = false;
            isReversing.gameObject.SetActive(false);
        }
        else
        {
            if (isReversing.gameObject.activeSelf == false)
                isReversing.gameObject.SetActive(true);
        }
    }
    // is reversing
    public void IsReversing(Toggle togl)
    {
        if (isMobileDemoScene)
        {
            for (int i = 0; i < resmob.Length; i++)
            {
                resmob[i].isReversing = togl.isOn;
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                res[i].isReversing = togl.isOn;
            }
        }
    }
    // is simulated rpm
    public void IsSimulated(Dropdown drpDown)
    {
        if (drpDown.value == 0)
        {
            simulated = true;
            accelerationSpeed.SetActive(true);
            gasPedalButton.SetActive(true);
        }
        if (drpDown.value == 1)
        {
            simulated = false;
            accelerationSpeed.SetActive(false);
            gasPedalButton.SetActive(false);
            if (!isMobileDemoScene)
                gasPedalPressing.isOn = true;
        }
    }
    // change car sound buttons
    public void ChangeCarSound(int a) // a = exterior, a+1 = interior prefabs id numbers in allChildren[]
    {
        if (isMobileDemoScene)
        {
            for (int i = 0; i < resmob.Length; i++)
            {
                if (i != a && i != a + 1)
                    resmob[i].enabled = false;
                resmob[a].enabled = true;
                resmob[a+1].enabled = true;
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                if (i != a && i != a + 1)
                    res[i].enabled = false;
                res[a].enabled = true;
                res[a + 1].enabled = true;
            }
        }
    }
    // gas pedal checkbox
    public void UpdateGasPedal(Toggle togl)
    {
        if (isMobileDemoScene)
        {
            for (int i = 0; i < resmob.Length; i++)
            {
                resmob[i].gasPedalPressing = togl.isOn;
            }
        }
        else
        {
            for (int i = 0; i < res.Length; i++)
            {
                res[i].gasPedalPressing = togl.isOn;
            }
        }
    }
}
