using UnityEngine;

public interface ILightmapController
{
    void SetDayTime();
    void SetNightTime();
}

public class LightmapController : ILightmapController
{
    public LightmapController(string dayName, string nightName, string tag)
    {
        dayLightmap = Resources.LoadAll<Texture2D>(dayName);
        nightLightmap = Resources.LoadAll<Texture2D>(nightName);

        Transform staticObjects = GameObject.FindGameObjectWithTag(tag).transform;
        staticMashes = staticObjects.GetComponentsInChildren<MeshRenderer>();
    }
    Texture2D[] dayLightmap;
    Texture2D[] nightLightmap;

    MeshRenderer[] staticMashes;

    public void SetNightTime()
    {
        setUpLightMapData(nightLightmap);
    }
    public void SetDayTime()
    {
        setUpLightMapData(dayLightmap);
    }

    void setUpLightMapData(Texture2D[] lightMap)
    {
        LightmapData[] lightmapData = new LightmapData[lightMap.Length];

        for (int i = 0; i < lightmapData.Length; i++)
        {
            lightmapData[i] = new LightmapData();
            lightmapData[i].lightmapColor = lightMap[i];
            lightmapData[i].lightmapDir = lightMap[i];
        }

        LightmapSettings.lightmaps = lightmapData;
    }
}