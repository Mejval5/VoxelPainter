using System;
using UnityEngine;
using UnityEngine.Events;

namespace WFCTD.GridManagement.UserInteraction
{
    public class ClickableMesh : MonoBehaviour
    {
        public UnityEvent Clicked { get; } = new();
        
        private void OnMouseDown()
        {
            Clicked.Invoke();
        }
    }
}