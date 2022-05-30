using UnityEngine;

/// Debug script
// [ExecuteInEditMode]
public class WorldBuilder : MonoBehaviour {
    [Header("Road")]
    [SerializeField] GameObject p_road;
    [SerializeField] Transform roadParent;
    [SerializeField] float roadZOffset;
    [SerializeField] float roadXOffset;
    [SerializeField] int roadCount = 20;
    [SerializeField] CombineMesh roadCombine;

    [Header("Trotuar")]
    [SerializeField] GameObject p_trotuar;
    [SerializeField] Transform tortuarParent;
    [SerializeField] float trotuarZOffset;
    [SerializeField] int trotuarCount = 20;
    [SerializeField] CombineMesh trotuarCombine;

    [Header("Pamant")]
    [SerializeField] GameObject p_pamant;
    [SerializeField] Transform pamantParent;
    [SerializeField] float pamantZOffset;
    [SerializeField] int pamantCount = 20;
    [SerializeField] CombineMesh pamantCombine;

    [Header("StreetLamp")]
    [SerializeField] GameObject p_streetLamps;
    [SerializeField] Transform streetLampParent;
    [SerializeField] float streetLampZOffset;
    [SerializeField] int streetLampCount = 20;
    [SerializeField] CombineMesh streetLampCombine;

    [Header("StreetLamp_Light")]
    [SerializeField] GameObject p_streetLampsLight;
    [SerializeField] Transform streetLampLightParent;
    [SerializeField] CombineMesh streetLampLightCombine;

    [Header("StreetLamp_StarLight")]
    [SerializeField] GameObject p_streetLampsStarLight;
    [SerializeField] Transform streetLampStarLightParent;
    [SerializeField] CombineMesh streetLampStarLightCombine;

    [Header("StreetLamp_GroundLight")]
    [SerializeField] GameObject p_streetLampsGroundLight;
    [SerializeField] Transform streetLampGroundLightParent;
    [SerializeField] CombineMesh streetLampGroundLightCombine;

    [Header("Shared Building Data")]
    [SerializeField] int buildingCountPerSide;
    [SerializeField] float buildingOffsetZ;
    [SerializeField] float buildingOffsetX;
    [SerializeField] float buildingDeltaOffsetZ;

    [Header("Building 1")]
    [SerializeField] GameObject p_building_1;
    [SerializeField] Transform building_1_parent;
    [SerializeField] CombineMesh building_1_combine;

    [Header("Building 2")]
    [SerializeField] GameObject p_building_2;
    [SerializeField] Transform building_2_parent;
    [SerializeField] CombineMesh building_2_combine;

    [Header("Building 3")]
    [SerializeField] GameObject p_building_3;
    [SerializeField] Transform building_3_parent;
    [SerializeField] CombineMesh building_3_combine;
    
    [Header("Building 4")]
    [SerializeField] GameObject p_building_4;
    [SerializeField] Transform building_4_parent;
    [SerializeField] CombineMesh building_4_combine;

    [Header("Building 5")]
    [SerializeField] GameObject p_building_5;
    [SerializeField] Transform building_5_parent;
    [SerializeField] CombineMesh building_5_combine;
    [Header("Building 6")]
    [SerializeField] GameObject p_building_6;
    [SerializeField] Transform building_6_parent;
    [SerializeField] CombineMesh building_6_combine;
    [Header("Building 7")]
    [SerializeField] GameObject p_building_7;
    [SerializeField] Transform building_7_parent;
    [SerializeField] CombineMesh building_7_combine;


