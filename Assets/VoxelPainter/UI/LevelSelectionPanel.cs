using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foxworks.Persistence;
using Foxworks.Utils;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    public class LevelSelectionPanel : MonoBehaviour
    {
        private const float MinLoadingTime = 0.0f;
        
        [Header("UI References")]
        [SerializeField] private Button _bgCloseButton;
        [SerializeField] private Button _closeButton;
        
        [Header("UI Elements")]
        [SerializeField] private GameObject _scrollView;
        [SerializeField] private GameObject _loadingCircle;
        [SerializeField] private GameObject _noDataText;
        [SerializeField] private Transform _contentHolder;
        
        [Header("Dependencies")]
        [SerializeField] private LevelPreviewButton _levelPreviewButtonPrefab;
        [SerializeField] private DrawingVisualizer _drawingVisualizer;
        
        private readonly ConcurrentBag<PaintingPreviewData> _paintingPreviewData = new ();

        private bool _showNextFrame = false;
        private readonly Stopwatch _loadingStopwatch = new ();
        
        private CancellationTokenSource _cancellationTokenSource;
        
        private Dictionary<string, LevelPreviewButton> _levelPreviewButtons = new ();

        protected void Awake()
        {
            _bgCloseButton.onClick.AddListener(Hide);
            _closeButton.onClick.AddListener(Hide);
        }
        
        protected async void OnEnable()
        {
            UpdateView(show: false);
            
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Setup(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }

        private void UpdateView(bool show)
        {
            if (show == false)
            {
                _scrollView.SetActive(false);
                _loadingCircle.SetActive(true);
                _noDataText.SetActive(false);
                return;
            }
            
            _levelPreviewButtons.Clear();
            _contentHolder.DestroyAllChildren();
            
            _loadingCircle.SetActive(false);
            _scrollView.SetActive(true);

            bool noData = _paintingPreviewData.Count == 0;
            _noDataText.SetActive(noData);
            
            if (noData)
            {
                return;
            }
            
            foreach (PaintingPreviewData previewData in _paintingPreviewData)
            {
                LevelPreviewButton levelPreviewButton = Instantiate(_levelPreviewButtonPrefab, _contentHolder);
                
                Vector2Int size = previewData.PreviewMetaData.PreviewTextureSize;
                byte[] data = previewData.ImageData;
                Texture2D texture = new (size.x, size.y);
                texture.LoadImage(data);
                levelPreviewButton.RawImage.texture = texture;
                
                levelPreviewButton.Clicked.AddListener(() => LoadLevel(previewData.SaveName));
                levelPreviewButton.DeleteClicked.AddListener(() => DeleteLevel(previewData.SaveName));
                levelPreviewButton.SetSelected(previewData.SaveName == _drawingVisualizer.CurrentSaveName);
                levelPreviewButton.JuicyButton.Interactable = previewData.SaveName != _drawingVisualizer.CurrentSaveName;
                levelPreviewButton.JuicyButton.DisableAlpha = 1f;
                
                _levelPreviewButtons[previewData.SaveName] = levelPreviewButton;
            }
        }

        private void DeleteLevel(string levelName)
        {
            LevelPreviewButton levelPreviewButton = _levelPreviewButtons[levelName];
            
            _drawingVisualizer.DeletePainting(levelName);
            Destroy(levelPreviewButton.gameObject);
            
            _levelPreviewButtons.Remove(levelName);
        }

        private void LoadLevel(string saveName)
        {
            _drawingVisualizer.Load(saveName);
            _drawingVisualizer.GenerateMesh();
            
            Hide();
        }

        protected void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        protected void Update()
        {
            if (_showNextFrame == false)
            {
                return;
            }

            UpdateView(show: true);
            _showNextFrame = false;
        }
        
        private async Task Setup(CancellationToken cancellationToken)
        {
            _showNextFrame = false;
            _paintingPreviewData.Clear();
            _loadingStopwatch.Restart();
            
            IEnumerable<Task> tasks = _drawingVisualizer.PaintingSaveHistoryData.SaveNames.Select(saveName => GetData(saveName, cancellationToken));

            await Task.WhenAll(tasks);
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            int millisecondsToWait = (int)(MinLoadingTime * 1000 - _loadingStopwatch.ElapsedMilliseconds);

            if (millisecondsToWait > 0)
            {
                await Task.Delay(millisecondsToWait, cancellationToken);
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            _showNextFrame = true;
        }

        private async Task GetData(string saveName, CancellationToken cancellationToken)
        {
            string savePath = saveName + PaintingPreviewData.SaveAppendKey;
            PaintingPreviewData savePreview = new()
            {
                SaveName = saveName,
                ImageData = await SaveManager.LoadAsync<byte[]>(savePath, PaintingPreviewData.ImageExtension, cancellationToken),
                PreviewMetaData = await SaveManager.LoadAsync<PaintingPreviewMetaData>(savePath, PaintingPreviewData.MetaDataExtension, cancellationToken)
            };
            
            _paintingPreviewData.Add(savePreview);
        }
    }
}