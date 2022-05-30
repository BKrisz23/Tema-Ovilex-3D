using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PG
{
    [System.Serializable]
    public struct SceneField
    {
        public Object SceneAsset;
        public string SceneName;

        public static implicit operator string (SceneField s) => s.SceneName;
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer (typeof (SceneField))]
    public class SceneFieldPropertyDrawer :PropertyDrawer
    {
        public override void OnGUI (Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty (rect, GUIContent.none, property);
            SerializedProperty sceneAsset = property.FindPropertyRelative("SceneAsset");
            SerializedProperty sceneName = property.FindPropertyRelative("SceneName");
            rect = EditorGUI.PrefixLabel (rect, GUIUtility.GetControlID (FocusType.Passive), label);
            if (sceneAsset != null)
            {
                sceneAsset.objectReferenceValue = EditorGUI.ObjectField (rect, sceneAsset.objectReferenceValue, typeof (SceneAsset), false);
                if (sceneAsset.objectReferenceValue != null)
                {
                    sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name;
                }
            }
            EditorGUI.EndProperty ();
        }
    }
#endif
}

