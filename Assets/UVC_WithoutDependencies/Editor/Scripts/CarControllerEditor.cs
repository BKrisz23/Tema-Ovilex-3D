using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PG
{
    [CustomEditor (typeof (CarController))]
    public class CarControllerEditor :Editor
    { 
        public override void OnInspectorGUI ()
        {

            PlayerPrefs.DeleteKey ("DontShowRewievMessage");
            if (!PlayerPrefs.HasKey ("DontShowRewievMessage"))
            {
                EditorGUILayout.HelpBox ("\nThank you for purchasing the asset.\n\nIf you have any questions, I will be happy to answer them, the contacts are in the documentation.\n\nIf you like the asset, you can write a review in the asset store, this will greatly help in the promotion and development of the asset.\n", MessageType.Info);
                EditorGUILayout.BeginHorizontal ();
                if (GUILayout.Button ("Write a review"))
                {
                    Application.OpenURL ("http://u3d.as/1ZdE");
                }
                if (GUILayout.Button ("Don't show this message"))
                {
                    PlayerPrefs.SetInt ("DontShowRewievMessage", 1);
                }
                EditorGUILayout.EndHorizontal ();
                EditorGUILayout.Space (10);
            }
            base.OnInspectorGUI ();
        }
    }
}
