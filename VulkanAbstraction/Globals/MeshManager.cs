using System.Numerics;
using Silk.NET.Vulkan;
using VulkanAbstraction.Common.Graphical;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Vulkan;

namespace VulkanAbstraction.Globals;

/// <summary>
/// This class is responsible for managing all the meshes in the application.
/// It also holds the current view and projection matrices, used for rendering.
/// </summary>
public class MeshManager
{
    public static Matrix4x4 CurrentViewMatrix = Matrix4x4.Identity;
    public static Matrix4x4 CurrentProjectionMatrix = Matrix4x4.Identity;
    
    public static UniformBuffer GlobalVertexBuffer;
    public static UniformBuffer GlobalIndexBuffer;
    public static Dictionary<string,MeshOffset> MeshOffsets = new();

    private static bool _initialized = false;
    public static unsafe void Init()
    {
        GlobalVertexBuffer = new UniformBuffer(10000000);
        GlobalIndexBuffer = new UniformBuffer(10000000); // 10MB
        
        GlobalVertexBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, BufferUsageFlags.VertexBufferBit | BufferUsageFlags.StorageBufferBit| BufferUsageFlags.TransferSrcBit);
        GlobalIndexBuffer.CreateBuffer(MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, BufferUsageFlags.VertexBufferBit | BufferUsageFlags.StorageBufferBit| BufferUsageFlags.TransferSrcBit);
    }
    
    public static unsafe void RegisterMesh(string name, (Vertex[], uint[]) meshResult)
    {
        if (!_initialized)
        {
            Init();
            _initialized = true;
        }
        
        MeshOffsets.Add(name, new MeshOffset
        {// Using linq to calculate the offset and count of the mesh
            VertexOffset = (uint)MeshOffsets.Sum(x => x.Value.VertexCount),
            IndexOffset = (uint)MeshOffsets.Sum(x => x.Value.IndexCount),
            VertexCount = (uint)((uint)meshResult.Item1.Length),
            IndexCount = (uint)((uint)meshResult.Item2.Length)
        });

        float[] data = Vertex.GetVertexData(meshResult.Item1);
        
        fixed (float* dataPtr = &data[0])
        {
            GlobalVertexBuffer.AppendData(dataPtr, (uint)(meshResult.Item1.Length * Vertex.SizeInBytes));
        }
        
        fixed (uint* indexPtr = &meshResult.Item2[0])
        {
            GlobalIndexBuffer.AppendData(indexPtr, (uint)meshResult.Item2.Length * sizeof(uint));
        }   
    }
}