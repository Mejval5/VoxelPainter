using System;
using System.Collections.Generic;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    [Serializable]
    public class PaintValueSettings
    {
        public float ValueToAdd = 0.03f;
    }
    
    public class PaintValuePanel : MonoBehaviour
    {
        private const string PaintValueSettingsSaveKey = "paint_value_settings";

        [SerializeField] private Vector2 _range = new (1E-3f, 0.1f);
        
        [SerializeField] private Slider _slider;
        [SerializeField] private Button _changeToColorButton;
        [SerializeField] private Button _changeToValueButton;
        
        [SerializeField] private PaintValueSettings _paintValueSettings;
        
        [SerializeField] private Rendering.VoxelPainter _voxelPainter;

        private void Awake()
        {
            _paintValueSettings = SaveManager.Load<PaintValueSettings>(PaintValueSettingsSaveKey);
            _paintValueSettings ??= new PaintValueSettings();
            
            // Do before subscribing as it will trigger the event
            _slider.minValue = _range.x;
            _slider.maxValue = _range.y;
            
            _slider.onValueChanged.AddListener(OnSliderChanged);
            
            // TODO: Implement this
            // _changeToColorButton.onClick.AddListener(() => ToggleLerp(false));
            // _changeToValueButton.onClick.AddListener(() => ToggleLerp(true));
            
            _voxelPainter.ValueToAddChanged += VoxelPaintedOnValueToAddChanged;

            UpdateSettingsAndVisuals();
        }

        private void OnDestroy()
        {
            _voxelPainter.ValueToAddChanged -= VoxelPaintedOnValueToAddChanged;
        }
        
        private void VoxelPaintedOnValueToAddChanged(float val)
        {
            _paintValueSettings.ValueToAdd = val;
            SaveManager.Save(PaintValueSettingsSaveKey, _paintValueSettings);
            
            // Do not update settings, otherwise you create recursion
            UpdateVisuals();
        }        

        private void UpdateSettingsAndVisuals()
        {
            _voxelPainter.ValueToAdd = _paintValueSettings.ValueToAdd;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _slider.SetValueWithoutNotify(_paintValueSettings.ValueToAdd);
        }
        
        private void OnSliderChanged(float value)
        {
            _paintValueSettings.ValueToAdd = value;
            SaveManager.Save(PaintValueSettingsSaveKey, _paintValueSettings);
            
            UpdateSettingsAndVisuals();
        }
    }
}
