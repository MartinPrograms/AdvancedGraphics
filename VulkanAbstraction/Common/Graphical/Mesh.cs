using System.Numerics;
using System.Runtime.InteropServices;
using Common;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Other;
using VulkanAbstraction.Common.Other.Vulkan;
using VulkanAbstraction.Globals;
using VulkanAbstraction.Helpers.Vulkan.Other;
using VulkanAbstraction.Pipelines;

namespace VulkanAbstraction.Common.Graphical;

public class Vertex
{
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector3 Bitangent;

    public static int SizeInBytes => 5 * 4 * sizeof(float);

    public static float[] GetVertexData(Vertex[] vertices)
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
        
        return data;
    }
}

public class MeshOffset
{
    public uint VertexOffset;
    public uint IndexOffset; // IndexOffset
    public uint VertexCount;
    public uint IndexCount;
}

public class Mesh : IUpdatable
{
    private static UniformBuffer GlobalVertexUniformBuffer; // Shared Uniform Buffer for all meshes
    private static UniformBuffer GlobalFragmentUniformBuffer; // Shared Uniform Buffer for all meshes
    private static uint globalVertexUniformOffset = 0; // Offset in the global uniform buffer for the next mesh
    private static uint GlobalVertexUniformItemSize = 0; // Total size of a single item in the global uniform buffer 
    private static uint GlobalFragmentUniformItemSize = 0; // Total size of a single item in the global uniform buffer
    private static uint globalFragmentUniformOffset = 0; // Offset in the global uniform buffer for the next mesh
    private uint vertexUniformOffset = 0; // Offset in the global uniform buffer for this mesh
    private uint fragmentUniformOffset = 0; // Offset in the global uniform buffer for this mesh
    
    public VaPipeline Pipeline { get; set; }
    private string _meshName;
    private string _materialName;
    private MeshOffset _offset;
    
    public Transform Transform { get; set; } = new(new Vector3(0, 0, 0), Quaternion.Identity, new Vector3(1, 1, 1));
    
    public static void InitializeGlobalUniformBuffer(uint vertexSize, uint totalSize)
    {
        GlobalVertexUniformBuffer = new UniformBuffer(vertexSize);
        GlobalVertexUniformBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferDstBit);
        
        GlobalFragmentUniformBuffer = new UniformBuffer(totalSize);
        GlobalFragmentUniformBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            BufferUsageFlags.UniformBufferBit | BufferUsageFlags.TransferDstBit);
    }
    
    private static bool _initialized = false;
    
    public unsafe Mesh(string meshName,string pipelineName,string materialName)
    {
        Pipeline = VaPipeline.GetPipeline(pipelineName); // Clone the pipeline so we don't modify the original.
        _meshName = meshName;
        _materialName = materialName;
        _offset = MeshManager.MeshOffsets[meshName];
    
        if (!_initialized)
        {
            GlobalVertexUniformItemSize = LayoutHelper.GetDynamicAlignment((uint)Marshal.SizeOf<UniformBufferObject>());
            GlobalFragmentUniformItemSize = LayoutHelper.GetDynamicAlignment((uint)Marshal.SizeOf<MaterialManager.Material>());
            InitializeGlobalUniformBuffer(1000000, 1000000);
            _initialized = true;
        }
        
        
        FillUniformBuffer();
        Bind(Pipeline.DescriptorSet.VulkanSet);

        vertexUniformOffset = globalVertexUniformOffset;
        globalVertexUniformOffset += GlobalVertexUniformItemSize;
        
        fragmentUniformOffset = globalFragmentUniformOffset;
        globalFragmentUniformOffset += GlobalFragmentUniformItemSize;

        
        //Bind(Pipeline.DescriptorSet);
        Logger.Info("Mesh created");
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Vulkan Struct")]
    struct UniformBufferObject
    {
        public Matrix4x4 Model;
        public Matrix4x4 View;
        public Matrix4x4 Proj;
        public uint IndexOffset;
        public uint ArrayOffset;
    }
    
    private unsafe void FillUniformBuffer()
    {
        Matrix4x4 model = Transform.GetMatrix();
        Matrix4x4 view = MeshManager.CurrentViewMatrix;
        Matrix4x4 proj = MeshManager.CurrentProjectionMatrix;
        uint offset = _offset.IndexOffset;
        
        UniformBufferObject ubo = new()
        {
            Model = model,
            View = view,
            Proj = proj,
            IndexOffset = offset, // The amount of indices that were before this mesh
            ArrayOffset = _offset.VertexOffset // The amount of vertices that were before this mesh
        };
        
        GlobalVertexUniformBuffer.UpdateRange(&ubo, vertexUniformOffset, GlobalVertexUniformItemSize);
        
        MaterialManager.Material material = MaterialManager.GetMaterial(_materialName);
        GlobalFragmentUniformBuffer.UpdateRange(&material, fragmentUniformOffset, GlobalFragmentUniformItemSize);
    }

    public unsafe void Bind(DescriptorSet set)
    {
        Device device = VaContext.Current!.Device;
        Vk vk = VaContext.Current.Vk;

        DescriptorBufferInfo vertexInfo = new()
        {
            Buffer = GlobalVertexUniformBuffer.Buffer,
            Offset = vertexUniformOffset,
            Range = GlobalVertexUniformItemSize
        };

        WriteDescriptorSet vertexWrite = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 3, // Uniform binding index
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            PBufferInfo = &vertexInfo
        };
        
        DescriptorBufferInfo fragmentInfo = new()
        {
            Buffer = GlobalFragmentUniformBuffer.Buffer,
            Offset = fragmentUniformOffset,
            Range = GlobalFragmentUniformItemSize
        };
        
        WriteDescriptorSet fragmentWrite = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 6, // Uniform binding index
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            PBufferInfo = &fragmentInfo
        };

        WriteDescriptorSet[] uniformWrites = new WriteDescriptorSet[2] { vertexWrite, fragmentWrite };
        
        fixed (WriteDescriptorSet* a = &uniformWrites[0])
        {
            vk.UpdateDescriptorSets(device, 2, a, 0, null);
        }
    }

    public void Use(DescriptorSet set)
    {
        Bind(set);
    }

    public unsafe void Draw(CommandBuffer contextCommandBuffer)
    {
        Vk vk = VaContext.Current.Vk;

        FillUniformBuffer();

        vk.CmdBindPipeline(contextCommandBuffer, PipelineBindPoint.Graphics, Pipeline.Pipeline);

        var layout = Pipeline.PipelineLayout;

        var set = Pipeline.DescriptorSet.VulkanSet;
        
        var vertexUniformOffset = this.vertexUniformOffset;
        var fragmentUniformOffset = this.fragmentUniformOffset;
        uint[] offsets = new uint[2] { vertexUniformOffset, fragmentUniformOffset };
        fixed (uint* a = &offsets[0])
        {
            vk.CmdBindDescriptorSets(contextCommandBuffer, PipelineBindPoint.Graphics, layout, 0, 1, &set, 2, a);
        }
        // This is secretely an indexed draw call, however we use a custom buffer for the indices.
        vk.CmdDraw(contextCommandBuffer, _offset.IndexCount, 1, _offset.IndexOffset, 0);
    }

    public unsafe void Update(float dt)
    {
        FillUniformBuffer();
    }
    
}