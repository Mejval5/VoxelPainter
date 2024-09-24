using Foxworks.Voxels;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Video;
using VoxelPainter.GridManagement;
using VoxelPainter.Rendering.Utils;

namespace VoxelPainter.VoxelVisualization
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
        
        [SerializeField] private int _downSampleFactor = 2;
        [SerializeField] private float _depthMultiplier = 1f;
        
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
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
            _videoFrameTexture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGBA32, true);

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
            _videoFrameTexture.Apply(true);

            RenderTexture.active = currentRT;
            
            GenerateMesh();
        }
        
        public override void GetVertexValues(NativeArray<int> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;
            
            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3 position = MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                Profiler.BeginSample("GetGridValue");
                // Ensure the texture is available
                if (_videoFrameTexture == null)
                {
                    verticesValues[i] = VoxelDataUtils.PackValueAndVertexId(0f, 0);
                    continue;
                }

                // Get grid dimensions
                int gridWidth = VertexAmountX;
                int gridHeight = VertexAmountY;
                int gridDepth = VertexAmountZ;

                // Get video frame dimensions
                int videoWidth = _videoFrameTexture.width;
                int videoHeight = _videoFrameTexture.height;

                // Decide scaling based on the smaller dimension (fit height, crop sides)
                float scale = (float)videoHeight / gridHeight;

                // Compute the video width in grid units
                float videoWidthInGridUnits = videoWidth / scale;

                // Compute horizontal offset to center the video
                float xOffset = (gridWidth - videoWidthInGridUnits) / 2f;

                // Map grid position to video frame coordinates
                float xRelativeToVideo = (position.x - xOffset) * scale;
                float yRelativeToVideo = position.y * scale;

                float finalValue = _invertOutput ? 1f : 0f;
                
                // Check if the position is within the video frame bounds
                if (xRelativeToVideo >= 0 && xRelativeToVideo < videoWidth && yRelativeToVideo >= 0 && yRelativeToVideo < videoHeight)
                {
                    float value = SameplMipMaps(xRelativeToVideo, videoWidth, yRelativeToVideo, videoHeight);

                    float depth = value * gridDepth * _depthMultiplier;

                    if (gridDepth - position.z <= depth)
                    {
                        finalValue = _invertOutput ? 1f - value : value;
                    }
                    else
                    {
                        finalValue = _invertOutput ? 1f : 0f;
                    }
                }
                
                Profiler.EndSample();
                verticesValues[i] = VoxelDataUtils.PackValueAndVertexId(finalValue);
            }
        }

        private float SameplMipMaps(float xRelativeToVideo, int videoWidth, float yRelativeToVideo, int videoHeight)
        {
            Profiler.BeginSample("SampleMipMaps");
            // Adjust coordinates for mipmap level
            // Normalize coordinates
            float xNormalized = xRelativeToVideo / videoWidth;
            float yNormalized = yRelativeToVideo / videoHeight;
            
            float value = 0f;
            for (int i = 0; i < _downSampleFactor; i++)
            {
                int mipWidth = _videoFrameTexture.width >> i;
                int mipHeight = _videoFrameTexture.height >> i;
                
                int pixelXm = Mathf.Clamp((int)(xNormalized * mipWidth), 0, mipWidth - 1);
                int pixelYm = Mathf.Clamp((int)(yNormalized * mipHeight), 0, mipHeight - 1);

                // Get the pixel color from the specified mipmap level
                Profiler.BeginSample("GetPixel");
                Color pixelColorMipMap = _videoFrameTexture.GetPixel(pixelXm, pixelYm, i);
                Profiler.EndSample();

                // Convert color to grayscale (or use one of the color channels)
                value += pixelColorMipMap.grayscale;
            }

            Profiler.EndSample();
            return value;
        }
    }
}