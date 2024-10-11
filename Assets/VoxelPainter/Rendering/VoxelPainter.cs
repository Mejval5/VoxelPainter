using System;
using System.Linq;
using Foxworks.Utils;
using Foxworks.Voxels;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelPainter.ControlsManagement;
using VoxelPainter.Rendering.Materials;

namespace VoxelPainter.Rendering
{
    public enum CursorMode
    {
        None,
        Draw,
        Erase,
        ChangeSizeMode
    }
    
    [Serializable]
    public enum PaintMode
    {
        Color,
        Addition
    }
    
    [RequireComponent(typeof(DrawingVisualizer))]
    public class VoxelPainter : MonoBehaviour
    {
        private static readonly int ColorShaderName = Shader.PropertyToID("_Color");
        private static readonly int FuzzinessPowerShaderName = Shader.PropertyToID("_FuzzinessPower");
        private const float MinBrushSize = 0.5f;
        private const float BrushSizePower = 2f;
        
        [Header("References")]
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private MeshRenderer _brushRenderer;
        
        [Header("Basic brush settings")]
        [SerializeField] private bool _draw = true;
        [Range(0f, 1f)] [SerializeField] private float _brushSize = 5f;
        [Range(0f, 1f)] [SerializeField] private float _valueToAdd = 1f;
        
        [Header("Advanced brush settings")]
        [Range(0f, 5f)] [SerializeField] private float _fuzziness = 0f;
        [Range(1f, 5f)] [SerializeField] private float _fuzzinessScale = 2f;
        [Range(0f, 2f)] [SerializeField] private float _offsetOfSphereDraw = 1f;
        [Range(0f, 2f)] [SerializeField] private float _offsetOfSpherePaint = 0.25f;
        
        [Header("Advanced drawing settings")]
        [Range(10f, 500f)] [SerializeField] private float _timeDrawDelayMs = 50f;
        
        [Header("Advanced settings")]
        [SerializeField] private float _rayMarchStepSize = 0.5f;
        
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _drawColor = Color.green;
        [SerializeField] private Color _eraseColor = Color.red;
        [SerializeField] private Color _changeSizeColor = Color.blue;
        
        private Vector3 _changeSizeStartPosition;
        private Vector3 _changeSizeStartScreenPosition;
        private float _changeSizeScreenWorldRatio;

        private long _lastTimeDraw;
        private Vector3 _lastDrawPosition;
        
        private HitMeshInfo _hitInfo;
        
        private DrawingVisualizer _drawingVisualizer;
        
        private Material _brushMaterial;
        private Transform _brushTransform;
        private GameObject _brushGameObject;
        
        public event Action<float> BrushSizeChanged = delegate { };
        public event Action<float> FuzzinessChanged = delegate { };
        public event Action<float> ValueToAddChanged = delegate { };

        private float MouseRadius => Mathf.Pow(_brushSize, BrushSizePower) * BrushSizeMultiplier + MinBrushSize;
        private float BrushSizeMultiplier => _drawingVisualizer.VertexAmount.magnitude / Mathf.Sqrt(3) / 4f;

        private float InnerRadius => MouseRadius / (EffectiveFuzziness + 1f);
        private float EffectiveFuzziness => _fuzziness * _fuzzinessScale;
        
        public PaintMode PaintMode { get; set; } = PaintMode.Color;
        public Color CurrentPaintColor { get; set; } = Color.white;
        
        public CursorMode CurrentCursorMode { get; private set; } = CursorMode.None;
        public float CurrentCursorSpeed { get; private set; }

        public float ValueToAdd
        {
            set
            {
                _valueToAdd = value;
                ValueToAddChanged.Invoke(value);
            }
            get => _valueToAdd;
            
        }

        public float Fuzziness
        {
            set
            {
                FuzzinessChanged.Invoke(value);
                _fuzziness = value;
            }
            get => _fuzziness;
        }

        public float BrushSize
        {
            set
            {
                BrushSizeChanged.Invoke(value);
                _brushSize = value;
            }
            get => _brushSize;
        }
        
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
            CurrentCursorMode = CursorMode.None;
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
            if (_hitInfo.IsHit == false && CurrentCursorMode is not CursorMode.ChangeSizeMode)
            {
                _brushGameObject.SetActive(false);
                return;
            }
            
            Color color = CurrentCursorMode switch
            {
                CursorMode.Draw => _drawColor,
                CursorMode.Erase => _eraseColor,
                CursorMode.ChangeSizeMode => _changeSizeColor,
                _ => _defaultColor
            };

            color *= new Color(1, 1, 1, 0.5f);
            _brushMaterial.SetColor(ColorShaderName, color);
            _brushMaterial.SetFloat(FuzzinessPowerShaderName, EffectiveFuzziness);

