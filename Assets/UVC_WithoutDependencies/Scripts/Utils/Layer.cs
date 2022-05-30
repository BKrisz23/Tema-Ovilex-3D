using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PG
{
    /// <summary>
    /// To select a layer in the inspector.
    /// </summary>
    [System.Serializable]
    public struct Layer
    {
#pragma warning disable 0649

        [SerializeField] int value;

#pragma warning restore 0649

        public static implicit operator int (Layer layer)
        {
            return layer.value;
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer (typeof (Layer))]

    public class CustomLayerEditor :PropertyDrawer
    {

        static string[] displayedOptions = new string[32];
        static int[] optionValues = new int[32];

        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {

            Rect contentPosition = EditorGUI.PrefixLabel (position, new GUIContent (label));
            var value = property.FindPropertyRelative ("value");

            List<string> layers = new List<string> ();
            int selectedValue = 0;
            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName (i);
                if (!string.IsNullOrEmpty (layerName))
                {
                    layers.Add (layerName);
                }
            }

            if (displayedOptions.Length != layers.Count)
            {
                displayedOptions = new string[layers.Count];
                optionValues = new int[layers.Count];
            }

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = LayerMask.NameToLayer (layers[i]);
                displayedOptions[i] = string.Format ("{0}: {1}", layer, layers[i]);
                optionValues[i] = layer;
                if (value.intValue == layer)
                {
                    selectedValue = layer;
                }
            }

            EditorGUIUtility.labelWidth = 14f;

            EditorGUI.BeginProperty (contentPosition, label, property);
            {
                EditorGUI.BeginChangeCheck ();
                var menu = EditorGUI.IntPopup (contentPosition, selectedValue, displayedOptions, optionValues);
                if (EditorGUI.EndChangeCheck ())
                {
                    value.intValue = System.Convert.ToInt16 (menu);
                }

            }
            EditorGUI.EndProperty ();
        }
    }

#endif
}
