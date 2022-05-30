using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Main game settings.
    /// </summary>

    [CreateAssetMenu (fileName = "EditorHelperSettings", menuName = "GameBalance/Settings/EditorHelperSettings")]
    public class EditorHelperSettings :ScriptableObject
    {
        static EditorHelperSettings _Settings;
        public static EditorHelperSettings GetSettings
        {
            get
            {
                if (_Settings == null)
                {
                    _Settings = Resources.Load<EditorHelperSettings> ("EditorHelperSettings");
                }
                return _Settings;
            }
        }

        #region Shards
        [Header("Shards")]
        public ParticleSystem MainFrontLightShards;
        public ParticleSystem TurnSignalShards;
        public ParticleSystem BrakeShards;
        public ParticleSystem ReverseShards;
        public ParticleSystem GlassShards;

        public ParticleSystem GetShardsForLight (CarLightType type)
        {
            switch (type)
            {
                case CarLightType.Main:
                return MainFrontLightShards;
                case CarLightType.TurnLeft:
                return TurnSignalShards;
                case CarLightType.TurnRight:
                return TurnSignalShards;
                case CarLightType.Brake:
                return BrakeShards;
                case CarLightType.Reverse:
                return ReverseShards;
                default:
                return MainFrontLightShards;
            }
        }
        #endregion Shards

        #region Create car settings
        [Header("Create vehicle settings")]
        public Layer VehicleLayer;
        public Layer WheelsLayer;

        [Header("Create car settings")]
        public CarSFX CarSFXPrefab;
        public CarVFX CarVFXPrefab;
        public Texture PivotTex;

        public VehiclePart BumperFront;
        public VehiclePart Hood;
        public VehiclePart WingFrontLeft;
        public VehiclePart WingFrontRight;
        public VehiclePart DoorFrontLeft;
        public VehiclePart DoorFrontRight;
        public VehiclePart DoorRearLeft;
        public VehiclePart DoorRearRight;
        public VehiclePart Trunk;
        public VehiclePart BumperRear;
        public VehiclePart Body;

        [Header ("Create bike settings")]
        public CarSFX BikeSFXPrefab;
        public CarVFX BikeVFXPrefab;
        public VehiclePart Bike_Body;
        public VehiclePart Bike_Handlebar;
        public VehiclePart Bike_ForkFront;
        public VehiclePart Bike_ForkRear;

        [Header ("Glass and lights settings")]
        public GlassDO GlassDO;
        public LightObject LightObject;
        public Light LightPintSource;
        [Tooltip("Left light")]
        public Light LightSpotSource;
        public Color RedLightColor;
        public Color YelowLightColor;

        [System.Serializable]
        public struct VehiclePart
        {
            public string PartCaption;
            public string PartName;
            public string PartTooltip;
            public Texture PartButtonTexture;
            public GameObject PartPrefab;
        }

        #endregion //Create car settings
    }
}
