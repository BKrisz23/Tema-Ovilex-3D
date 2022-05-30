using System;
using UniStorm;
using UnityEngine;
public enum DayTime { Day, Night }

public class WeatherController : MonoBehaviour {
    [Header("Refferences")]
    [SerializeField] UniStormSystem uniStormSystem;

    [Header("WeatherProfiles")]
    [SerializeField] WeatherType rainWeather;
    [SerializeField] WeatherType partlyCloudyWeather;

    [Header("StreetLamps")]
    [SerializeField] MeshRenderer[] streetLampMashes;

    DayTime dayTime = DayTime.Day;
    public Action<DayTime> OnDayTimeChange;
    ILightmapController lightmapController;

    void Start() {
        lightmapController = new LightmapController("Day","Night", "Static");
    }

    public void SetRainWeather(){
        UniStormManager.Instance.ChangeWeatherInstantly(rainWeather);
    }
    public void SetPartlyCloudyWeather(){
        UniStormManager.Instance.ChangeWeatherInstantly(partlyCloudyWeather);
    }
     public void SetDayTime(){
        if(dayTime == DayTime.Day) return;

        uniStormSystem.m_TimeFloat = .5f;
        dayTime = DayTime.Day;
        lightmapController.SetDayTime();

        for (int i = 0; i < streetLampMashes.Length; i++)
        {
            streetLampMashes[i].enabled = false;
        }

        OnDayTimeChange?.Invoke(dayTime);
    }

    public void SetNightTime(){
        if(dayTime == DayTime.Night) return;

        lightmapController.SetNightTime();
        dayTime = DayTime.Night;
        uniStormSystem.m_TimeFloat = .2f;
        
        for (int i = 0; i < streetLampMashes.Length; i++)
        {
            streetLampMashes[i].enabled = true;
        }

        OnDayTimeChange?.Invoke(dayTime);
    }
}