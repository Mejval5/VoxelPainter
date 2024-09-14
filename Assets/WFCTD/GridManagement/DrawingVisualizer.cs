using System;
using UnityEngine;

namespace WFCTD.GridManagement
{
    public class DrawingVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private bool _allowRegeneration = false;
        [field: Range(0.1f, 10f)] [field: SerializeField] public float InitScale { get; set; } = 2f;
        
        private int[,,] _drawing;

        protected override void Setup()
        {
            bool didChange = false;
            if (_drawing == null 
                || _drawing.GetLength(0) != VertexAmountX 
                || _drawing.GetLength(1) != VertexAmountY 
                || _drawing.GetLength(2) != VertexAmountZ)
            {
                _drawing = new int[VertexAmountX, VertexAmountY, VertexAmountZ];
                didChange = true;
            }
            
            if (_allowRegeneration || didChange)
            {
                InitDrawing();
            }
            
            base.Setup();
        }

        private void InitDrawing()
        {
            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        float sineOffset = InitScale * Mathf.PerlinNoise(z * GenerationProperties.Frequency / 51.164658416f, x * GenerationProperties.Frequency / 87.1777416f);
                        _drawing[x, y, z] = y < (VertexAmountY / 2f + sineOffset) ? 1 : 0;
                    }
                }
            }
        }

        public override float GetGridValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            return _drawing[(int)position.x, (int)position.y, (int)position.z];
        }
    }
}