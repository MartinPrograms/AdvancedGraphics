#version 450

struct OutData {
    vec4 fragPos;
    vec2 texCoord;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

layout(location = 0) in OutData inData;

layout(location = 0) out vec4 outColor;

void main() {
    // Display uv
    
    outColor = vec4(inData.texCoord, 0.0, 1.0);
}