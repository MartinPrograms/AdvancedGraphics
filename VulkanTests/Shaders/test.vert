#version 450



struct Vertex {
    vec3 position;
    vec2 texCoord;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

struct OutData {
    vec4 fragPos;
    vec2 texCoord;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

layout (location = 0) out OutData outData;

// 
layout (binding = 0) readonly buffer UBO {
    Vertex vertices[];
} vbo;

layout (binding = 1) readonly buffer IndexBuffer {
    uint indices[];
} ibo;

layout (binding = 3) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo2;

void main() {
    uint index = ibo.indices[gl_VertexIndex];
    
    // Transform the vertex position
    vec4 pos = ubo2.proj * ubo2.view * ubo2.model * vec4(vbo.vertices[index].position, 1.0);
    gl_Position = pos;
    
    outData.fragPos = pos;
    outData.texCoord = vbo.vertices[index].texCoord;
    outData.normal = vbo.vertices[index].normal;
    outData.tangent = vbo.vertices[index].tangent;
    outData.bitangent = vbo.vertices[index].bitangent;
}