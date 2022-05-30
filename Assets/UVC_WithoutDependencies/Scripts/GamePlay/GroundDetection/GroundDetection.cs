using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Basic controller for detecting the ground under the wheel.
    /// </summary>
    public class GroundDetection :Singleton<GroundDetection>
    {
#pragma warning disable 0649

        [SerializeField] GroundConfig DefaultGroundConfig;      //Default config if no suitable GroundConfig is found under the wheel.

#pragma warning restore 0649

        public static GroundConfig GetDefaultGroundConfig
        {
            get
            {
                if (Instance == null)
                {
                    Instantiate (B.ResourcesSettings.DefaultGroundDetection);
                    Debug.Log ("GroundDetection has been created");
                }

                return Instance.DefaultGroundConfig;
            }
        }

        //Dictionary of configs, to remember the config so that you do not use GetComponent <IGroundEntity> all the time.
        Dictionary<GameObject, IGroundEntity> GroundsDictionary = new Dictionary<GameObject, IGroundEntity>();

        //Get IGroundEntity for GameObject.
        public static IGroundEntity GetGroundEntity (GameObject go)
        {
            if (Instance == null)
            {
                Debug.LogError ("Scene without GroundDetection");
                return null;
            }

            IGroundEntity result = null;
            if (!Instance.GroundsDictionary.TryGetValue (go, out result))
            {
                result = go.GetComponent<IGroundEntity> ();
                Instance.GroundsDictionary.Add (go, result);
            }

            return result;
        }
    }
}
