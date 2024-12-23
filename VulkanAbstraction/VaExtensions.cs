using Common;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace VulkanAbstraction;

public static unsafe class VaExtensions
{
    public static PhysicalDeviceProperties GetProperties(this PhysicalDevice device)
    {
        PhysicalDeviceProperties props;
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        vk.GetPhysicalDeviceProperties(device, &props);
        return props;
    }
    
    public static string GetName(this PhysicalDevice device)
    {
        var props = device.GetProperties();
        return UnsafeExtensions.ToString(props.DeviceName);
    }
    
    public static void GetQueueFamilyProperties(this PhysicalDevice device, uint* count, QueueFamilyProperties* properties)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        vk.GetPhysicalDeviceQueueFamilyProperties(device, count, properties);
    }

    public static void Destroy(this SurfaceKHR surface)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        // DestroySurfaceKHR is an extension method, so we need to check if it's available.
        var a = vk.TryGetInstanceExtension<KhrSurface>(VaContext.Current!.Instance, out var surfaceExtension);
        surfaceExtension.DestroySurface(VaContext.Current.Instance, surface, null);
        
        // Unfortunately we can not use vk.DestroySurfaceKHR, because it does not exist in the Silk.NET Vulkan bindings.
    }
}