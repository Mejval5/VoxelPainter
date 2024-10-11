using System;
using Foxworks.Utils;
using UnityEngine;
using CursorMode = VoxelPainter.Rendering.CursorMode;

namespace VoxelPainter.Sound
{
    public class PhysicsSoundController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _physicsAudioClip;
        [SerializeField] private Rendering.DrawingVisualizer _drawingVisualizer;

        [Header("Settings")]
        [SerializeField] private float _baseAudioVolume = 0.5f;
        [SerializeField] private Vector2 _audioPitchRange = new(0.9f, 1.1f);
        [SerializeField] private Vector2 _voxelChangeRange = new(0f, 50f);
        [SerializeField] private float _audioPitchChangeSpeed = 0.1f;
        [SerializeField] private float _audioRampUpTime = 0.1f;
        
        private float _targetVolume = 0f;
        private float _perlinNoiseOffset = 0f;

        private void Awake()
        {
            _audioSource.volume = 0;
            _audioSource.pitch = (_audioPitchRange.x + _audioPitchRange.y) / 2;
            _audioSource.loop = true;
            
            _audioSource.clip = _physicsAudioClip;
            _audioSource.Play();
        }

        private void Update()
        {
            HandleDrawingSound();
        }

        private void HandleDrawingSound()
        {
            int movedVoxels = _drawingVisualizer.MovedVoxelsThisFrame;
            
            float speedRate = Mathf.InverseLerp(_voxelChangeRange.x, _voxelChangeRange.y, _drawingVisualizer.MovedVoxelsThisFrame);
            
            if (movedVoxels <= 0)
            {
                _targetVolume = 0f;
            }
            else
            {
                _targetVolume = _baseAudioVolume * speedRate;
                AdjustPitchWithPerlinNoise();
            }

            // Gradually adjust the volume based on the target volume
            _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, _targetVolume, Time.deltaTime / _audioRampUpTime);
        }

        private void AdjustPitchWithPerlinNoise()
        {
            // Move the perlin noise offset to keep the noise changing smoothly over time
            _perlinNoiseOffset += _audioPitchChangeSpeed * Time.deltaTime;

            // Get a Perlin noise value that ranges from 0 to 1
            float noiseValue = Mathf.PerlinNoise(_perlinNoiseOffset, 0f);

            // Map the Perlin noise value to the specified pitch range
            float targetPitch = Mathf.Lerp(_audioPitchRange.x, _audioPitchRange.y, noiseValue);

            // Smoothly adjust the pitch of the audio source
            _audioSource.pitch = targetPitch;
        }
    }
}