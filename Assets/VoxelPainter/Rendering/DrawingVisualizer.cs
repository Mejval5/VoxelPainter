using System;
using Foxworks.Persistence;
using Foxworks.Voxels;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelPainter.GridManagement;
using VoxelPainter.VoxelVisualization;

namespace VoxelPainter.Rendering
{
    [ExecuteAlways]
    public class DrawingVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private bool _allowRegeneration = false;
        [field: Range(0.1f, 10f)] [field: SerializeField] public float InitScale { get; set; } = 2f;
        [field: Range(0.1f, 10f)] [field: SerializeField] public float InitFrequency { get; set; } = 2f;

        private NativeArray<float> _verticesValuesNative;
        private Vector3Int _cachedVertexAmount;
        
        public NativeArray<float> VerticesValuesNative => _verticesValuesNative;
        
        public bool Generating { get; private set; }

        public override void GenerateMesh()
        {
            try
            {
                Profiler.BeginSample("GenerateMesh");
                Generating = true;

                bool didChange = false;
                if (_verticesValuesNative.IsCreated == false
                    || _cachedVertexAmount.x != VertexAmountX
                    || _cachedVertexAmount.y != VertexAmountY
                    || _cachedVertexAmount.z != VertexAmountZ)
                {
                    MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, VertexAmount);
                    _cachedVertexAmount = new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ);
                    didChange = true;
                }

                if (_allowRegeneration || didChange)
                {
                    InitDrawing();
                }

                base.GenerateMesh();
            }
            catch (Exception exception)
            {
                Debug.LogError("Error generating mesh: " + exception);
            }
            finally
            {
                Generating = false;
                Profiler.EndSample();
            }
        }

        public void Save()
        {
            SaveManager.Save("drawing", _verticesValuesNative.ToArray());
            SaveManager.Save("drawingParameters", _cachedVertexAmount);
        }

        public void Load()
        {
            MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ));
            
            float[] verticesValues = SaveManager.Load<float[]>("drawing");
            if (verticesValues == null)
            {
                return;
            }
            
            _verticesValuesNative.CopyFrom(verticesValues);
            
            _cachedVertexAmount = SaveManager.Load<Vector3Int>("drawingParameters");
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();

            Load();

            GenerateMesh();
        }

        private void InitDrawing()
        {
            Profiler.BeginSample("InitDrawing");
            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        float sineOffset = InitScale * Mathf.PerlinNoise(z * InitFrequency / 51.164658416f, x * InitFrequency / 87.1777416f);
                        float value = y < VertexAmountY / 2f + sineOffset ? 1 : 0;
                        WriteIntoGrid(pos, value);
                    }
                }
            }
            Profiler.EndSample();
        }

        public void WriteIntoGrid(Vector3Int pos, float value)
        {
            int index = MarchingCubeUtils.ConvertPositionToIndex(pos, VertexAmount);
            WriteIntoGrid(index, value);
        }

        public void WriteIntoGrid(int index, float value)
        {
            _verticesValuesNative[index] = value;
        }

        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            // Do nothing
        }
    }
}