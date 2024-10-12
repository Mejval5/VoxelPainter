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
            {
                VoxelControlKey.AltModifier, new[]
                {
                    KeyCode.LeftAlt, KeyCode.RightAlt,
                    KeyCode.LeftShift, KeyCode.RightShift,
                    KeyCode.LeftControl, KeyCode.RightControl,
                    KeyCode.Space
                }
            }
        };

        private static readonly Dictionary<VoxelControlKey, KeyCode[][]> KeyMappingWithSingleModifier = new()
        {
            { VoxelControlKey.Undo, new[] { new[] { KeyCode.LeftControl, KeyCode.Z }, new[] { KeyCode.RightControl, KeyCode.Z } } },
            { VoxelControlKey.Redo, new[] { new[] { KeyCode.LeftControl, KeyCode.Y }, new[] { KeyCode.RightControl, KeyCode.Y } } }
        };

        public bool IsKeyDown(VoxelControlKey voxelKey)
        {
            if (KeyMapping.TryGetValue(voxelKey, out KeyCode[] value))
            {
                return value.Any(Input.GetKeyDown);
            }

            if (KeyMappingWithSingleModifier.TryGetValue(voxelKey, out KeyCode[][] keyCombos))
            {
                foreach (KeyCode[] keyCombo in keyCombos)
                {
                    if (Input.GetKey(keyCombo[0]) && Input.GetKeyDown(keyCombo[1]))
                    {
                        return true;
                    }
                }

                return false;
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

            if (KeyMappingWithSingleModifier.TryGetValue(voxelKey, out KeyCode[][] keyCombos))
            {
                foreach (KeyCode[] keyCombo in keyCombos)
                {
                    if (Input.GetKeyUp(keyCombo[0]) || Input.GetKeyUp(keyCombo[1]))
                    {
                        return true;
                    }
                }

                return false;
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

            if (KeyMappingWithSingleModifier.TryGetValue(voxelKey, out KeyCode[][] keyCombos))
            {
                foreach (KeyCode[] keyCombo in keyCombos)
                {
                    if (Input.GetKey(keyCombo[0]) && Input.GetKey(keyCombo[1]))
                    {
                        return true;
                    }
                }

                return false;
            }

            Debug.LogError($"No key mapping found for button {voxelKey}");
            return false;
        }
    }
}