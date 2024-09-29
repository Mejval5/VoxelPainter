using System;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    [Serializable]
    public class VolumeSettings
    {
        public float Volume = 1f;
        public bool IsMuted = false;
    }
    
    public class VolumeSlider : MonoBehaviour
    {
        private const string VolumeSettingsKey = "volume_settings";
        private const string VolumeMixerKey = "Volume";

        
        [SerializeField] private CanvasGroup _sliderCanvasGroup;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Button _muteButton;
        [SerializeField] private Button _unMuteButton;

        [SerializeField] private AudioMixer _audioMixer;
        
        [SerializeField] private VolumeSettings _volumeSettings;
        
        private void Awake()
        {
            _volumeSettings = SaveManager.Load<VolumeSettings>(VolumeSettingsKey);
            _volumeSettings ??= new VolumeSettings();
            
            _volumeSlider.value = _volumeSettings.Volume;
            
            UpdateSound();
            
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            _muteButton.onClick.AddListener(OnMute);
            _unMuteButton.onClick.AddListener(OnUnMute);
        }
        
        private void OnVolumeChanged(float value)
        {
            _volumeSettings.Volume = value;
            SaveManager.Save(VolumeSettingsKey, _volumeSettings);
            UpdateSound();
        }
        
        private void OnMute()
        {
            _volumeSettings.IsMuted = true;
            SaveManager.Save(VolumeSettingsKey, _volumeSettings);
            UpdateSound();
        }
        
        private void OnUnMute()
        {
            _volumeSettings.IsMuted = false;
            SaveManager.Save(VolumeSettingsKey, _volumeSettings);
            UpdateSound();
        }

        private void UpdateSound()
        {
            _muteButton.gameObject.SetActive(!_volumeSettings.IsMuted);
            _unMuteButton.gameObject.SetActive(_volumeSettings.IsMuted);
            _sliderCanvasGroup.alpha = _volumeSettings.IsMuted ? 0.5f : 1f;
            
            float value = _volumeSettings.IsMuted ? 0 : _volumeSettings.Volume;
            value = Mathf.Max(0.0001f, value);
            value = Mathf.Log10(value) * 20;
            
            _audioMixer.SetFloat(VolumeMixerKey, value);
        }
    }
}
