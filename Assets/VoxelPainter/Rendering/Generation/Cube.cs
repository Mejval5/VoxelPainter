using System;
using UnityEngine;

namespace VoxelPainter.GridManagement
{
    [Serializable]
    public class Cube
    {
        [field: SerializeField] public GridPoint[] Corners { get; set; }
    }
}