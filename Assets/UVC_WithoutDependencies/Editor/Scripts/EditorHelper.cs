using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PG
{
    public static class EditorHelper
    {
        [MenuItem ("GameObject/Create Other/UVC/AIPath")]
        public static void CreateAIPath ()
        {
            var aiPath = new GameObject("AIPath");
            aiPath.AddComponent<AIPath> ();
            Selection.activeObject = aiPath.gameObject;
        }

        [MenuItem ("GameObject/Create Other/UVC/AITrigger")]
        public static void CreateAITrigger ()
        {
            var aiTrigger = new GameObject("AITrigger");
            aiTrigger.AddComponent<AITrigger> ();
            var collider = aiTrigger.AddComponent<BoxCollider> ();
            collider.isTrigger = true;
            collider.size = Vector3.one * 10;
            Selection.activeObject = collider.gameObject;
        }

        [MenuItem ("GameObject/Create Other/UVC/AI Debug Spawner (Only for Editor)")]
        public static void CreateAiSpawner ()
        {
            var go = new GameObject("AISpawner");
            var aiSpawner = go.AddComponent<AIDebugSpawner> ();
            aiSpawner.AIPath = Transform.FindObjectOfType<AIPath> ();
            aiSpawner.AIControl = Transform.FindObjectOfType<PositioningAIControl> ();
            Selection.activeObject = go;
        }

        [MenuItem ("CONTEXT/GlassDO/Add glass shards")]
        public static ParticleSystem CreateGlassShards (MenuCommand command)
        {
            var glass = (GlassDO)command.context;
            return CreateGlassShards (glass);
        }
        public static ParticleSystem CreateGlassShards (GlassDO glass)
        {
            Vector3 pos = glass.transform.position + glass.LocalCenterPoint;
            Quaternion rot = glass.transform.rotation;
            if (glass.ShardsParticles != null)
            {
                pos = glass.ShardsParticles.transform.position;
                rot = glass.ShardsParticles.transform.rotation;
                GameObject.DestroyImmediate (glass.ShardsParticles.gameObject);
            }
            else
            {
                CarController parentCar = glass.GetComponentInParent<CarController>();
                if (parentCar)
                {
                    var glassPos = glass.transform.position;
                    var carPos = parentCar.transform.position;
                    glassPos.y = 0;
                    carPos.y = 0;

                    rot = Quaternion.LookRotation (glassPos - carPos, Vector3.up);
                }
            }

            var shardsRef = EditorHelperSettings.GetSettings.GlassShards;
            glass.ShardsParticles = GameObject.Instantiate (shardsRef, glass.transform.parent);
            glass.ShardsParticles.transform.position = pos;
            glass.ShardsParticles.transform.rotation = rot;
            glass.ShardsParticles.name = glass.name + "_Shards";
            glass.ShardsParticles.SetActive (false);
            glass.ShardsParticles.transform.SetSiblingIndex (glass.transform.GetSiblingIndex());
            UnityEditor.EditorUtility.SetDirty (glass);
            return glass.ShardsParticles;
        }

        [MenuItem ("CONTEXT/LightObject/Add light shards")]
        public static ParticleSystem CreateLightShards (MenuCommand command)
        {
            var light = (LightObject)command.context;
            return CreateLightShards (light);
        }
        public static ParticleSystem CreateLightShards (LightObject light)
        {
            Vector3 pos = light.transform.position + light.LocalCenterPoint;
            Quaternion rot = light.transform.rotation;
            if (light.ShardsParticles != null)
            {
                pos = light.ShardsParticles.transform.position;
                rot = light.ShardsParticles.transform.rotation;
                GameObject.DestroyImmediate (light.ShardsParticles.gameObject);
            }
            else
            {
                CarController parentCar = light.GetComponentInParent<CarController>();
                if (parentCar)
                {
                    var glassPos = light.transform.position;
                    var carPos = parentCar.transform.position;
                    glassPos.y = 0;
                    carPos.y = 0;

                    rot = Quaternion.LookRotation (glassPos - carPos, Vector3.up);
                }
            }

            var shardsRef = EditorHelperSettings.GetSettings.GetShardsForLight(light.CarLightType);
            light.ShardsParticles = GameObject.Instantiate (shardsRef, light.transform.parent);
            light.ShardsParticles.transform.position = pos;
            light.ShardsParticles.transform.rotation = rot;
            light.ShardsParticles.name = light.name + "_Shards";
            light.ShardsParticles.transform.SetSiblingIndex (light.transform.GetSiblingIndex ());
            light.ShardsParticles.SetActive (false);
            light.ShardsParticles.transform.SetSiblingIndex (light.transform.GetSiblingIndex ());
            UnityEditor.EditorUtility.SetDirty (light);
            return light.ShardsParticles;
        }
    }
}
