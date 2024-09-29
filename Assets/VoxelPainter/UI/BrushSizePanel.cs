using System;
using System.Collections.Generic;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    [Serializable]
    public class BrushSizeSettings
    {
        public float BrushSize = 1f;
    }
    
    public class BrushSizePanel : MonoBehaviour
    {
        private const string SizeSettingsSaveKey = "brush_size_settings";
        
        [SerializeField] private Slider _brushSizeSlider;
        [SerializeField] private Button _brushSizeButton;
        [SerializeField] private Transform _brushSizeButtonVisual;
        [SerializeField] private BrushSizeSettings _brushSizeSettings;

        [SerializeField] private int _breakPointCount = 5;
        
        [SerializeField] private Rendering.VoxelPainter _voxelPainter;
        
        private float BreakPointStep => 1f / _breakPointCount;

        private void Awake()
        {
            _brushSizeSettings = SaveManager.Load<BrushSizeSettings>(SizeSettingsSaveKey);
            _brushSizeSettings ??= new BrushSizeSettings();
            
            _brushSizeSettings.BrushSize = float.IsNaN(_brushSizeSettings.BrushSize) ? 1f : _brushSizeSettings.BrushSize;
            _brushSizeSettings.BrushSize = Mathf.Clamp(_brushSizeSettings.BrushSize, 0f, 1f);
            
            _brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            _brushSizeButton.onClick.AddListener(OnBrushSizeButtonClicked);
            
            _voxelPainter.BrushSizeChanged += VoxelPaintedOnBrushSizeChanged;

            UpdateBrushAndVisuals();
        }

        private void OnDestroy()
        {
            _voxelPainter.BrushSizeChanged -= VoxelPaintedOnBrushSizeChanged;
        }
        
        private void VoxelPaintedOnBrushSizeChanged(float size)
        {
            _brushSizeSettings.BrushSize = size;
            SaveManager.Save(SizeSettingsSaveKey, _brushSizeSettings);
            
            // Do not update brush, otherwise you create recursion
            UpdateVisuals();
        }        

        private void UpdateBrushAndVisuals()
        {
            _voxelPainter.BrushSize = _brushSizeSettings.BrushSize;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _brushSizeSlider.SetValueWithoutNotify(_brushSizeSettings.BrushSize);
            _brushSizeButtonVisual.transform.localScale = Vector3.one * BreakPointStep * (GetBreakPointIndex(_brushSizeSettings.BrushSize) + 1);
        }
        
        private void OnBrushSizeChanged(float value)
        {
            _brushSizeSettings.BrushSize = value;
            SaveManager.Save(SizeSettingsSaveKey, _brushSizeSettings);
            
            UpdateBrushAndVisuals();
        }
        
        private void OnBrushSizeButtonClicked()
        {
            float currentSize = _brushSizeSettings.BrushSize;
            int nextBreakPoint = GetBreakPointIndex(currentSize) + 1;
            if (nextBreakPoint == _breakPointCount && Mathf.Approximately(currentSize, 1f))
            {
                nextBreakPoint = 0;
            }
            float nextSize = nextBreakPoint * BreakPointStep;
            
            _brushSizeSettings.BrushSize = nextSize;
            SaveManager.Save(SizeSettingsSaveKey, _brushSizeSettings);
            
            UpdateBrushAndVisuals();
        }

        private int GetBreakPointIndex(float size)
        {
            for (int i = 0; i < _breakPointCount; i++)
            {
                if (size < BreakPointStep * (i + 1))
                {
                    return i;
                }
            }
            
            return _breakPointCount;
        }
    }
}
