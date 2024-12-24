#version 450

#extension VK_EXT_descriptor_indexing : enable // Enable bindless textures
#extension GL_EXT_nonuniform_qualifier : enable // Enable nonuniformEXT 

struct OutData {
    vec4 fragPos;
    vec2 texCoord;
    vec3 normal;
    vec3 tangent;
    vec3 bitangent;
};

layout(location = 0) in OutData inData;

layout(set = 0, binding = 5) uniform sampler2D textures[];

struct Material {
    int albedo; // Texture index
};
        
layout(set = 0, binding = 6) uniform MaterialBlock {
    Material material;
};

layout(location = 0) out vec4 outColor;

void main() {
    // Display uv
    
    vec4 albedo = texture(textures[material.albedo], inData.texCoord);
    outColor = albedo;
}