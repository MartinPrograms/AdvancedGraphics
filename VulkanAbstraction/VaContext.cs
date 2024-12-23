﻿using Silk.NET.SDL;
using Silk.NET.Vulkan;
using VulkanAbstraction.Common.Graphical;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Vulkan;
using VulkanAbstraction.Helpers.Vulkan.Other;

namespace VulkanAbstraction;

/// <summary>
/// Contains all the Vulkan context information.
/// For example, the device, the instance, the surface, etc.
/// </summary>
public class VaContext
{
    public static VaContext? Current { get; private set; }
    
    // Begin basic Vulkan objects
    public Vk Vk;
    public Instance Instance = default;
    public PhysicalDevice PhysicalDevice = default;
    public QueueFamilyIndices Indices = default;
    public Device Device = default;
    public Queue GraphicsQueue = default;
    public Queue PresentQueue = default;
    public SurfaceKHR Surface = default;
    public VaWindow? Window = default;
    public CommandPool CommandPool = default;
    public Sdl Sdl = default;
    
    // Debug:
    public DebugUtilsMessengerEXT DebugMessenger = default;
    
    // Begin swapchain objects
    public VaSwapchain Swapchain = default;
    
    public Stack<Action<VaContext>> DeletionQueue = new(); // Fifo, so we can delete things in the correct order. 
    
    public VaContext()
    {
        if (Current != null)
        {
            throw new Exception("Only one VaContext can be created at a time");
        }
        Vk = Vk.GetApi();
        
        Current = this;
    }

    public void LoadDevices(out QueueFamilyIndices indices, params string[] extensions)
    {
        // Here we load the physical and logical devices, the rest will be loaded in the future, as it requires a surface.
        PhysicalDevice = DeviceHelper.LoadPhysicalDevice(Instance); // This creates the physical device.
        
        Device = DeviceHelper.LoadLogicalDevice(PhysicalDevice, out indices, extensions); // This creates the logical device and the queues.
    }

    public uint FindMemoryType(uint memoryRequirementsMemoryTypeBits, MemoryPropertyFlags flags)
    {
        var vk = Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        var memoryProperties = vk.GetPhysicalDeviceMemoryProperties(Current.PhysicalDevice);
        
        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((memoryRequirementsMemoryTypeBits & (1 << (int)i)) != 0 && (memoryProperties.MemoryTypes[(int)i].PropertyFlags & flags) == flags)
            {
                return i;
            }
        }
        
        throw new Exception("Failed to find suitable memory type");
    }
}