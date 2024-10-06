using System;
using AYellowpaper.SerializedCollections;
using Foxworks.Utils;
using UnityEngine;

namespace VoxelPainter.Rendering.Materials
{
    public enum VoxelMaterial
    {
        Air = 0,
        Stone = 1,
        Dirt = 2,
        Grass = 3,
        Sand = 4,
        Water = 5,
        Snow = 6,
        Ice = 7,
        Wood = 8,
        Leaves = 9,
        Glass = 10,
        Lava = 11,
        Metal = 12,
        Diamond = 13,
        Fire = 14,
        Obsidian = 15,
        Coal = 16,
        Smoke = 17,
    }
    
    [ExecuteAlways]
    public class TerrainMaterialsConfigurator : MonoBehaviour
    {
        private static readonly int ColorMap = Shader.PropertyToID("_ColorMap");
        private static readonly int BaseHeight = Shader.PropertyToID("_BaseHeight");
        private static readonly int HeightScale = Shader.PropertyToID("_HeightScale");

        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private DrawingVisualizer _drawingVisualizer;

        [Range(5, 1000)] [SerializeField] private int _resolution;
        [SerializeField] private Gradient _gradient;

        [SerializeField] private Texture2D  _texture2D;
        [SerializeField] private SerializedDictionary<VoxelMaterial, Color> _materialColorMap;
        [SerializeField] private SerializedDictionary<float, VoxelMaterial> _materialHeightMap;
        
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
            
            if (_drawingVisualizer == null)
            {
                _drawingVisualizer = GetComponent<DrawingVisualizer>();
            }
            
            _drawingVisualizer.MeshGenerated -= GenerateTerrainTexture;
            _drawingVisualizer.MeshGenerated += GenerateTerrainTexture;
        }

        protected void Update()
        {
            AssignColors();
        }
        
        private void OnDisable()
        {
            _drawingVisualizer.MeshGenerated -= GenerateTerrainTexture;
        }
        
        private void OnValidate()
        {
            Setup();

            GenerateTerrainTexture();
        }

        public Color SampleGradient(float height)
        {
            return _gradient.Evaluate(1f - height / _drawingVisualizer.VertexAmountY);
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
            
            AssignColors();
        }

        private void AssignColors()
        {
            _meshRenderer.sharedMaterial.SetTexture(ColorMap, _texture2D);
            _meshRenderer.sharedMaterial.SetFloat(BaseHeight, _drawingVisualizer.transform.position.y);
            _meshRenderer.sharedMaterial.SetFloat(HeightScale, _drawingVisualizer.VertexAmountY);
        }
    }
}