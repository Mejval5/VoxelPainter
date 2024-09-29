using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelPainter.ControlsManagement
{
    public class MouseAndKeyboardController : IController
    {
        private static readonly Dictionary<VoxelControlKey, KeyCode[]> KeyMapping = new()
        {
            { VoxelControlKey.PositivePaint, new[] { KeyCode.Mouse0 } },
            { VoxelControlKey.NegativePaint, new[] { KeyCode.Mouse1 } },
            { VoxelControlKey.RotateHeld, new[] { KeyCode.Mouse2 } },
            { VoxelControlKey.AltModifier, new[] { 
                KeyCode.LeftAlt, KeyCode.RightAlt, 
                KeyCode.LeftShift, KeyCode.RightShift, 
                KeyCode.LeftControl, KeyCode.RightControl,
                KeyCode.Space,
            } }
        };
        
        public bool IsKeyDown(VoxelControlKey voxelKey)
        {
            if (KeyMapping.TryGetValue(voxelKey, out KeyCode[] value))
            {
                return value.Any(Input.GetKeyDown);
            }

            Debug.LogError($"No key mapping found for button {voxelKey}");
            return false;
        }
        
        public bool IsKeyUp(VoxelControlKey voxelKey)
        {
            if (KeyMapping.TryGetValue(voxelKey, out KeyCode[] value))
            {
                return value.Any(Input.GetKeyUp);
            }

            Debug.LogError($"No key mapping found for button {voxelKey}");
            return false;
        }

        public bool IsKeyPressed(VoxelControlKey voxelKey)
        {
            if (KeyMapping.TryGetValue(voxelKey, out KeyCode[] value))
            {
                return value.Any(Input.GetKey);
            }

            Debug.LogError($"No key mapping found for button {voxelKey}");
            return false;
        }
    }
}