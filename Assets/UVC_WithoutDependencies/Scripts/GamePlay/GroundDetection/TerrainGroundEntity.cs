using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PG
{
    /// <summary>
    /// Terrain, with GroundConfigs, can be assigned a separate config for each texture.
    /// </summary>
    [RequireComponent (typeof (Terrain))]
    public class TerrainGroundEntity :IGroundEntity
    {
#pragma warning disable 0649

        [SerializeField] GroundConfig DefaultConfig;                                                    //Default config if the required config is not found.
        [SerializeField] List<TerrainGroundConfig> GroundConfigs = new List<TerrainGroundConfig>();     //List of available configs.

        [SerializeField,HideInInspector] byte [] DominateTextures;                                      //A one-dimensional array with the index of the dominating textures at all positions of the terrain.
        [SerializeField,HideInInspector] float StepX;
        [SerializeField,HideInInspector] float StepZ;

#pragma warning restore 0649

        Dictionary<byte, GroundConfig> GroundConfigsDictionary = new Dictionary<byte, GroundConfig>();  //A dictionary created in Awake to find the desired texture.

        Terrain Terrain;

        TerrainData TerrainData;
        Vector3 TerrainPos;

        /// <summary>
        /// Search for the required config at a specific point.
        /// </summary>
        public override GroundConfig GetGroundConfig (Vector3 position)
        {
            //mapX * TerrainData.alphamapWidth + mapZ - the index of the dominating texture in the one-dimensional array.
            int mapX = (int)((position.x - TerrainPos.x) / StepX);
            int mapZ = (int)((position.z - TerrainPos.z) / StepZ);

            var textureIndex = DominateTextures[mapX * TerrainData.alphamapWidth + mapZ];

            GroundConfig result = null;

            if (!GroundConfigsDictionary.TryGetValue (textureIndex, out result))
            {
                return DefaultConfig;
            }

            return result;
        }

        void Awake ()
        {
            //Dictionary creation.
            foreach (var config in GroundConfigs)
            {
                if (GroundConfigsDictionary.ContainsKey (config.TextureIndex))
                {
                    Debug.LogErrorFormat ("[TerrainGroundDetection.GroundConfigs] The value [{0}] is already in the dictionary", config.TextureIndex);
                }
                else
                {
                    GroundConfigsDictionary.Add (config.TextureIndex, config.GroundConfig);
                }
            }

            Terrain = GetComponent<Terrain> ();
            TerrainData = Terrain.terrainData;
            TerrainPos = Terrain.transform.position;
        }

        /// <summary>
        /// CacheDominantTextures is a laborious process, so it is displayed in the context menu, 
        /// after finishing editing the terrain, you need to call this method through the context menu.
        /// </summary>
        [ContextMenu ("Cache dominant textures")]
        public void CacheDominantTextures ()
        {
            var terrain = GetComponent<Terrain> ();
            var terrainData = terrain.terrainData;
            var terrainPos = terrain.transform.position;

            StepX = terrainData.size.x / terrainData.alphamapWidth;
            StepZ = terrainData.size.z / terrainData.alphamapHeight;

            var width = terrainData.alphamapWidth;
            var height = terrainData.alphamapWidth;

            DominateTextures = new byte[terrainData.alphamapWidth * terrainData.alphamapHeight];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    float[,,] splatmapData = terrainData.GetAlphamaps(x, z, 1, 1 );

                    float[] cellMix = new float[ splatmapData.GetUpperBound(2) + 1 ];

                    for (int n = 0; n < cellMix.Length; n++)
                    {
                        cellMix[n] = splatmapData[0, 0, n];
                    }

                    float maxMix = 0;
                    int maxIndex = 0;

                    for (int n = 0; n < cellMix.Length; n++)
                    {
                        if (cellMix[n] > maxMix)
                        {
                            maxIndex = n;
                            maxMix = cellMix[n];
                        }
                    }

                    DominateTextures[x * terrainData.alphamapWidth + z] = (byte)maxIndex;
                }
            }

#if UNITY_EDITOR

            UnityEditor.EditorUtility.SetDirty (gameObject);

#endif

            Debug.Log ("Dominant textures cached");
        }

        /// <summary>
        /// A GroundConfig wrapper with a texture index.
        /// </summary>
        [System.Serializable]
        public class TerrainGroundConfig
        {
            [SerializeField] string Caption;    //Field for easy editing
            public byte TextureIndex;           //Texture index (Index of the layer in terrain)
            public GroundConfig GroundConfig;
        }
    }
}
