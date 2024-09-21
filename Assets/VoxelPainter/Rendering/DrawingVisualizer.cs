using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    public struct HitMeshInfo
    {
        public Vector3 HitPoint;
        public int VertexIndex;
        public bool IsHit;
    }
    
    public enum CurrentPaintMode
    {
        None,
        Draw,
        Erase,
        ChangeSize
    }

    [ExecuteAlways]
    public class DrawingVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private Camera _mainCamera;
        
        [SerializeField] private bool _allowRegeneration = false;
        [field: Range(0.1f, 10f)]
        [field: SerializeField]
        public float InitScale { get; set; } = 2f;
        [Range(1f, 100f)] [SerializeField] private float _mouseRadius = 5f;
        [Range(0f, 1f)] [SerializeField] private float _valueToAdd = 1f;
        [SerializeField] private bool _draw = true;
        [Range(0f, 0.5f)] [SerializeField] private float _offsetOfSphereDraw = 0.3f;
        [Range(0f, 5f)] [SerializeField] private float _fuzziness = 2f;
        [Range(10f, 500f)] [SerializeField] private float _timeDrawDelayMs = 50f;
        [SerializeField] private float _changeSizeSpeed = 0.1f;
        [SerializeField] private float _cursorSizePixels = 50f;
        [SerializeField] private float _rayMarchStepSize = 0.5f;

        [HideInInspector] [SerializeField] private float[] _serializedDrawing;

        private float _mouseRadiusChangeSizeStart;
        private float _changeSizeScreenSpaceDistance;
        private Vector2 _changeSizeStartPosition;
        private Vector3 _changeSizeAnchor;

        private long _lastTimeDraw;
        private bool _generating;
        
        private CurrentPaintMode _currentPaintMode = CurrentPaintMode.Draw;
        
        private NativeArray<float> _verticesValuesNative;
        private Vector3Int _cachedVertexAmount;
        
        private HitMeshInfo _hitInfo;

        protected override void GenerateMesh()
        {
            try
            {
                _generating = true;

                bool didChange = false;
                if (_verticesValuesNative.IsCreated == false
                    || _cachedVertexAmount.x != VertexAmountX
                    || _cachedVertexAmount.y != VertexAmountY
                    || _cachedVertexAmount.z != VertexAmountZ)
                {
                    MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ));
                    _cachedVertexAmount = new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ);
                    didChange = true;
                }

                if (_allowRegeneration || didChange)
                {
                    InitDrawing();
                }

                base.GenerateMesh();

                if (_serializedDrawing == null || _serializedDrawing.Length != VertexAmountX * VertexAmountY * VertexAmountZ)
                {
                    _serializedDrawing = new float[VertexAmountX * VertexAmountY * VertexAmountZ];
                }

                _verticesValuesNative.CopyTo(_serializedDrawing);
            }
            catch
            {
                // ignored0
            }
            finally
            {
                _generating = false;
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();

            if (_serializedDrawing == null || _allowRegeneration || _serializedDrawing.Length != VertexAmountX * VertexAmountY * VertexAmountZ)
            {
                GenerateMesh();
                return;
            }

            MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ));
            _cachedVertexAmount = new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ);
            _verticesValuesNative.CopyFrom(_serializedDrawing);

            GenerateMesh();
        }

        private void ResetUserInteraction()
        {
            _currentPaintMode = CurrentPaintMode.None;
        }
        
        protected void Update()
        {
            if (_draw == false)
            {
                return;
            }

            ProcessUserInput();

            ShowVisualIndicatorToUser();
            
            ProcessUserChangingBrushSize();

            ProcessUserDrawing();
        }
        
        private void ShowVisualIndicatorToUser()
        {
            if (_currentPaintMode is CurrentPaintMode.None)
            {
                return;
            }
            
            if (_hitInfo.IsHit == false)
            {
                return;
            }
            
            Color color = _currentPaintMode switch
            {
                CurrentPaintMode.Draw => Color.green,
                CurrentPaintMode.Erase => Color.red,
                CurrentPaintMode.ChangeSize => Color.blue,
                _ => Color.white
            };

            Handles.color = color;
            Handles.color *= new Color(1, 1, 1, 0.5f);

            Handles.SphereHandleCap(0, _hitInfo.HitPoint, Quaternion.identity, _mouseRadius, EventType.Repaint);
        }

        private void ProcessUserChangingBrushSize()
        {
            if (_currentPaintMode is not CurrentPaintMode.ChangeSize)
            {
                return;
            }

            Vector2 mouseScreenPosition = Input.mousePosition;
            float changeSize = ((mouseScreenPosition - _changeSizeStartPosition).magnitude - _changeSizeScreenSpaceDistance) * _changeSizeSpeed;
            _mouseRadius = Mathf.Clamp(_mouseRadiusChangeSizeStart + changeSize, 0f, 100f);
        }

        private void ProcessUserDrawing()
        {
            if (_hitInfo.IsHit == false)
            {
                return;
            }

            if (_currentPaintMode is not (CurrentPaintMode.Draw or CurrentPaintMode.Erase))
            {
                return;
            }

            // Only draw every 100ms
            float timeDrawDelay = _timeDrawDelayMs * 1000f;
            if (DateTime.Now.Ticks - _lastTimeDraw < timeDrawDelay || _generating)
            {
                return;
            }

            _lastTimeDraw = DateTime.Now.Ticks;

            float addValue = _currentPaintMode is CurrentPaintMode.Draw ? _valueToAdd : -_valueToAdd;

            AddValue(_hitInfo.HitPoint, _mouseRadius, addValue);
            GenerateMesh();
        }

        private void ProcessUserInput()
        {
            Profiler.BeginSample("ProcessUserInput");
            
            Profiler.BeginSample("ProcessUserInput.ProcessButtonDown");
            ProcessButtonDown();
            Profiler.EndSample();
            
            Profiler.BeginSample("ProcessUserInput.ProcessButtonUp");
            ProcessButtonUp();
            Profiler.EndSample();
            
            Profiler.EndSample();
        }

        private void ProcessButtonUp()
        {
            bool endedDraw = Input.GetMouseButtonUp(0);
            bool endedDelete = Input.GetMouseButtonUp(1);
            bool endedChangeSize = Input.GetMouseButtonUp(2);
            
            bool stopInput = _currentPaintMode is CurrentPaintMode.Draw && endedDraw
                             || _currentPaintMode is CurrentPaintMode.Erase && endedDelete
                             || _currentPaintMode is CurrentPaintMode.ChangeSize && endedChangeSize;

            if (!stopInput)
            {
                return;
            }

            ResetUserInteraction(); 
        }

        private void ProcessButtonDown()
        {
            _hitInfo = new HitMeshInfo();
            
            Vector2 mouseScreenPosition = Input.mousePosition;
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (mouseScreenPosition is { x: >= 0, y: >= 0 }
                && mouseScreenPosition.x < Screen.width
                && mouseScreenPosition.y < Screen.height)
            {
                Profiler.BeginSample("RayMarch");
                _hitInfo = VoxelRaycaster.RayMarch(ray, _rayMarchStepSize, Threshold, VertexAmountX, VertexAmountY, VertexAmountZ, 
                    MarchingCubesVisualizer.ReadOnlyVerticesValuesNative, transform.position);
                Profiler.EndSample();
            }
            
            bool startedToDraw = Input.GetMouseButtonDown(0);
            bool startedToRemove = Input.GetMouseButtonDown(1);
            bool startedToChangeSize = Input.GetMouseButtonDown(2);
            
            bool didPressTooManyButtons = startedToDraw && startedToRemove || startedToDraw && startedToChangeSize || startedToRemove && startedToChangeSize;
            bool cannotStart = (startedToDraw || startedToRemove) && _hitInfo.IsHit == false;

            if (didPressTooManyButtons || cannotStart)
            {
                ResetUserInteraction();
                return;
            }
            
            if (startedToChangeSize)
            {
                StartChangingSize(mouseScreenPosition);
            }

            if (startedToDraw)
            {
                StartDrawing();
            }
                    
            if (startedToRemove)
            {
                StartErasing();
            }
        }

        private void StartErasing()
        {
            _currentPaintMode = CurrentPaintMode.Erase;
        }

        private void StartDrawing()
        {
            _currentPaintMode = CurrentPaintMode.Draw;
        }

        private void StartChangingSize(Vector2 mouseScreenPosition)
        {
            _currentPaintMode = CurrentPaintMode.ChangeSize;
            _mouseRadiusChangeSizeStart = _mouseRadius;
            _changeSizeScreenSpaceDistance = _mouseRadius * _cursorSizePixels;
            _changeSizeAnchor = _hitInfo.HitPoint - Vector3.right * _mouseRadius / 2f;
            _changeSizeStartPosition = HandleUtility.WorldToGUIPoint(_changeSizeAnchor);
            _changeSizeScreenSpaceDistance = (mouseScreenPosition - _changeSizeStartPosition).magnitude;
        }

        private void AddValue(Vector3 position, float radius, float value)
        {
            Profiler.BeginSample("DrawingVisualizer.AddValue");
            // Only do update near the center to optimize
            int minX = Mathf.Max(0, (int)(position.x - radius));
            int minY = Mathf.Max(0, (int)(position.y - radius));
            int minZ = Mathf.Max(0, (int)(position.z - radius));
            int maxX = Mathf.Min(VertexAmountX, (int)(position.x + radius));
            int maxY = Mathf.Min(VertexAmountY, (int)(position.y + radius));
            int maxZ = Mathf.Min(VertexAmountZ, (int)(position.z + radius));

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    for (int z = minZ; z < maxZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        int index = MarchingCubeUtils.ConvertPositionToIndex(pos, VertexAmount);
                        float distance = Vector3.Distance(pos, position);
                        float effectiveRadius = radius * (1 + _fuzziness);
                        float additionAmount = Mathf.Clamp(value * (1 - Mathf.Pow(distance / effectiveRadius, 4)), -1, 1);
                        if (distance <= effectiveRadius)
                        {
                            _verticesValuesNative[index] = Mathf.Clamp(_verticesValuesNative[index] + additionAmount, 0, 1);
                        }
                    }
                }
            }
            Profiler.EndSample();
        }

        private void InitDrawing()
        {
            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        int index = MarchingCubeUtils.ConvertPositionToIndex(pos, VertexAmount);
                        float sineOffset = InitScale * Mathf.PerlinNoise(z * GenerationProperties.Frequency / 51.164658416f, x * GenerationProperties.Frequency / 87.1777416f);
                        _verticesValuesNative[index] = y < VertexAmountY / 2f + sineOffset ? 1 : 0;
                    }
                }
            }
        }

        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            // Do nothing
        }
    }
}