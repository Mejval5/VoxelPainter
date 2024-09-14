using UnityEngine;

namespace WFCTD.GridManagement
{
    public class TrigVisualizer : MarchingCubeRendererBase
    {
        public override float GetGridValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            float x = (position.x + generationProperties.Origin.x) * generationProperties.Frequency / 1000f;
            float y = (position.y + generationProperties.Origin.y) * generationProperties.Frequency / 1000f;
            float z = (position.z + generationProperties.Origin.z) * generationProperties.Frequency / 1000f;
            
            return (Mathf.Sin(x) + Mathf.Cos(y) + Mathf.Cos(z) + 3) / 6f;
        }
    }
}