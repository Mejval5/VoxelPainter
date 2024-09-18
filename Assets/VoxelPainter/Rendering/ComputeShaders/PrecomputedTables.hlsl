
static const uint CornersPerCube = 8u;
static const int EdgesPerCube = 12;
static const uint TriangleConnectionTableWidth = 16;
static const uint TriangleConnectionTableRowSize = 16;

static const uint EdgeConnectionTableRowSize = 2;
static const uint EdgeDirectionTableRowSize = 3;
static const uint CubeCornersPositionsRowSize = 3;

/// <summary>
/// VertexOffset lists the positions, relative to vertex0, 
/// of each of the 8 vertices of a cube.
/// vertexOffset[8][3]
/// </summary>
static const uint CubeCornersPositions[24] =
{
    0, 0, 0,1, 0, 0,1, 1, 0,0, 1, 0,
    0, 0, 1,1, 0, 1,1, 1, 1,0, 1, 1
};

/// <summary>
/// EdgeConnection lists the index of the endpoint vertices for each 
/// of the 12 edges of the cube.
/// edgeConnection[12][2]
/// </summary>
static const uint EdgeConnection[24] =
{
    0,1, 1,2, 2,3, 3,0,
    4,5, 5,6, 6,7, 7,4,
    0,4, 1,5, 2,6, 3,7
};

/// <summary>
/// edgeDirection lists the direction vector (vertex1-vertex0) for each edge in the cube.
/// edgeDirection[12][3]
/// </summary>
static const float EdgeDirection[36] =
{
     1.0f, 0.0f, 0.0f ,  0.0f, 1.0f, 0.0f ,  -1.0f, 0.0f, 0.0f ,  0.0f, -1.0f, 0.0f ,
     1.0f, 0.0f, 0.0f ,  0.0f, 1.0f, 0.0f ,  -1.0f, 0.0f, 0.0f ,  0.0f, -1.0f, 0.0f ,
     0.0f, 0.0f, 1.0f ,  0.0f, 0.0f, 1.0f ,  0.0f, 0.0f, 1.0f ,  0.0f, 0.0f, 1.0f 
};
