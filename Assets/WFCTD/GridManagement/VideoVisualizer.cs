using System;
using UnityEngine;
using UnityEngine.Video;

namespace WFCTD.GridManagement
{
    [ExecuteAlways]
    public class VideoVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private VideoPlayer _videoPlayer;

        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private Texture2D _videoFrameTexture;
        [SerializeField] private int _videoFrameIndex;
        [SerializeField] private bool _restartVideo;

        [SerializeField] private bool _invertOutput = false;
        
        
        private void OnEnable()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
            }
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;

            VideoClip clip = _videoPlayer.clip;
            _renderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            _videoPlayer.targetTexture = _renderTexture;
                
            // Initialize Texture2D
            _videoFrameTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, false);

            // Start playing the video
            _videoPlayer.Play();
        }
        
        private void Update()
        {
            if (_restartVideo)
            {
                _videoPlayer.Stop();
                _videoPlayer.Play();
                _restartVideo = false;
            }
            
            // Update the video frame index
            _videoFrameIndex = (int)_videoPlayer.frame;

            // Only update the texture if the frame has changed
            if (!_videoPlayer.isPrepared || _videoPlayer.frame == (long) _videoPlayer.frameCount)
            {
                return;
            }

            // Read pixels from the RenderTexture
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;

            _videoFrameTexture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
            _videoFrameTexture.Apply();

            RenderTexture.active = currentRT;
            
            GenerateMesh();
        }

        public override float GetGridValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            // Ensure the texture is available
            if (_videoFrameTexture == null)
            {
                return 0f;
            }

            // Get grid dimensions
            int gridWidth = VertexAmountX;
            int gridHeight = VertexAmountY;

            // Get video frame dimensions
            int videoWidth = _videoFrameTexture.width;
            int videoHeight = _videoFrameTexture.height;

            // Compute aspect ratios
            float gridAspectRatio = (float)gridWidth / gridHeight;
            float videoAspectRatio = (float)videoWidth / videoHeight;

            // Decide scaling based on the smaller dimension (fit height, crop sides)
            float scale = (float)videoHeight / gridHeight;

            // Compute the video width in grid units
            float videoWidthInGridUnits = videoWidth / scale;

            // Compute horizontal offset to center the video
            float xOffset = (gridWidth - videoWidthInGridUnits) / 2f;

            // Map grid position to video frame coordinates
            float xRelativeToVideo = (position.x - xOffset) * scale;
            float yRelativeToVideo = position.y * scale;

            // Check if the position is within the video frame bounds
            if (xRelativeToVideo >= 0 && xRelativeToVideo < videoWidth && yRelativeToVideo >= 0 && yRelativeToVideo < videoHeight)
            {
                int pixelX = Mathf.Clamp((int)xRelativeToVideo, 0, videoWidth - 1);
                int pixelY = Mathf.Clamp((int)yRelativeToVideo, 0, videoHeight - 1);

                // Get the pixel color
                Color pixelColor = _videoFrameTexture.GetPixel(pixelX, pixelY);

                // Convert color to grayscale (or use one of the color channels)
                float value = pixelColor.grayscale;

                return _invertOutput ? 1f - value : value;
            }
            else
            {
                // Outside the video frame, return zero or a default value
                return 0f;
            }
        }
    }
}