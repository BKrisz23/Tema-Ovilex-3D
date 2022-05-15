//______________________________________________//
//___________Realistic Engine Sounds____________//
//______________________________________________//
//_______Copyright © 2018 Yugel Mobile__________//
//______________________________________________//
//_________ http://mobile.yugel.net/ ___________//
//______________________________________________//
//________ http://fb.com/yugelmobile/ __________//
//______________________________________________//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Audio;
using UnityEngine.Audio;

[CustomEditor(typeof(RealisticEngineSound))]
[CanEditMultipleObjects]
public class RealisticEngineSoundEditor : Editor {

    override public void OnInspectorGUI()
    {
        var res = target as RealisticEngineSound;
        DrawDefaultInspector();
        if(res.engineShakeSetting == RealisticEngineSound.EngineShake.Random)
        {
            res.shakeVolumeChange = EditorGUILayout.Slider("Shake Volume Change", res.shakeVolumeChange, 0.3f, 0.9f);
            res.randomChance = EditorGUILayout.Slider("Random Chance", res.randomChance, 0.1f, 0.9f);
            res.shakeLenghtSetting = (RealisticEngineSound.ShakeLenghtType) EditorGUILayout.EnumPopup("Shake Lenght Setting", res.shakeLenghtSetting);
            if(res.shakeLenghtSetting == RealisticEngineSound.ShakeLenghtType.Fix)
            {
                res.shakeLength = EditorGUILayout.Slider("Shake Length", res.shakeLength, 10, 100);
            }
        }
        if (res.engineShakeSetting == RealisticEngineSound.EngineShake.AllwaysOn)
        {
            res.shakeVolumeChange = EditorGUILayout.Slider("Shake Volume Change", res.shakeVolumeChange, 0.3f, 0.9f);
            res.shakeLenghtSetting = (RealisticEngineSound.ShakeLenghtType)EditorGUILayout.EnumPopup("Shake Lenght Setting", res.shakeLenghtSetting);
            if (res.shakeLenghtSetting == RealisticEngineSound.ShakeLenghtType.Fix)
            {
                res.shakeLength = EditorGUILayout.Slider("Shake Length", res.shakeLength, 10, 100);
            }
        }
    }
}
