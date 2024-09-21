# VoxelPainter

VoxelPainter is a Unity-based tool for creating and visualizing voxel-based graphics. This project leverages advanced rendering techniques such as Marching Cubes, Simplex Noise, and custom shaders to provide an interactive 3D voxel painting experience. 

### Demos
https://www.youtube.com/watch?v=owe9IhLvLaY
https://www.youtube.com/watch?v=6uRV_go0Ygc

### Early demo
https://www.youtube.com/watch?v=RvF3tPzCbck

## Features

- **Voxel-based Rendering**: Create and render 3D voxel models using both CPU and GPU-based visualizers.
- **Marching Cubes Algorithm**: Visualize voxel data with smooth surfaces using the Marching Cubes technique.
- **Simplex Noise**: Generate procedurally textured terrains and grids with Simplex Noise.
- **Custom Shaders**: Fine-tune visual appearances with a variety of shaders, including terrain, water, wireframe, and skybox effects.
- **Interactive Voxel Painting**: Paint directly onto voxel objects using a variety of materials and textures.
- **High-Quality Materials and Textures**: Supports a wide range of materials, from basic terrain to sci-fi inspired designs.
- **Custom Prefabs and Scenes**: Includes pre-built scenes and prefabs for rapid development and experimentation.
- **Visualizer Modules**: Modular visualizer scripts that offer flexibility in rendering voxel data.
  
## Project Structure

The project is organized into the following key directories:

- **Assets/Materials**: Contains various materials for voxel rendering, including specific textures for terrains, water, and objects.
- **Assets/Prefabs**: Contains pre-configured prefabs such as `ClickableMesh`.
- **Assets/Scenes**: Includes two primary scenes, `MainVisualizersScene` for rendering visualizations and `PrototypeLevelScene` for prototyping voxel structures.
- **Assets/Textures**: A wide variety of textures for water, stone, tiles, and sci-fi elements to enhance the voxel models.
- **Assets/VoxelPainter**: The core of the project, with subdirectories for generation and rendering, housing essential scripts and shaders.

## How to Use

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/yourusername/VoxelPainter.git
   ```

2. **Open in Unity**: Open the project in Unity 2022.3.15f1 (other versions might work fine as well) + URP

3. **Explore the Scenes**: 
   - Open `MainVisualizersScene` to see the voxel painting and visualization in action.
   - Use `PrototypeLevelScene` to experiment with voxel structures and designs.

4. **Customize Your Voxel World**: 
   - Modify the materials, textures, and shaders in the `Assets/Materials` and `Assets/Textures` folders to create your own visual style.
   - Play with the visualizer scripts under `Assets/VoxelPainter/Rendering` to change the rendering techniques.

## Technologies Used

- **Unity Universal Render Pipeline (URP)**: For high-performance, flexible rendering.
- **Marching Cubes Algorithm**: For creating smooth voxel surfaces.
- **Simplex Noise**: For procedurally generating complex terrains.
- **Custom Shader Graphs**: Extensive use of Unity Shader Graph for custom visual effects.
  
## Contribution

Contributions are welcome! Feel free to open issues or submit pull requests for bug fixes or new features.

## License

This project is licensed under the MIT License.
