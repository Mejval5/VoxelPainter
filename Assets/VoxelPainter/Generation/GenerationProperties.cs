using System;
using UnityEngine;

namespace VoxelPainter.GridManagement
{
    [Serializable]
    public class GenerationProperties
    {
        [field: SerializeField] public Vector3 Origin { get; set; }
        [field: Range(0f, 500f)]
        [field: SerializeField] public float Frequency { get; set; } = 50;
    }
}