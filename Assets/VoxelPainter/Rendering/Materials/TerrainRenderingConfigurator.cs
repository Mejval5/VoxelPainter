using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPainter.GridManagement
{
    [ExecuteAlways]
    public class TerrainRenderingConfigurator : MonoBehaviour
    {
        [SerializeField] private int _amountOfColors;
        [SerializeField] private List<Color> _colors;


        private void OnValidate()
        {
            if (_amountOfColors < 1)
            {
                _amountOfColors = 1;
            }

            if (_colors == null)
            {
                _colors = new List<Color>();
            }
            
            if (_colors.Count < _amountOfColors)
            {
                for (int i = _colors.Count; i < _amountOfColors; i++)
                {
                    _colors.Add(Color.white);
                }
            }
            else if (_colors.Count > _amountOfColors)
            {
                _colors.RemoveRange(_amountOfColors, _colors.Count - _amountOfColors);
            }
            

        }
        //
        // private void GenerateTerrainTexture()
        // {
        //     Texture2D texture = new Texture2D(
        //     Color[] colors = new Color[256 * 256];
        //     for (int i = 0; i < 256; i++)
        //     {
        //         for (int j = 0; j < 256; j++)
        //         {
        //             colors[i * 256 + j] = _colors[UnityEngine.Random.Range(0, _amountOfColors)];
        //         }
        //     }
        //
        //     texture.SetPixels(colors);
        //     texture.Apply();
        // }
    }
}