﻿using Foxworks.Noise;
using Foxworks.Voxels;
using Unity.Collections;
using UnityEngine;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    /// <summary>
    /// This class is used to visualize a simplex noise.
    /// </summary>
    [ExecuteAlways]
    public class SimplexNoiseVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private GenerationProperties _generationProperties;
        
        public override void GetVertexValues(NativeArray<int> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;

            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int position =MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                
                float x = (position.x) * _generationProperties.Frequency / 1000f + _generationProperties.Origin.x;
                float y = (position.y) * _generationProperties.Frequency / 1000f + _generationProperties.Origin.y;
                float z = (position.z) * _generationProperties.Frequency / 1000f + _generationProperties.Origin.z;

                verticesValues[i] = VoxelDataUtils.PackValueAndVertexColor(CustomNoiseSimplex(x, y, z));
            }
        }

        private static float CustomNoiseSimplex(float x, float y, float z)
        {
            return Mathf.Clamp01(Mathf.Pow(SimplexNoiseGenerator.Generate(x, y, z), 2));
        }
    }
}