using System;
using Foxworks.Voxels;
using UnityEngine;
using UnityEngine.Profiling;

namespace VoxelPainter.Rendering
{
    public enum CurrentPaintMode
    {
        None,
        Draw,
        Erase,
        ChangeSize
    }
    
    [RequireComponent(typeof(DrawingVisualizer))]
    public class VoxelPainter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private MeshRenderer _brushRenderer;
        
        [Header("Basic brush settings")]
        [SerializeField] private bool _draw = true;
        [Range(1f, 20f)] [SerializeField] private float _mouseRadius = 5f;
        [Range(0f, 1f)] [SerializeField] private float _valueToAdd = 1f;
        
        [Header("Advanced brush settings")]
        [Range(0f, 5f)] [SerializeField] private float _fuzziness = 2f;
        [Range(0f, 0.5f)] [SerializeField] private float _offsetOfSphereDraw = 0.3f;
        [SerializeField] private float _changeSizeSpeed = 0.1f;
        [SerializeField] private float _cursorSizePixels = 50f;
        
        [Header("Advanced drawing settings")]
        [Range(10f, 500f)] [SerializeField] private float _timeDrawDelayMs = 50f;
        
        [Header("Advanced settings")]
        [SerializeField] private float _rayMarchStepSize = 0.5f;

        private CurrentPaintMode _currentPaintMode = CurrentPaintMode.None;
        private long _lastTimeDraw;
        
        private float _mouseRadiusChangeSizeStart;
        private float _changeSizeScreenSpaceDistance;
        private Vector2 _changeSizeStartPosition;
        
        private HitMeshInfo _hitInfo;
        
        private DrawingVisualizer _drawingVisualizer;
        
        private Material _brushMaterial;
        private Transform _brushTransform;
        private GameObject _brushGameObject;

        protected void Awake()
        {
            _drawingVisualizer = GetComponent<DrawingVisualizer>();
            
            _brushMaterial = _brushRenderer.material;
            _brushTransform = _brushRenderer.transform;
            _brushGameObject = _brushRenderer.gameObject;
        }

        protected void OnDestroy()
        {
            Destroy(_brushMaterial);
            _brushMaterial = null;
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

            Profiler.BeginSample("ProcessUserInput");
            ProcessUserInput();
            Profiler.EndSample();

            Profiler.BeginSample("ShowVisualIndicatorToUser");
            ShowVisualIndicatorToUser();
            Profiler.EndSample();
            
            Profiler.BeginSample("ProcessUserChangingBrushSize");
            ProcessUserChangingBrushSize();
            Profiler.EndSample();

            Profiler.BeginSample("ProcessUserDrawing");
            ProcessUserDrawing();
            Profiler.EndSample();
        }
        
        private void ShowVisualIndicatorToUser()
        {
            if (_hitInfo.IsHit == false)
            {
                _brushGameObject.SetActive(false);
                return;
            }
            
            Color color = _currentPaintMode switch
            {
                CurrentPaintMode.Draw => Color.green,
                CurrentPaintMode.Erase => Color.red,
                CurrentPaintMode.ChangeSize => Color.blue,
                _ => Color.white
            };

            color *= new Color(1, 1, 1, 0.5f);
            _brushMaterial.color = color;
            
            Vector3 position = _hitInfo.HitPoint + _offsetOfSphereDraw * _hitInfo.Ray.direction;

            _brushTransform.position = position;
            _brushTransform.localScale = Vector3.one * _mouseRadius * 2f;
            _brushGameObject.SetActive(true);
        }

        private void ProcessUserChangingBrushSize()
        {
            if (_currentPaintMode is not CurrentPaintMode.ChangeSize)
            {
                return;
            }
            
            if (IsMouseInsideScreen() == false)
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
            if (DateTime.Now.Ticks - _lastTimeDraw < timeDrawDelay || _drawingVisualizer.Generating)
            {
                return;
            }

            _lastTimeDraw = DateTime.Now.Ticks;

            float addValue = _currentPaintMode is CurrentPaintMode.Draw ? _valueToAdd : -_valueToAdd;

            AddValue(_hitInfo.HitPoint, _mouseRadius, addValue);
            _drawingVisualizer.GenerateMesh();
        }

