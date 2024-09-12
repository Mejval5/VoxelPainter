using System;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [Serializable]
    public class GridProperties
    {
        [field: SerializeField] public Vector3 Origin { get; set; }
        [field: SerializeField] public float Density { get; set; }
        [field: SerializeField] public Vector3 Scale { get; set; }
        
        [field: SerializeField] public float Frequency { get; set; } = 1;
        [field: SerializeField] public float Amplitude { get; set; } = 1;
        [field: SerializeField] public float Persistence { get; set; } = 0.5f;
        [field: SerializeField] public int Octave { get; set; } = 1;
        [field: SerializeField] public int Seed { get; set; } = 0;
    }
}