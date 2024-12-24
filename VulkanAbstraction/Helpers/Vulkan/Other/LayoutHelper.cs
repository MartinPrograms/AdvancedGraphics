using Silk.NET.Vulkan;

namespace VulkanAbstraction.Helpers.Vulkan.Other;

public class LayoutHelper
{
    public static uint MinUniformBufferOffsetAlignment { get; private set; }
    public static uint GetDynamicAlignment(uint sizeOf)
    {
        if (MinUniformBufferOffsetAlignment == 0)
        {
            var vk = VaContext.Current.Vk;
            if (vk == null)
            {
                throw new Exception("Vulkan API is not initialized");
            }
            
            PhysicalDeviceProperties properties = vk.GetPhysicalDeviceProperties(VaContext.Current.PhysicalDevice);
            MinUniformBufferOffsetAlignment = (uint)properties.Limits.MinUniformBufferOffsetAlignment;
        }
        
        // Size sizeof up to the nearest multiple of MinUniformBufferOffsetAlignment
        uint alignedSize = sizeOf;
        if (MinUniformBufferOffsetAlignment > 0)
        {
            alignedSize = (sizeOf + MinUniformBufferOffsetAlignment - 1) & ~(MinUniformBufferOffsetAlignment - 1);
        }
        
        return alignedSize;
    }
}