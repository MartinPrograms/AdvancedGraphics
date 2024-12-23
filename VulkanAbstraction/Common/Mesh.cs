using System.Numerics;
using Common;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Pipelines;

namespace VulkanAbstraction.Common;

public class Vertex
{
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector3 Bitangent;
}

public class Mesh
{
    UniformBuffer _vertexBuffer;
    UniformBuffer _indexBuffer;
    UniformBuffer _uniformBuffer; // mat4 model, mat4 view, mat4 proj
    public uint IndicesCount { get; set; }
    public VaPipeline Pipeline { get; set; }
    
    public Transform Transform { get; set; } = new(new Vector3(0, 0, 0), Quaternion.Identity, new Vector3(1, 1, 1));
    
    public unsafe Mesh(Vertex[] vertices, uint[] indices, string pipelineName)
    {
        IndicesCount = (uint)indices.Length;
        Pipeline = VaPipeline.GetPipeline(pipelineName).Clone(); // Clone the pipeline so we don't modify the original.
        // Every single variable in the mesh aligns to 4 bytes.
        CreateBuffers(vertices, indices);

        FillUniformBuffer();

        Bind(Pipeline.DescriptorSet);
        Logger.Info("Mesh created");
    }

    private unsafe void FillUniformBuffer()
    {
        Vector3 eye = new(0, 0, 2);
        Vector3 center = new(0, 0, 0);
        Vector3 up = new(0, 1, 0);
        Matrix4x4 view = Matrix4x4.CreateLookAt(eye, center, up);
        
        Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(MathUtils.DegToRad(90f), 800.0f / 600, 0.1f, 100f);

        Matrix4x4 model = Transform.GetMatrix();
        Matrix4x4[] uniformData = new[]{model, view, proj};
        
        fixed (float* ptr = &uniformData[0].M11)
        {
            _uniformBuffer.SetData(ptr);
        }
    }

    private unsafe void CreateBuffers(Vertex[] vertices, uint[] indices)
    {
        float[] data = new float[vertices.Length * (5 * 4)];
        for (int i = 0; i < vertices.Length; i++)
        {
            data[i * 20 + 0] = vertices[i].Position.X;
            data[i * 20 + 1] = vertices[i].Position.Y;
            data[i * 20 + 2] = vertices[i].Position.Z;
            data[i * 20 + 3] = 0; // Padding
            data[i * 20 + 4] = vertices[i].TexCoord.X;
            data[i * 20 + 5] = vertices[i].TexCoord.Y;
            data[i * 20 + 6] = 0; // Padding
            data[i * 20 + 7] = 0;
            data[i * 20 + 8] = vertices[i].Normal.X;
            data[i * 20 + 9] = vertices[i].Normal.Y;
            data[i * 20 + 10] = vertices[i].Normal.Z;
            data[i * 20 + 11] = 0; // Padding
            data[i * 20 + 12] = vertices[i].Tangent.X;
            data[i * 20 + 13] = vertices[i].Tangent.Y;
            data[i * 20 + 14] = vertices[i].Tangent.Z;
            data[i * 20 + 14] = 0; // Padding
            data[i * 20 + 15] = vertices[i].Bitangent.X;
            data[i * 20 + 16] = vertices[i].Bitangent.Y;
            data[i * 20 + 17] = vertices[i].Bitangent.Z;
            data[i * 20 + 18] = 0; // Padding
            data[i * 20 + 19] = 0;
        }

        _vertexBuffer = new UniformBuffer((uint)(data.Length * sizeof(float))); 
        _vertexBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
     
        fixed (float* ptr = data)
        {
            _vertexBuffer.SetData(ptr);
        }
        
        _indexBuffer = new UniformBuffer((uint)(indices.Length * sizeof(uint)));
        _indexBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        
        fixed (uint* ptr = indices)
        {
            _indexBuffer.SetData(ptr);
        }
        
        _uniformBuffer = new UniformBuffer(3 * 16 * sizeof(float));
        _uniformBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            BufferUsageFlags.UniformBufferBit | BufferUsageFlags.VertexBufferBit);
    }


    public unsafe void Bind(DescriptorSet set)
    {
        // They're ALWAYS bound to the 0th and 1st binding.
        Device device = VaContext.Current.Device;
        Vk vk = VaContext.Current.Vk;
        
        WriteDescriptorSet[] writes = new WriteDescriptorSet[3];
        
        DescriptorBufferInfo vertexBufferInfo = new()
        {
            Buffer = _vertexBuffer.Buffer,
            Offset = 0,
            Range = _vertexBuffer.Size
        };
        
        DescriptorBufferInfo indexBufferInfo = new()
        {
            Buffer = _indexBuffer.Buffer,
            Offset = 0,
            Range = _indexBuffer.Size
        };
        
        writes[0] = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &vertexBufferInfo
        };
        
        writes[1] = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 1,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &indexBufferInfo
        };
        
        DescriptorBufferInfo uniformBufferInfo = new()
        {
            Buffer = _uniformBuffer.Buffer,
            Offset = 0,
            Range = _uniformBuffer.Size
        };
        
        writes[2] = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 3,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            PBufferInfo = &uniformBufferInfo
        };
        
        vk.UpdateDescriptorSets(device, (uint)writes.Length, writes, 0, (CopyDescriptorSet*)null);
        Logger.Info("Bound mesh buffers to descriptor set");
    }

    public void Use(DescriptorSet set)
    {
        Bind(set);
    }

    public unsafe void Draw(CommandBuffer contextCommandBuffer)
    {
        Vk vk = VaContext.Current.Vk;
        FillUniformBuffer();
        
        var pipeline = Pipeline;
        vk.CmdBindPipeline(contextCommandBuffer, PipelineBindPoint.Graphics, pipeline.Pipeline);
        
        var set = pipeline.DescriptorSet;
        vk.CmdBindDescriptorSets(contextCommandBuffer, PipelineBindPoint.Graphics, pipeline.PipelineLayout, 0, 1, &set, 0, null);
        
        
        vk.CmdDraw(contextCommandBuffer, IndicesCount, 1, 0, 0);
    }
}