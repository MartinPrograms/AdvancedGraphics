using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Helpers;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanAbstraction.Common;

/// <summary>
/// Class representing a uniform buffer, it can set data to the GPU.
/// </summary>
public class UniformBuffer
{
    public Buffer Buffer;
    public DeviceMemory Memory;
    public uint Size;
    private Buffer StagingBuffer;
    private DeviceMemory StagingMemory;
    
    public UniformBuffer(uint size)
    {
        Size = size;
        
        VaContext.Current.DeletionQueue.Push((c) =>
        {
            Dispose();
        });
    }
    
    public void CreateBuffer(MemoryPropertyFlags flags, BufferUsageFlags bufferFlags = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.VertexBufferBit)
    {
        CommonHelper.CreateBuffer(Size, bufferFlags | BufferUsageFlags.TransferDstBit, flags, out Buffer, out Memory);
        CommonHelper.CreateBuffer(Size, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostVisibleBit, out StagingBuffer, out StagingMemory);
    }

    public unsafe void SetData<T>(T* data) where T : unmanaged
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        void* mappedMemory;
        vk.MapMemory(VaContext.Current.Device, StagingMemory, 0, Size, 0, &mappedMemory);
        Unsafe.CopyBlock(mappedMemory, data, Size);
        vk.UnmapMemory(VaContext.Current.Device, StagingMemory);
        
        CommonHelper.CopyBuffer(StagingBuffer, Buffer, Size);
    }
    
    public unsafe void Dispose()
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        vk.DestroyBuffer(VaContext.Current.Device, Buffer, null);
        vk.FreeMemory(VaContext.Current.Device, Memory, null);
        
        vk.DestroyBuffer(VaContext.Current.Device, StagingBuffer, null);
        vk.FreeMemory(VaContext.Current.Device, StagingMemory, null);
    }
}