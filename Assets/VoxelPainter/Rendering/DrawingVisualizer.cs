using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Foxworks.Persistence;
using Foxworks.Voxels;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelPainter.Rendering.Utils;
using VoxelPainter.VoxelVisualization;
using Debug = UnityEngine.Debug;

namespace VoxelPainter.Rendering
{
    [Serializable]
    public enum HeightmapInitType
    {
        PerlinNoise,
        Texture
    }

    [Serializable]
    public class PaintingSaveData
    {
        public int[] VerticesValues;
        public Vector3Int VertexAmount;
    }

    [Serializable]
    public class PaintingPreviewData
    {
        public const string SaveAppendKey = "_preview";
        public const string ImageExtension = "png";
        public const string MetaDataExtension = "json";
        
        public string SaveName;
        
        public byte[] ImageData;
        public PaintingPreviewMetaData PreviewMetaData;
    }

    [Serializable]
    public class PaintingPreviewMetaData
    {
        public Vector2Int PreviewTextureSize;
    }

    [Serializable]
    public class PaintingSaveHistoryData
    {
        public List<string> SaveNames = new();
    }
    
    [ExecuteAlways]
    public class DrawingVisualizer : MarchingCubeRendererBase
    {
        private const string DrawingHistorySaveKey = "drawing_history";
        
        [SerializeField] private bool _allowRegeneration = false;
        [SerializeField] private HeightmapInitType _heightmapInitType;
        
        [field: Header("Perlin Noise Parameters")]
        [field: Range(0f, 100f)] [field: SerializeField] public float InitOffset { get; set; } = 0f;
        [field: Range(1f, 100f)] [field: SerializeField] public float InitScale { get; set; } = 2f;
        [field: Range(1f, 100f)] [field: SerializeField] public float InitFrequency { get; set; } = 2f;
        
        [Header("Save Reference")]
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _previewRenderTexture;
        
        [Header("Texture Sample Parameters")]
        [SerializeField] private Texture2D _texture2D;
        [SerializeField] private float _textureScale = 1f;
        [SerializeField] private float _textureOffset = 1f;
        [SerializeField] private float _textureScalePower = 1f;

        [NaughtyAttributes.ReadOnly] [SerializeField] private string _currentSaveName;
        [NaughtyAttributes.ReadOnly] [SerializeField] private PaintingSaveHistoryData _paintingSaveHistoryData;
        
        private NativeArray<int> _verticesValuesNative;
        private Vector3Int _cachedVertexAmount;
        
        public NativeArray<int> VerticesValuesNative => _verticesValuesNative;
        
        public bool Generating { get; private set; }
        
        private readonly ObservableCollection<PaintingSaveData> _undoStack = new();
        private readonly ObservableCollection<PaintingSaveData> _redoStack = new();
        
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        
        public event Action UndoRedoStateChanged = delegate { };
        
        public PaintingSaveHistoryData PaintingSaveHistoryData => _paintingSaveHistoryData;
        public string CurrentSaveName => _currentSaveName;

        public DrawingVisualizer()
        {
            _undoStack.CollectionChanged += OnUndoCollectionChanged;
            _redoStack.CollectionChanged += OnRedoCollectionChanged;
        }

        private void OnUndoCollectionChanged(object _, NotifyCollectionChangedEventArgs __)
        {
            UndoRedoStateChanged.Invoke();
        }

        private void OnRedoCollectionChanged(object _, NotifyCollectionChangedEventArgs __)
        {
            UndoRedoStateChanged.Invoke();
        }
        
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
                    CreateNewDrawing();
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

        public void StartDrawing()
        {
            _redoStack.Clear();
        }

        public void StopDrawing()
        {
            Save();
            CreateUndoPoint();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            PaintingSaveData saveData = _undoStack.Last();
            _undoStack.Remove(saveData);
            _redoStack.Add(saveData);
            
            _verticesValuesNative.CopyFrom(saveData.VerticesValues);
            _cachedVertexAmount = saveData.VertexAmount;
            
            GenerateMesh();
        }

        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                return;
            }

