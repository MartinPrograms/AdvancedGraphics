using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Helpers.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanAbstraction.Common.Other.Vulkan;

/// <summary>
/// Class representing a uniform buffer, it can set data to the GPU.
/// </summary>
public class UniformBuffer
{
    public Buffer Buffer;
    public DeviceMemory Memory;
    public uint Size;
    public uint CurrentSize; // Tracks the actual used size of the buffer.
    private Buffer StagingBuffer;
    private DeviceMemory StagingMemory;
    
    public MemoryPropertyFlags PropertyFlags { get; set; } = MemoryPropertyFlags.DeviceLocalBit;
    public BufferUsageFlags UsageFlags { get; set; } = BufferUsageFlags.UniformBufferBit;

    public UniformBuffer(uint size)
    {
        Size = size;
        CurrentSize = 0;

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

    /// <summary>
    /// Updates a specific range of the buffer.
    /// </summary>
    public unsafe void UpdateRange<T>(T* data, uint offset, uint dataSize) where T : unmanaged
    {
        if (offset + dataSize > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(dataSize), "Data range exceeds buffer size.");
        }

        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        // Map memory for the range
        void* mappedMemory;
        vk.MapMemory(VaContext.Current.Device, StagingMemory, offset, dataSize, 0, &mappedMemory);
        Unsafe.CopyBlock(mappedMemory, data, dataSize);
        vk.UnmapMemory(VaContext.Current.Device, StagingMemory);

        // Copy the updated range to the GPU buffer
        CommonHelper.CopyBuffer(StagingBuffer, Buffer, dataSize, offset, offset);
    }

    /// <summary>
    /// Appends new data to the buffer. Resizes if necessary.
    /// </summary>
    public unsafe void AppendData<T>(T* data, uint dataSize) where T : unmanaged
    {
        if (CurrentSize + dataSize > Size)
        {
            Logger.Warning("Resizing buffer to accommodate new data.");
            ResizeBuffer(CurrentSize + dataSize);
        }

        UpdateRange(data, CurrentSize, dataSize);
        CurrentSize += dataSize;
    }

    /// <summary>
    /// Resizes the buffer to accommodate new data.
    /// </summary>
    private unsafe void ResizeBuffer(uint newSize)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        // Create new buffers with larger size
        Buffer newBuffer;
        DeviceMemory newMemory;
        CommonHelper.CreateBuffer(newSize, UsageFlags | BufferUsageFlags.TransferDstBit, PropertyFlags, out newBuffer, out newMemory);

        // Copy existing data to the new buffer
        CommonHelper.CopyBuffer(Buffer, newBuffer, CurrentSize, 0, 0);

        // Destroy the old buffers
        vk.DestroyBuffer(VaContext.Current.Device, Buffer, null);
        vk.FreeMemory(VaContext.Current.Device, Memory, null);

        // Replace the old buffers with new ones
        Buffer = newBuffer;
        Memory = newMemory;

        // Update size
        Size = newSize;
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

    public unsafe void FillBuffer<T>(T* ptr) where T : unmanaged
    {
        UpdateRange(ptr, 0, Size);
    }
}