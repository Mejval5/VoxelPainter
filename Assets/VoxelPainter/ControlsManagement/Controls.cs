
namespace VoxelPainter.ControlsManagement
{
    public enum VoxelControlKey
    {
        PositivePaint,
        NegativePaint,
        RotateHeld,
        AltModifier
    }

    public static class Controls
    {
        static Controls()
        {
            CurrentController = new MouseAndKeyboardController();
        }
        
        private static IController CurrentController { get; set; }

        public static void SetController(IController controller)
        {
            CurrentController = controller;
        }
        
        public static bool IsKeyDown(VoxelControlKey voxelKey)
        {
            return CurrentController.IsKeyDown(voxelKey);
        }
        
        public static bool IsKeyUp(VoxelControlKey voxelKey)
        {
            return CurrentController.IsKeyUp(voxelKey);
        }

        public static bool IsKeyPressed(VoxelControlKey voxelKey)
        {
            return CurrentController.IsKeyPressed(voxelKey);
        }
    }

    public interface IController
    {
        bool IsKeyPressed(VoxelControlKey voxelKey);
        bool IsKeyDown(VoxelControlKey voxelKey);
        bool IsKeyUp(VoxelControlKey voxelKey);
    }
}