using System;
using System.Collections.Generic;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    [Serializable]
    public class FuzzinessSettings
    {
        public float FuzzinessSize = 0.05f;
    }
    
    public class FuzzinessSizePanel : MonoBehaviour
    {
        private const string FuzzinessSettingsSaveKey = "brush_fuzziness_settings";
        
        [SerializeField] private Slider _fuzzinessSlider;
        [SerializeField] private Button _fuzzinessButton;
        

        [SerializeField] private int _breakPointCount = 5;
        [SerializeField] private Image _breakPointImage;
        [SerializeField] private List<Sprite> _breakPointSprites;
        
        
        [SerializeField] private FuzzinessSettings _fuzzinessSettings;
        [SerializeField] private Rendering.VoxelPainter _voxelPainter;
        
        private float BreakPointStep => 1f / _breakPointCount;

        private void OnValidate()
        {
            if (_breakPointSprites != null && _breakPointSprites.Count != _breakPointCount)
            {
                Debug.LogError("Break point sprites count does not match break point count");
            }
        }

        private void Awake()
        {
            _fuzzinessSettings = SaveManager.Load<FuzzinessSettings>(FuzzinessSettingsSaveKey);
            _fuzzinessSettings ??= new FuzzinessSettings();
            
            _fuzzinessSlider.onValueChanged.AddListener(OnFuzzinessSizeChanged);
            _fuzzinessButton.onClick.AddListener(OnFuzzinessSizeButtonClicked);
            
            _voxelPainter.FuzzinessChanged += VoxelPaintedOnFuzzinessChanged;

            UpdateFuzzinessAndVisuals();
        }

        private void OnDestroy()
        {
            _voxelPainter.BrushSizeChanged -= VoxelPaintedOnFuzzinessChanged;
        }
        
        private void VoxelPaintedOnFuzzinessChanged(float size)
        {
            _fuzzinessSettings.FuzzinessSize = size;
            
            // Do not update brush, otherwise you create recursion
            UpdateVisuals();
            SaveManager.Save(FuzzinessSettingsSaveKey, _fuzzinessSettings);
        }        

        private void UpdateFuzzinessAndVisuals()
        {
            _voxelPainter.Fuzziness = _fuzzinessSettings.FuzzinessSize;
            SaveManager.Save(FuzzinessSettingsSaveKey, _fuzzinessSettings);
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _fuzzinessSlider.SetValueWithoutNotify(_fuzzinessSettings.FuzzinessSize);
            _breakPointImage.sprite = _breakPointSprites[GetBreakPointIndex(_fuzzinessSettings.FuzzinessSize)];
        }
        
        private void OnFuzzinessSizeChanged(float value)
        {
            _fuzzinessSettings.FuzzinessSize = value;
            
            UpdateFuzzinessAndVisuals();
        }
        
        private void OnFuzzinessSizeButtonClicked()
        {
            float currentSize = _fuzzinessSettings.FuzzinessSize;
            int nextBreakPoint = GetBreakPointIndex(currentSize) + 1;
            if (nextBreakPoint == _breakPointCount && Mathf.Approximately(currentSize, 1f))
            {
                nextBreakPoint = 0;
            }
            float nextSize = nextBreakPoint * BreakPointStep;
            
            _fuzzinessSettings.FuzzinessSize = nextSize;
            
            UpdateFuzzinessAndVisuals();
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
