using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelPainter.Rendering;

namespace VoxelPainter.GridManagement
{
    [Serializable]
    public struct ColorBreakpoint
    {
        public Color Color;
        public float Height;

        public ColorBreakpoint(Color color, float height)
        {
            Color = color;
            Height = height;
        }
    }
    
    [ExecuteAlways]
    public class TerrainRenderingConfigurator : MonoBehaviour
    {
        private static readonly int ColorMap = Shader.PropertyToID("_ColorMap");
        private static readonly int BaseHeight = Shader.PropertyToID("_BaseHeight");
        private static readonly int HeightScale = Shader.PropertyToID("_HeightScale");

        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private DrawingVisualizer _drawingVisualizer;

        [Range(5, 1000)] [SerializeField] private int _resolution;
        [SerializeField] private Gradient _gradient;

        [SerializeField] private Texture2D  _texture2D;
        
        private Material _sharedMaterial;
        private Color[] _colors;

        private void OnEnable()
        {
            Setup();
            
            GenerateTerrainTexture();
        }

        private void Setup()
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
            
            if (_sharedMaterial == null)
            {
                _sharedMaterial = _meshRenderer.sharedMaterial;
            }
            
            if (_drawingVisualizer == null)
            {
                _drawingVisualizer = GetComponent<DrawingVisualizer>();
            }
            
            _drawingVisualizer.MeshGenerated -= GenerateTerrainTexture;
            _drawingVisualizer.MeshGenerated += GenerateTerrainTexture;
        }
        
        private void OnDisable()
        {
            _sharedMaterial = null;
            _drawingVisualizer.MeshGenerated -= GenerateTerrainTexture;
        }
        
        private void OnValidate()
        {
            Setup();

            GenerateTerrainTexture();
        }
        
        private void GenerateTerrainTexture()
        {
            if (_colors == null || _colors.Length != _resolution)
            {
                _colors = new Color[_resolution];
            }
            
            _texture2D = new Texture2D(_resolution, 1, TextureFormat.RGBA32, false);
            
            for (int i = 0; i < _resolution; i++)
            {
                float t = 1f - i / (float) _resolution;
                _colors[i] = _gradient.Evaluate(t);
            }
            
            _texture2D.SetPixels(_colors);
            _texture2D.Apply();
            
            _sharedMaterial.SetTexture(ColorMap, _texture2D);
            _sharedMaterial.SetFloat(BaseHeight, _drawingVisualizer.transform.position.y);
            _sharedMaterial.SetFloat(HeightScale, _drawingVisualizer.VertexAmountY);
        }
    }
}