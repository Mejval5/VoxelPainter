using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPainter.GridManagement
{
    [Serializable]
    public struct GridPoint
    {
        public Vector3 position;
        public float value;
    }

    [Serializable]
    public class GridProperties : GenerationProperties
    {
        [field: SerializeField] public float Density { get; set; }
        [field: SerializeField] public Vector3 Scale { get; set; } = Vector3.one;
    }
    
    [Serializable]
    public class Grid
    {
        
        private GridProperties _generationProperties;
        private Func<Vector3, float> _noiseFunction;
        
        public GridPoint[] GridPoints { get; private set; }

        public Grid(GridProperties generationProperties, Func<Vector3, float> noiseFunction)
        {
            _generationProperties = generationProperties;
            _noiseFunction = noiseFunction;
            RecalculateGrid();
        }
        
        public void UpdateGridProperties(GridProperties generationProperties, Func<Vector3, float> noiseFunction)
        {
            _generationProperties = generationProperties;
            _noiseFunction = noiseFunction;
            RecalculateGrid();
        }
        
        private void RecalculateGrid()
        {
            if (_generationProperties.Density <= 0)
            {
                return;
            }

            float distanceBetweenVertices = 1 / _generationProperties.Density;

            int xSize = Mathf.CeilToInt(_generationProperties.Scale.x / distanceBetweenVertices);
            int ySize = Mathf.CeilToInt(_generationProperties.Scale.y / distanceBetweenVertices);
            int zSize = Mathf.CeilToInt(_generationProperties.Scale.z / distanceBetweenVertices);
            
            GridPoints = new GridPoint[(xSize + 1) * (ySize + 1) * (zSize + 1)];
            
            Vector3 halfSize = _generationProperties.Scale / 2;
            for (int i = 0, y = 0; y <= ySize; y++)
            {
                for (int x = 0; x <= xSize; x++)
                {
                    for (int z = 0; z <= zSize; z++, i++)
                    {
                        Vector3 offset = new (x * distanceBetweenVertices, y * distanceBetweenVertices, z * distanceBetweenVertices);
                        
                        Vector3 vertex = offset + _generationProperties.Origin - halfSize;
                        vertex.x *= _generationProperties.Frequency;
                        vertex.y *= _generationProperties.Frequency;
                        vertex.z *= _generationProperties.Frequency;
                        float value = _noiseFunction.Invoke(vertex);      
                        
                        GridPoints[i] = new GridPoint
                        {
                            position = vertex,
                            value = value
                        };
                    }
                }
            }
        }
    }
}