
StructuredBuffer<int> VerticesValues; // Define buffer in shader
float4 VertexAmount;

static const int ValueBits = 11; // First bits store value as 0f-1f
static const int Values = 1 << ValueBits;
static const int ValueMask = (1 << ValueBits) - 1;
static const float ValueMultiplier = 1.0f / ValueMask;
        
static const int RedBits = 7; // First bits store red color component
static const int GreenBits = 7; // Next bits store green color component
static const int BlueBits = 7; // Next bits store blue color component
static const int RedMask = (1 << RedBits) - 1;
static const int GreenMask = (1 << GreenBits) - 1;
static const int BlueMask = (1 << BlueBits) - 1;

static const int ColorBits = RedBits + GreenBits + BlueBits; // We consider next bits for vertex id, this is used to encode special data
static const int ColorsBitsMask = (1 << ColorBits) - 1;

int GetId(float x, float y, float z)
{
    int xInt = clamp(x, 0.0f, VertexAmount.x - 1);
    int yInt = clamp(y, 0.0f, VertexAmount.y - 1);
    int zInt = clamp(z, 0.0f, VertexAmount.z - 1);
    return (int) (xInt + zInt * VertexAmount.x + yInt * VertexAmount.w);
}

void SampleVertexData_float(float3 position, out float3 color, out float value)
{
    int id = GetId(position.x, position.y, position.z);
    int packedData = VerticesValues[id];
    
    // Unpack value (last 10 bits)
    //value = float(packedData & 0x3FF) * (1.0f / 1023.0f); // Normalize the value to [0, 1]
    value = 0;
    
    // Unpack color (first 22 bits)
    int packedColor = (packedData >> ValueBits) & ColorsBitsMask;

    // Unpack the 7-bit Red, 8-bit Green, and 7-bit Blue channels
    float red   = float((packedColor >> (GreenBits + BlueBits)) & RedMask) / RedMask;  // 7 bits for Red
    float green = float((packedColor >> BlueBits) & GreenMask) / GreenMask;  // 8 bits for Green
    float blue  = float(packedColor & BlueMask) / BlueMask;          // 7 bits for Blue

    // Return the normalized color
    //const float precisionConstant = - 5.0f / 127.0f;
    //color = float3(red + precisionConstant, green + precisionConstant, blue + precisionConstant);
    color = float3(red, green, blue);
}
