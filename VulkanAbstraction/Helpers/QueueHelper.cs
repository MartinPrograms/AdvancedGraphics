﻿using Silk.NET.Vulkan;
using VulkanAbstraction.Common;

namespace VulkanAbstraction.Helpers;

public class QueueHelper
{
    public static unsafe Queue GetDeviceQueue(Device contextDevice, QueueFamilyIndices indices, bool graphics = true)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        Queue queue = default;
        vk.GetDeviceQueue(contextDevice, indices.GraphicsFamily, 0, &queue);
        return queue;
    }
}