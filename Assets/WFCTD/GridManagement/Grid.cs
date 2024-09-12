using System;
using System.Collections.Generic;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [Serializable]
    public struct GridPoint
    {
        public Vector3 position;
        public float value;
    }
    
    [Serializable]
    public class Grid
    {
        
        private GridProperties _gridProperties;
        private Func<Vector3, float> _noiseFunction;
        
        public GridPoint[] GridPoints { get; private set; }

        public Grid(GridProperties gridProperties, Func<Vector3, float> noiseFunction)
        {
            _gridProperties = gridProperties;
            _noiseFunction = noiseFunction;
            RecalculateGrid();
        }
        
        public void UpdateGridProperties(GridProperties gridProperties, Func<Vector3, float> noiseFunction)
        {
            _gridProperties = gridProperties;
            _noiseFunction = noiseFunction;
            RecalculateGrid();
        }
        
        private void RecalculateGrid()
        {
            if (_gridProperties.Density <= 0)
            {
                return;
            }

            float distanceBetweenVertices = 1 / _gridProperties.Density;

            int xSize = Mathf.CeilToInt(_gridProperties.Scale.x / distanceBetweenVertices);
            int ySize = Mathf.CeilToInt(_gridProperties.Scale.y / distanceBetweenVertices);
            int zSize = Mathf.CeilToInt(_gridProperties.Scale.z / distanceBetweenVertices);
            
            GridPoints = new GridPoint[(xSize + 1) * (ySize + 1) * (zSize + 1)];
            
            Vector3 halfSize = _gridProperties.Scale / 2;
            for (int i = 0, y = 0; y <= ySize; y++)
            {
                for (int x = 0; x <= xSize; x++)
                {
                    for (int z = 0; z <= zSize; z++, i++)
                    {
                        Vector3 offset = new (x * distanceBetweenVertices, y * distanceBetweenVertices, z * distanceBetweenVertices);
                        
                        Vector3 vertex = offset + _gridProperties.Origin - halfSize;
                        vertex.x *= _gridProperties.Frequency;
                        vertex.y *= _gridProperties.Frequency;
                        vertex.z *= _gridProperties.Frequency;
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