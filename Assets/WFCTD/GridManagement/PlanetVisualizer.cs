using Unity.Collections;
using UnityEngine;

namespace WFCTD.GridManagement
{
    public class PlanetVisualizer : MarchingCubeRendererBase
    {
        [Range(0f, 200f)]
        [SerializeField] private float _planetSurface;
        
        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;
            
            Vector3 middleOfPlanet = new (VertexAmountX / 2f, VertexAmountY / 2f, VertexAmountZ / 2f);
            
            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int pos = MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                float distance = Vector3.Distance(middleOfPlanet, pos);
                verticesValues[i] = distance < _planetSurface ? 1f : 0f;
            }
        }
    }
}