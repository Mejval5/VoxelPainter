using System;
using System.Collections.Generic;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    [Serializable]
    public class ThresholdSettings
    {
        public float Threshold = 0.5f;
        public bool Lerp = true;
    }
    
    public class ThresholdPanel : MonoBehaviour
    {
        private const string ThresholdSettingsSaveKey = "threshold_settings";
        
        [SerializeField] private Slider _slider;
        [SerializeField] private Button _disableLerpButton;
        [SerializeField] private Button _enableLerpButton;
        
        [SerializeField] private ThresholdSettings _thresholdSettings;
        
        [SerializeField] private Rendering.DrawingVisualizer _drawingVisualizer;

        private void Awake()
        {
            _thresholdSettings = SaveManager.Load<ThresholdSettings>(ThresholdSettingsSaveKey);
            _thresholdSettings ??= new ThresholdSettings();
            
            _slider.maxValue = 1f - 1E-06f;;
            _slider.minValue = 1E-06f;
            
            _slider.onValueChanged.AddListener(OnThresholdSliderChanged);
            _disableLerpButton.onClick.AddListener(() => ToggleLerp(false));
            _enableLerpButton.onClick.AddListener(() => ToggleLerp(true));
            
            
            _drawingVisualizer.ThresholdChanged += VoxelPaintedOnThresholdChanged;
            _drawingVisualizer.LerpChanged += VoxelPaintedOnLerpChanged;

            UpdateSettingsAndVisuals();
        }
        private void ToggleLerp(bool lerp)
        {
            _thresholdSettings.Lerp = lerp;
            SaveManager.Save(ThresholdSettingsSaveKey, _thresholdSettings);
            
            UpdateSettingsAndVisuals();
        }

        private void OnDestroy()
        {
            _drawingVisualizer.ThresholdChanged -= VoxelPaintedOnThresholdChanged;
            _drawingVisualizer.LerpChanged -= VoxelPaintedOnLerpChanged;
        }
        
        private void VoxelPaintedOnLerpChanged(bool lerp)
        {
            _thresholdSettings.Lerp = lerp;
            SaveManager.Save(ThresholdSettingsSaveKey, _thresholdSettings);
            
            // Do not update settings, otherwise you create recursion
            UpdateVisuals();
        }
        
        private void VoxelPaintedOnThresholdChanged(float size)
        {
            _thresholdSettings.Threshold = size;
            SaveManager.Save(ThresholdSettingsSaveKey, _thresholdSettings);
            
            // Do not update settings, otherwise you create recursion
            UpdateVisuals();
        }        

        private void UpdateSettingsAndVisuals()
        {
            _drawingVisualizer.Threshold = _thresholdSettings.Threshold;
            _drawingVisualizer.Lerp = _thresholdSettings.Lerp;
            _drawingVisualizer.GenerateMesh();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _slider.SetValueWithoutNotify(_thresholdSettings.Threshold);
            _disableLerpButton.gameObject.SetActive(_thresholdSettings.Lerp);
            _enableLerpButton.gameObject.SetActive(!_thresholdSettings.Lerp);
        }
        
        private void OnThresholdSliderChanged(float value)
        {
            _thresholdSettings.Threshold = value;
            SaveManager.Save(ThresholdSettingsSaveKey, _thresholdSettings);
            
            UpdateSettingsAndVisuals();
        }
    }
}