            Vector3 position = _changeSizeStartPosition;
            if (CurrentCursorMode is not CursorMode.ChangeSizeMode)
            {
                position = _hitInfo.HitPoint;
                if (PaintMode is PaintMode.Addition)
                {
                    position += _offsetOfSphereDraw * _hitInfo.Ray.direction * MouseRadius;
                }
                else
                {
                    position += _offsetOfSpherePaint * _hitInfo.Ray.direction * MouseRadius;
                }
            }

            _brushTransform.position = position;
            _brushTransform.localScale = Vector3.one * MouseRadius * 2f;
            _brushGameObject.SetActive(true);
        }

        private void ProcessUserChangingBrushSize()
        {
            if (CurrentCursorMode is not CursorMode.ChangeSizeMode)
            {
                return;
            }
            
            if (ScreenUtils.IsMouseInsideScreen == false)
            {
                return;
            }
            
            Vector2 distanceMoved = Input.mousePosition - _changeSizeStartScreenPosition;
            float radiusDelta = distanceMoved.magnitude / _changeSizeScreenWorldRatio;
            
            float brushSize = Mathf.Pow(Mathf.Abs(radiusDelta - MinBrushSize) / BrushSizeMultiplier, 1 / BrushSizePower);

            BrushSize = Mathf.Clamp(brushSize, 0f, 1f);
        }

        private void ProcessUserDrawing()
        {
            if (_hitInfo.IsHit == false)
            {
                return;
            }

            if (CurrentCursorMode is not (CursorMode.Draw or CursorMode.Erase))
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

            float addValue = CurrentCursorMode is CursorMode.Draw ? _valueToAdd : -_valueToAdd;
            Vector3 drawPosition = _brushTransform.position;
            CurrentCursorSpeed = (_lastDrawPosition - drawPosition).magnitude / Time.deltaTime;
            _lastDrawPosition = drawPosition;

            AddValue(drawPosition, addValue);
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
        
        private void ProcessRaycastHit()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (ScreenUtils.IsMouseInsideScreen == false)
            {
                _hitInfo = new HitMeshInfo
                {
                    Ray = ray,
                    IsHit = false
                };
                return;
            }
            
            if (UiUtils.IsPointerOverUi())
            {
                _hitInfo = new HitMeshInfo
                {
                    Ray = ray,
                    IsHit = false
                };
                return;
            }

            Profiler.BeginSample("RayMarch");
            _hitInfo = VoxelRaycaster.RayMarch(
                ray, 
                _rayMarchStepSize, 
                _drawingVisualizer.Threshold, 
                _drawingVisualizer.VertexAmountX, 
                _drawingVisualizer.VertexAmountY, 
                _drawingVisualizer.VertexAmountZ, 
                _drawingVisualizer.VerticesValuesNative, 
                transform.position,
                hitExitWalls: true);
            _hitInfo.Ray = ray;
            Profiler.EndSample();
        }

        private void ProcessButtonUp()
        {
            bool noDraw = Controls.IsKeyPressed(VoxelControlKey.PositivePaint) == false;
            bool noRemove = Controls.IsKeyPressed(VoxelControlKey.NegativePaint) == false;
            bool noAlt = Controls.IsKeyPressed(VoxelControlKey.AltModifier) == false;
            
            bool stopDrawingOrErasing = CurrentCursorMode is CursorMode.Draw && noDraw
                                        || CurrentCursorMode is CursorMode.Erase && noRemove;
            
            bool stopInput = CurrentCursorMode is CursorMode.Draw && noDraw
                             || CurrentCursorMode is CursorMode.Erase && noRemove
                             || CurrentCursorMode is CursorMode.ChangeSizeMode && noAlt
                             || CurrentCursorMode is CursorMode.ChangeSizeMode && noDraw && noRemove;

            if (stopDrawingOrErasing)
            {
                _drawingVisualizer.StopDrawing();
            }
            
            if (stopInput == false)
            {
                return;
            }

            ResetUserInteraction(); 
        }