    void Start() {
        Vector3 position = Vector3.zero;
        position.x = roadXOffset;
        Quaternion rotation = Quaternion.Euler(0f,90f,0f);
        for (int i = 0; i < roadCount; i++){
            Instantiate(p_road,position,rotation,roadParent);
            position.z += roadZOffset;
        }
        // roadCombine.CombineMeshes();

        position.z = 0;
        position.x = 0;
        for (int i = 0; i < trotuarCount; i++)
        {
            Instantiate(p_trotuar,position,Quaternion.identity,tortuarParent);
            position.z += trotuarZOffset;
        }
        // trotuarCombine.CombineMeshes();

        position.z = 0;
        for (int i = 0; i < pamantCount; i++)
        {
            Instantiate(p_pamant,position,Quaternion.identity,pamantParent);
            position.z += pamantZOffset;
        }
        // pamantCombine.CombineMeshes();


        position.z = 0;
        for (int i = 0; i < streetLampCount; i++)
        {
            Instantiate(p_streetLamps,position,Quaternion.identity,streetLampParent);
            position.z += streetLampZOffset;
        }
        // streetLampCombine.CombineMeshes();

        position.z = 0;
        for (int i = 0; i < streetLampCount; i++)
        {
            Instantiate(p_streetLampsLight,position,Quaternion.identity,streetLampLightParent);
            position.z += streetLampZOffset;
        }
        // streetLampLightCombine.CombineMeshes();

        position.z = 0;
        for (int i = 0; i < streetLampCount; i++)
        {
            Instantiate(p_streetLampsStarLight,position,Quaternion.identity,streetLampStarLightParent);
            position.z += streetLampZOffset;
        }
        // streetLampStarLightCombine.CombineMeshes();

        position.z = 0;
        for (int i = 0; i < streetLampCount; i++)
        {
            Instantiate(p_streetLampsGroundLight,position,Quaternion.identity,streetLampGroundLightParent);
            position.z += streetLampZOffset;
        }
        // streetLampGroundLightCombine.CombineMeshes();

        
        int buildingIndex = 0;

        position.z = 0;
        position.x = buildingOffsetX;

        for (int i = 0; i < buildingCountPerSide; i++)
        {
            buildingIndex = Random.Range(0,7);
            rotation = Quaternion.Euler(0f,90f,0f);
            switch(buildingIndex){
                case 0: 
                rotation = Quaternion.Euler(0f,180f,0f);
                Instantiate(p_building_1,position,rotation,building_1_parent); break;
                case 1: Instantiate(p_building_2,position,rotation,building_2_parent); break;
                case 2: Instantiate(p_building_3,position,rotation,building_3_parent); break;
                case 3: Instantiate(p_building_4,position,rotation,building_4_parent); break;
                case 4: Instantiate(p_building_5,position,rotation,building_5_parent); break;
                case 5: Instantiate(p_building_6,position,rotation,building_6_parent); break;
                case 6: Instantiate(p_building_7,position,rotation,building_7_parent); break;

            }
            
            position.z += buildingOffsetZ + Random.Range(-buildingDeltaOffsetZ,buildingDeltaOffsetZ);
        }

        position.z = 5f;
        position.x = -buildingOffsetX;

        for (int i = 0; i < buildingCountPerSide; i++)
        {
            buildingIndex = Random.Range(0,7);
            rotation = Quaternion.Euler(0f,-90f,0f);
            switch(buildingIndex){
                case 0: 
                rotation = Quaternion.Euler(0f,0f,0f);
                Instantiate(p_building_1,position,rotation,building_1_parent); break;
                case 1: Instantiate(p_building_2,position,rotation,building_2_parent); break;
                case 2: Instantiate(p_building_3,position,rotation,building_3_parent); break;
                case 3: Instantiate(p_building_4,position,rotation,building_4_parent); break;
                case 4: Instantiate(p_building_5,position,rotation,building_5_parent); break;
                case 5: Instantiate(p_building_6,position,rotation,building_6_parent); break;
                case 6: Instantiate(p_building_7,position,rotation,building_7_parent); break;

            }
            
            position.z += buildingOffsetZ + Random.Range(-buildingDeltaOffsetZ,buildingDeltaOffsetZ);
        }


        // building_1_combine.CombineMeshes();
        // building_2_combine.CombineMeshes();
        // building_3_combine.CombineMeshes();
        // building_4_combine.CombineMeshes();
        // building_5_combine.CombineMeshes();
        // building_6_combine.CombineMeshes();
        // building_7_combine.CombineMeshes();

    }
}