        private void ProcessUserInput()
        {
            Profiler.BeginSample("ProcessUserInput.ProcessRaycastHit");
            ProcessRaycastHit();
            Profiler.EndSample();
            
            Profiler.BeginSample("ProcessUserInput.ProcessButtonDown");
            ProcessButtonDown();
            Profiler.EndSample();
            
            Profiler.BeginSample("ProcessUserInput.ProcessButtonUp");
            ProcessButtonUp();
            Profiler.EndSample();
        }

        private static bool IsMouseInsideScreen()
        {
            Vector2 mouseScreenPosition = Input.mousePosition;
            
            return mouseScreenPosition is { x: >= 0, y: >= 0 }
                   && mouseScreenPosition.x < Screen.width
                   && mouseScreenPosition.y < Screen.height;
        }
        
        private void ProcessRaycastHit()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (IsMouseInsideScreen() == false)
            {
                _hitInfo = new HitMeshInfo
                {
                    Ray = ray
                };
                return;
            }

            Profiler.BeginSample("RayMarch");
            _hitInfo = VoxelRaycaster.RayMarch(ray, _rayMarchStepSize, _drawingVisualizer.Threshold, _drawingVisualizer.VertexAmountX, _drawingVisualizer.VertexAmountY, _drawingVisualizer.VertexAmountZ, _drawingVisualizer.VerticesValuesNative, transform.position);
            _hitInfo.Ray = ray;
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
                StartChangingSize();
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

        private void StartChangingSize()
        {
            _currentPaintMode = CurrentPaintMode.ChangeSize;
            _mouseRadiusChangeSizeStart = _mouseRadius;
            _changeSizeScreenSpaceDistance = _mouseRadius * _cursorSizePixels;
            Vector2 mouseScreenPosition = Input.mousePosition;
            Vector2 direction = mouseScreenPosition.x > Screen.width / 2f ? Vector2.left : Vector2.right;
            _changeSizeStartPosition = (mouseScreenPosition + direction) * _mouseRadius / 2f;
            _changeSizeScreenSpaceDistance = (mouseScreenPosition - _changeSizeStartPosition).magnitude;
        }

        private void AddValue(Vector3 position, float radius, float value)
        {
            Profiler.BeginSample("DrawingVisualizer.AddValue");
            // Only do update near the center to optimize
            int minX = Mathf.Max(0, (int)(position.x - radius));
            int minY = Mathf.Max(0, (int)(position.y - radius));
            int minZ = Mathf.Max(0, (int)(position.z - radius));
            int maxX = Mathf.Min(_drawingVisualizer.VertexAmountX, (int)(position.x + radius));
            int maxY = Mathf.Min(_drawingVisualizer.VertexAmountY, (int)(position.y + radius));
            int maxZ = Mathf.Min(_drawingVisualizer.VertexAmountZ, (int)(position.z + radius));

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    for (int z = minZ; z < maxZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        int index = MarchingCubeUtils.ConvertPositionToIndex(pos, _drawingVisualizer.VertexAmount);
                        float distance = Vector3.Distance(pos, position);
                        float effectiveRadius = radius * (1 + _fuzziness);
                        float additionAmount = Mathf.Clamp(value * (1 - Mathf.Pow(distance / effectiveRadius, 4)), -1, 1);
                        if (!(distance <= effectiveRadius))
                        {
                            continue;
                        }

                        float writeValue = Mathf.Clamp(_drawingVisualizer.VerticesValuesNative[index] + additionAmount, 0, 1);
                        _drawingVisualizer.WriteIntoGrid(index, writeValue);
                    }
                }
            }
            Profiler.EndSample();
        }
    }
}