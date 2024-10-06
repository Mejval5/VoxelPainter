
StructuredBuffer<int> VerticesValues; // Define buffer in shader
float4 VertexAmount;

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
    value = float(packedData & 0x3FF) * (1.0f / 1023.0f); // Normalize the value to [0, 1]

    // Unpack color (first 22 bits)
    int packedColor = (packedData >> 10) & 0x3FFFFF;

    // Unpack the 7-bit Red, 8-bit Green, and 7-bit Blue channels
    float red   = float((packedColor >> 15) & 0x7F) / 127.0f;  // 7 bits for Red
    float green = float((packedColor >> 7)  & 0xFF) / 255.0f;  // 8 bits for Green
    float blue  = float(packedColor & 0x7F) / 127.0f;          // 7 bits for Blue

    // Return the normalized color
    color = float3(red, green, blue);
}
