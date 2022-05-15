//______________________________________________//
//___________Realistic Engine Sounds____________//
//______________________________________________//
//_______Copyright © 2019 Yugel Mobile__________//
//______________________________________________//
//_________ http://mobile.yugel.net/ ___________//
//______________________________________________//
//________ http://fb.com/yugelmobile/ __________//
//______________________________________________//
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarSimulator : MonoBehaviour { // this script was only made for slider demo scene for demonstration purposes only
    public bool gasPedalPressing = false;
    public float maxRPM = 7000;
    public float idle = 900;
    public float rpm = 0;
    public float accelerationSpeed = 1000f;
    public float decelerationSpeed = 1200f;
	public Slider accelSlider;

    private void Start()
    {
        rpm = idle;
    }
    void Update ()
    {
        if (gasPedalPressing)
        {
            if (rpm <= maxRPM)
				rpm = Mathf.Lerp(rpm, rpm + accelerationSpeed * accelSlider.value, Time.deltaTime);
        }
        else
        {
            if (rpm > idle)
                rpm = Mathf.Lerp(rpm, rpm - decelerationSpeed * accelSlider.value, Time.deltaTime);
        }
	}
    public void onPointerDownRaceButton()
    {
        gasPedalPressing = true;
    }
    public void onPointerUpRaceButton()
    {
        gasPedalPressing = false;
    }
}
