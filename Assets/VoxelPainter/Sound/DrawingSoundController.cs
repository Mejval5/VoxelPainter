using System;
using Foxworks.Utils;
using UnityEngine;
using CursorMode = VoxelPainter.Rendering.CursorMode;

namespace VoxelPainter.Sound
{
    public class DrawingSoundController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioSource _drawAudioSource;
        [SerializeField] private AudioClip _drawAudioClip;
        [SerializeField]  private AudioClip _deleteDrawAudioClip;
        [SerializeField] private Rendering.VoxelPainter _voxelPainter;

        [Header("Settings")]
        [SerializeField] private float _drawAudioVolume = 0.5f;
        [SerializeField] private Vector2 _drawAudioPitchRange = new(0.9f, 1.1f);
        [SerializeField] private Vector2 _drawSpeedRange = new(0f, 50f);
        [SerializeField] private float _drawAudioPitchChangeSpeed = 0.1f;
        [SerializeField] private float _drawAudioRampUpTime = 0.1f;
        private float _targetVolume = 0f;
        private float _perlinNoiseOffset = 0f;

        private void Awake()
        {
            _drawAudioSource.volume = 0;
            _drawAudioSource.pitch = (_drawAudioPitchRange.x + _drawAudioPitchRange.y) / 2;
            _drawAudioSource.loop = true;
        }

        private void Update()
        {
            HandleDrawingSound();
        }

        private void HandleDrawingSound()
        {
            // Check the current CursorMode from the voxel painter
            switch (_voxelPainter.CurrentCursorMode)
            {
                case CursorMode.Draw:
                    // When drawing, we want to ramp up the volume and adjust the pitch using Perlin noise
                    if (_drawAudioSource.clip != _drawAudioClip)
                    {
                        _drawAudioSource.clip = _drawAudioClip;
                        _drawAudioSource.Play();
                    }
                    
                    _targetVolume = _drawAudioVolume;
                    AdjustPitchWithPerlinNoise();
                    break;

                case CursorMode.Erase:
                    // We can also add sound for erasing if desired, or just lower the volume
                    if (_drawAudioSource.clip != _deleteDrawAudioClip)
                    {
                        _drawAudioSource.clip = _deleteDrawAudioClip;
                        _drawAudioSource.Play();
                    }
                    
                    _targetVolume = _drawAudioVolume;
                    AdjustPitchWithPerlinNoise();
                    break;

                case CursorMode.None:
                case CursorMode.ChangeSizeMode:
                default:
                    // For other modes or None, lower the volume to zero
                    _targetVolume = 0f;
                    break;
            }

            // Gradually adjust the volume based on the target volume
            _drawAudioSource.volume = Mathf.MoveTowards(_drawAudioSource.volume, _targetVolume, Time.deltaTime / _drawAudioRampUpTime);
        }

        private void AdjustPitchWithPerlinNoise()
        {
            // Move the perlin noise offset to keep the noise changing smoothly over time
            _perlinNoiseOffset += _drawAudioPitchChangeSpeed * Time.deltaTime;

            // Get a Perlin noise value that ranges from 0 to 1
            float noiseValue = Mathf.PerlinNoise(_perlinNoiseOffset, 0f);

            float speedRate = Mathf.InverseLerp(_drawSpeedRange.x, _drawSpeedRange.y, _voxelPainter.CurrentCursorSpeed);

            // Map the Perlin noise value to the specified pitch range
            float targetPitch = Mathf.Lerp(_drawAudioPitchRange.x, _drawAudioPitchRange.y, speedRate * noiseValue);

            // Smoothly adjust the pitch of the audio source
            _drawAudioSource.pitch = targetPitch;
        }
    }
}