using Unity.Collections;
using UnityEngine;

namespace WFCTD.GridManagement
{
    public class TrigVisualizer : MarchingCubeRendererBase
    {
        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;

            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int position = MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                float x = (position.x + GenerationProperties.Origin.x) * GenerationProperties.Frequency / 1000f;
                float y = (position.y + GenerationProperties.Origin.y) * GenerationProperties.Frequency / 1000f;
                float z = (position.z + GenerationProperties.Origin.z) * GenerationProperties.Frequency / 1000f;
            
                verticesValues[i] = (Mathf.Sin(x) + Mathf.Cos(y) + Mathf.Cos(z) + 3) / 6f;
            }
        }
    }
}