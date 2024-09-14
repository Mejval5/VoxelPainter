using System.Collections.Generic;
using UnityEngine;

namespace WFCTD.GridManagement
{
    public class SimplexGrid
    {
        private GridProperties _generationProperties;

        public Grid Grid { get; private set; }
        
        public Mesh GridMesh { get; private set; }

        public SimplexGrid(GridProperties generationProperties, float threshold)
        {
            _generationProperties = generationProperties;
            RecalculateGrid(threshold);
        }
        
        public void UpdateGridProperties(GridProperties generationProperties, float threshold)
        {
            _generationProperties = generationProperties;
            RecalculateGrid(threshold);
        }
        
        private void RecalculateGrid(float threshold)
        {
            if (Grid == null)
            {
                Grid = new Grid(_generationProperties, SimplexNoise.Generate);
            }
            else
            {
                Grid.UpdateGridProperties(_generationProperties, SimplexNoise.Generate);
            }

            GridPoint[] gridPoints = Grid.GridPoints;
            List<Vector3> vertices = new ();
            
            for (int i = 0; i < gridPoints.Length; ++i)
            {
                GridPoint gridPoint = gridPoints[i];
                
                if (gridPoint.value < threshold)
                {
                    vertices.Add(gridPoint.position);
                }
            }

            if (GridMesh == null)
            {
                GridMesh = new Mesh
                {
                    vertices = vertices.ToArray()
                };
            }
            else
            {
                GridMesh.Clear();
                GridMesh.vertices = vertices.ToArray();
            }
        }
    }
}