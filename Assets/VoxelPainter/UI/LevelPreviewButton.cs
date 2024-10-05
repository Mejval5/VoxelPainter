using System;
using Foxworks.Components.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    public class LevelPreviewButton : MonoBehaviour
    {
        [SerializeField] private JuicyButton _button;
        [SerializeField] private GameObject _selectionBox;
        
        [SerializeField] private RawImage _rawImage;
        
        public RawImage RawImage => _rawImage;
        public JuicyButton JuicyButton => _button;
        
        [HideInInspector] public UnityEvent Clicked = new ();
        
        protected void Awake()
        {
            _button.Clicked.AddListener(() => Clicked.Invoke());
            
            _selectionBox.SetActive(false);
        }
        
        public void SetSelected(bool selected)
        {
            _selectionBox.SetActive(selected);
        }
    }
}