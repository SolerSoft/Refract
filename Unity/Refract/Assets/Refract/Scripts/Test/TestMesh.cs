using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Refract
{
    public class TestMesh : TestSplit
    {
        [SerializeField]
        private MeshFilter meshObject;

        protected override void Start()
        {
            // Pass to base first
            base.Start();

            Texture2D source = inputTex;

            // Get height map pixels
            float[,] heightMap = new float[source.width, source.height];
            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    // heightMap[x, y] = source.GetPixel(x, y).grayscale;
                    heightMap[x, y] = source.GetPixel(x, y).r;
                }
            }

            // Create mesh settings
            MeshSettings settings = new MeshSettings();
            settings.chunkSizeIndex = 8;

            // Generate mesh
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap, settings, 0);
            meshObject.mesh = meshData.CreateMesh();
        }
    }
}