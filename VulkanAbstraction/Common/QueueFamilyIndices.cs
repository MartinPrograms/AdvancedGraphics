using Silk.NET.Vulkan;

namespace VulkanAbstraction.Common;

public class QueueFamilyIndices 
{
    public uint GraphicsFamily { get; set; } = uint.MaxValue;
    public bool IsComplete => GraphicsFamily != uint.MaxValue;

    public static unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice physicalDevice, SurfaceKHR surface)
    {
        QueueFamilyIndices indices = new();

        uint queueFamilyCount = 0;
        physicalDevice.GetQueueFamilyProperties(&queueFamilyCount, null);

        QueueFamilyProperties* queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        physicalDevice.GetQueueFamilyProperties(&queueFamilyCount, queueFamilies);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            if (indices.IsComplete)
            {
                break;
            }
        }

        return indices;
    }
}