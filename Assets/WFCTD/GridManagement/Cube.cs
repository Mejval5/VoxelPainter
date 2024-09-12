using System;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [Serializable]
    public class Cube
    {
        [field: SerializeField] public GridPoint[] Corners { get; set; }
    }
}