            PaintingSaveData saveData = _redoStack.Last();
            _redoStack.Remove(saveData);
            _undoStack.Add(saveData);
            
            _verticesValuesNative.CopyFrom(saveData.VerticesValues);
            _cachedVertexAmount = saveData.VertexAmount;
            
            GenerateMesh();
        }

        private void CreateUndoPoint()
        {
            Profiler.BeginSample("CreateUndoSavePoint");
            PaintingSaveData saveData = new()
            {
                VerticesValues = _verticesValuesNative.ToArray(),
                VertexAmount = _cachedVertexAmount
            };

            _undoStack.Add(saveData);
            Profiler.EndSample();
        }

        public void Save()
        {
            if (_verticesValuesNative.IsCreated == false)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(_currentSaveName))
            {
                return;
            }
            
            Profiler.BeginSample("SaveDrawing.ConvertToArray");
            PaintingSaveData saveData = new()
            {
                VerticesValues = _verticesValuesNative.ToArray(),
                VertexAmount = _cachedVertexAmount
            };
            Profiler.EndSample();
            
            if (_paintingSaveHistoryData.SaveNames.Contains(_currentSaveName))
            {
                // Remove the current save name from the list, so it can be added to the end of the list.
                _paintingSaveHistoryData.SaveNames.Remove(_currentSaveName);
            }

            _paintingSaveHistoryData.SaveNames.Add(_currentSaveName);

            Profiler.BeginSample("SaveDrawing.TakeSnapshot");
            _previewCamera.Render();
            Profiler.EndSample();
            
            Profiler.BeginSample("SaveDrawing.TakeSnapshot.ReadPixels");
            RenderTexture.active = _previewRenderTexture;
            Texture2D texture2D = new (_previewRenderTexture.width, _previewRenderTexture.height);
            texture2D.ReadPixels(new Rect(0, 0, _previewRenderTexture.width, _previewRenderTexture.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            Profiler.EndSample();
            
            Profiler.BeginSample("SaveDrawing.SerializeSnapshot");
            PaintingPreviewData previewData = new()
            {
                ImageData = texture2D.EncodeToPNG(),
                PreviewMetaData = new PaintingPreviewMetaData
                {
                    PreviewTextureSize = new Vector2Int(texture2D.width, texture2D.height)
                }
            };
            Profiler.EndSample();
            
            Profiler.BeginSample("SaveDrawing.Save.History");
            _ = SaveManager.SaveAsync(DrawingHistorySaveKey, _paintingSaveHistoryData);
            Profiler.EndSample();
            
            Profiler.BeginSample("SaveDrawing.Save.Data");
            _ = SaveManager.SaveAsync(_currentSaveName, saveData);
            Profiler.EndSample();
            
            Profiler.BeginSample("SaveDrawing.Save.Preview");
            string saveName = _currentSaveName + PaintingPreviewData.SaveAppendKey;
            _ = SaveManager.SaveAsync(saveName, previewData.ImageData, PaintingPreviewData.ImageExtension);
            _ = SaveManager.SaveAsync(saveName, previewData.PreviewMetaData, PaintingPreviewData.MetaDataExtension);
            Profiler.EndSample();
        }

        public bool Load(string paintingName)
        {
            Debug.Log("Loading painting: " + paintingName);
            
            Profiler.BeginSample("LoadDrawing");
            MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ));
            
            PaintingSaveData saveData = SaveManager.Load<PaintingSaveData>(paintingName);

            if (saveData?.VerticesValues == null)
            {
                Debug.LogWarning("Failed to load painting data.");
                Profiler.EndSample();
                return false;
            }
            
            if (_paintingSaveHistoryData.SaveNames.Contains(paintingName))
            {
                // Remove the current save name from the list, so it can be added to the end of the list.
                _paintingSaveHistoryData.SaveNames.Remove(paintingName);
            }
            
            _paintingSaveHistoryData.SaveNames.Add(paintingName);
            
            Profiler.BeginSample("SaveDrawing.Save.History");
            _ = SaveManager.SaveAsync(DrawingHistorySaveKey, _paintingSaveHistoryData);
            Profiler.EndSample();
            
            _verticesValuesNative.CopyFrom(saveData.VerticesValues);
            
            _cachedVertexAmount = saveData.VertexAmount;
            
            _currentSaveName = paintingName;
            
            _undoStack.Clear();
            _undoStack.Add(saveData);
            
            Profiler.EndSample();
            return true;
        }

        private bool LoadLast()
        {
            _paintingSaveHistoryData = LoadSaveHistoryData();

            return _paintingSaveHistoryData.SaveNames.Any() && Load(_paintingSaveHistoryData.SaveNames.Last());
        }
        
        private PaintingSaveHistoryData LoadSaveHistoryData()
        {
            PaintingSaveHistoryData paintingSaveHistoryData = SaveManager.Load<PaintingSaveHistoryData>(DrawingHistorySaveKey);
            paintingSaveHistoryData ??= new PaintingSaveHistoryData();
            
            foreach (string saveName in paintingSaveHistoryData.SaveNames.ToList())
            {
                if (SaveManager.Exists(saveName) == false)
                {
                    paintingSaveHistoryData.SaveNames.Remove(saveName);
                }
            }
            
            return paintingSaveHistoryData;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            _undoStack.Clear();
            _redoStack.Clear();

            bool didLoadSomething = LoadLast();
            
            if (didLoadSomething == false)
            {
                CreateNewDrawing();
            }

            GenerateMesh();
        }

        public void NewDrawing()
        {
            CreateNewDrawing();
            GenerateMesh();
        }

        private void CreateNewDrawing()
        {
            Save();
            
            Profiler.BeginSample("InitDrawing");
            if (_heightmapInitType == HeightmapInitType.PerlinNoise)
            {
                InitDrawingUsingPerlinNoise();
            }
            else
            {
                InitDrawingUsingTexture();
            }
            
            _undoStack.Clear();
            _redoStack.Clear();
            
            _undoStack.Add(new PaintingSaveData
            {
                VerticesValues = _verticesValuesNative.ToArray(),
                VertexAmount = _cachedVertexAmount
            });

            _currentSaveName = Guid.NewGuid().ToString();

            Profiler.EndSample();
        }

        private void InitDrawingUsingTexture()
        {
            if (_texture2D == null)
            {
                Debug.LogWarning("Texture2D is null. Initialized painting using perlin noise.");
                InitDrawingUsingPerlinNoise();
                return;
            }

            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        float valueThreshold = _texture2D.GetPixelBilinear(x / (float) VertexAmountX, z / (float) VertexAmountZ).r;
                        valueThreshold = Mathf.Pow(valueThreshold, _textureScalePower);
                        valueThreshold = valueThreshold * _textureScale + _textureOffset;
                        valueThreshold *= VertexAmountY;
                        float value = (2f - y / valueThreshold) * Threshold;
                        WriteIntoGrid(pos, value);
                    }
                }
            }
        }

        private void InitDrawingUsingPerlinNoise()
        {
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
        }

        public void WriteIntoGrid(Vector3Int pos, float value)
        {
            int index = MarchingCubeUtils.ConvertPositionToIndex(pos, VertexAmount);
            WriteIntoGrid(index, value);
        }

        public void WriteIntoGrid(int index, float value)
        {
            if (_verticesValuesNative.IsCreated == false)
            {
                return;
            }
            
            _verticesValuesNative[index] = VoxelDataUtils.PackValueAndVertexId(value, 0);
        }

        public override void GetVertexValues(NativeArray<int> verticesValues)
        {
            // Do nothing
        }
    }
}