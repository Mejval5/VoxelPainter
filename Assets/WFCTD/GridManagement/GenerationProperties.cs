using System;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [Serializable]
    public class GenerationProperties
    {
        [field: SerializeField] public Vector3 Origin { get; set; }
        [field: Range(1f, 500f)]
        [field: SerializeField] public float Frequency { get; set; } = 50;
    }
}