#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_EXT_scalar_block_layout : enable

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

layout (std140, binding = 3) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
    uint IndexOffset; 
    uint ArrayOffset;
} ubo;

void main() {
    uint index = ibo.indices[gl_VertexIndex + ubo.IndexOffset]; // Get the index from the index buffer
    index += ubo.ArrayOffset; // Add the offset to the index
    
    // Transform the vertex position
    vec4 pos = ubo.proj * ubo.view * ubo.model * vec4(vbo.vertices[index].position, 1.0);
    gl_Position = pos;
    
    outData.fragPos = pos;
    outData.texCoord = vbo.vertices[index].texCoord;
    outData.normal = vbo.vertices[index].normal;
    outData.tangent = vbo.vertices[index].tangent;
    outData.bitangent = vbo.vertices[index].bitangent;
}