        private void ProcessButtonDown()
        {
            if (ScreenUtils.IsMouseInsideScreen == false)
            {
                return;
            }
            
            if (CurrentCursorMode is not CursorMode.None)
            {
                return;
            }

            if (UiUtils.IsPointerOverUi())
            {
                return;
            }
            
            bool startedToDraw = Controls.IsKeyDown(VoxelControlKey.PositivePaint);
            bool startedToRemove = Controls.IsKeyDown(VoxelControlKey.NegativePaint);
            bool holdingAltKey = Controls.IsKeyPressed(VoxelControlKey.AltModifier);
            
            bool[] buttonsPressed = {startedToDraw, startedToRemove};
            int amountOfKeys = buttonsPressed.Count(x => x);
            
            bool didPressTooManyButtons = amountOfKeys > 1;
            bool cannotStart = (startedToDraw || startedToRemove) && _hitInfo.IsHit == false;

            if (didPressTooManyButtons || cannotStart)
            {
                return;
            }
            
            if (startedToDraw && holdingAltKey || startedToRemove && holdingAltKey)
            {
                StartChangingSize();
                return;
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
            CurrentCursorMode = CursorMode.Erase;
            _drawingVisualizer.StartDrawing();
        }

        private void StartDrawing()
        {
            CurrentCursorMode = CursorMode.Draw;
            _drawingVisualizer.StartDrawing();
        }

        private void StartChangingSize()
        {
            _changeSizeStartPosition = _hitInfo.HitPoint;
            _changeSizeStartScreenPosition = Input.mousePosition;

            Transform cameraTransform = _mainCamera.transform;
            Vector3 cameraRight = cameraTransform.right;
            
            Vector2 screenPosHit = _mainCamera.WorldToScreenPoint(_changeSizeStartPosition);
            
            Vector2 halfScreenSize = new (Screen.width / 2f, Screen.height / 2f);
            
            Vector3 directionToPick = (screenPosHit - halfScreenSize) switch
            {
                { x: > 0 } => - cameraRight,
                { x: < 0} => cameraRight,
                _ => cameraRight
            };
            
            _changeSizeStartPosition = _hitInfo.HitPoint + MouseRadius * directionToPick.normalized;
            _changeSizeStartScreenPosition = _mainCamera.WorldToScreenPoint(_changeSizeStartPosition);
            _changeSizeScreenWorldRatio = Vector2.Distance(_changeSizeStartScreenPosition, Input.mousePosition) / MouseRadius;
            CurrentCursorMode = CursorMode.ChangeSizeMode;
        }

        private void AddValue(Vector3 position, float value)
        {
            float radius = MouseRadius;
            float innerRadius = InnerRadius;
            float constantA = 1f / Mathf.Pow(innerRadius - radius, 4);

            if (PaintMode is PaintMode.Addition)
            {
                value *= _timeDrawDelayMs / 1000f;
                // Smallest value to add is 1 / VoxelDataUtils.Values, so we need to ensure the value is at least that
                // Fuzziness can affect the value
                value = Mathf.Sign(value) * Mathf.Clamp(Mathf.Abs(value), 1f / VoxelDataUtils.Values * (Fuzziness + 1f), 1f);
            }

            Profiler.BeginSample("DrawingVisualizer.AddValue");
            // Only do update near the center to optimize
            int minX = Mathf.Max(0, Mathf.RoundToInt(position.x - radius));
            int minY = Mathf.Max(0, Mathf.RoundToInt(position.y - radius));
            int minZ = Mathf.Max(0, Mathf.RoundToInt(position.z - radius));
            int maxX = Mathf.Min(_drawingVisualizer.VertexAmountX, Mathf.RoundToInt(position.x + radius));
            int maxY = Mathf.Min(_drawingVisualizer.VertexAmountY, Mathf.RoundToInt(position.y + radius));
            int maxZ = Mathf.Min(_drawingVisualizer.VertexAmountZ, Mathf.RoundToInt(position.z + radius));

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    for (int z = minZ; z < maxZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        float distance = Vector3.Distance(pos, position);
                        if (distance > radius)
                        {
                            continue;
                        }

                        int index = MarchingCubeUtils.ConvertPositionToIndex(pos, _drawingVisualizer.VertexAmount);
                        int currentValuePacked = _drawingVisualizer.VerticesValuesNative[index];
                        
                        float inner = distance - radius;
                        float multiplier = Mathf.Clamp01(constantA * Mathf.Pow(inner, 4));
                        float additionAmount = Mathf.Clamp(value * multiplier, - 1f, 1f);
                        
                        if (PaintMode is PaintMode.Addition)
                        {
                            float writeValue = VoxelDataUtils.UnpackValue(currentValuePacked);
                            writeValue = Mathf.Clamp(writeValue + additionAmount, 0, 1);
                            _drawingVisualizer.WriteIntoGrid(index, writeValue);
                        }
                        else
                        {
                            Color currentColor = VoxelDataUtils.UnpackColor(currentValuePacked);
                            Color newColor;
                            if (additionAmount > 0f)
                            {
                                newColor = Color.Lerp(currentColor, CurrentPaintColor, additionAmount);
                            }
                            else
                            {
                                newColor = Color.Lerp(currentColor, currentColor.grayscale * Color.white, - additionAmount);
                            }
                            
                            _drawingVisualizer.WriteIntoGrid(index, newColor);
                        }
                    }
                }
            }
            
            Profiler.EndSample();
        }
    }
}