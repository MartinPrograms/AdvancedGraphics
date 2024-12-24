using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Vulkan;

namespace VulkanAbstraction.Helpers.Vulkan.Visual;

public class SwapchainHelper
{
    public static unsafe SwapchainMode GetSwapchainFormat(SurfaceKHR surface, PhysicalDevice currentPhysicalDevice)
    {
        Format format = Format.Undefined;
        ColorSpaceKHR colorSpace = ColorSpaceKHR.PaceSrgbNonlinearKhr;
        
        // Get the supported formats
        uint formatCount = 0;
        KhrSurface surfaceExtension;
        if (!VaContext.Current.Vk.TryGetInstanceExtension<KhrSurface>(VaContext.Current.Instance, out surfaceExtension))
        {
            throw new Exception("Failed to get surface extension");
        }
        
        surfaceExtension.GetPhysicalDeviceSurfaceFormats(currentPhysicalDevice, surface, &formatCount, null);
        if (formatCount == 0)
        {
            throw new Exception("Failed to get surface formats");
        }
        
        var formats = stackalloc SurfaceFormatKHR[(int)formatCount];
        surfaceExtension.GetPhysicalDeviceSurfaceFormats(currentPhysicalDevice, surface, &formatCount, formats);

        for (var i = 0; i < formatCount; i++)
        {
            var f = formats[i];
            if (f.Format == Format.B8G8R8A8Srgb && f.ColorSpace == ColorSpaceKHR.PaceSrgbNonlinearKhr)
            {
                format = f.Format;
                colorSpace = f.ColorSpace;
                break; // For now no HDR support
                
                // TODO: Add HDR support
            }
        }
        
        if (format == Format.Undefined)
        {
            format = formats[0].Format;
            colorSpace = formats[0].ColorSpace;
        }
        
        return new SwapchainMode(format, colorSpace);
    }
}