using Silk.NET.Vulkan;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Other;

namespace VulkanAbstraction.Helpers.Vulkan.Other;

public class DescriptorHelper
{

    public static unsafe DescriptorPool CreateDescriptorPool(Device contextDevice)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        DescriptorPoolSize poolSize = new()
        {
            Type = DescriptorType.StorageBuffer,
            DescriptorCount = Constants.MaxTextures + (uint)3
        };
        
        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PPoolSizes = &poolSize,
            PoolSizeCount = 1,
            MaxSets = 1
        };
        
        DescriptorPool descriptorPool = default;
        vk.CreateDescriptorPool(contextDevice, &poolInfo, null, &descriptorPool);
        return descriptorPool;
    }

    public static unsafe DescriptorSet AllocateDescriptorSet(DescriptorSetLayout descriptorSetLayout, DescriptorPool pool)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &descriptorSetLayout
        };
        
        DescriptorSet descriptorSet = default;
        vk.AllocateDescriptorSets(VaContext.Current.Device, &allocInfo, &descriptorSet);
        return descriptorSet;
    }
}