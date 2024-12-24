using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;

namespace VulkanAbstraction.Helpers.Vulkan.Visual;

public class SurfaceHelper
{
    public static unsafe SurfaceKHR CreateSurface(Instance contextInstance, Window* window, Sdl sdl)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        VkNonDispatchableHandle surface = default;
        if (sdl.VulkanCreateSurface(window, new VkHandle(contextInstance.Handle), &surface) == SdlBool.True)
        {
            return new SurfaceKHR(surface.Handle);
        }
        
        throw new Exception("Failed to create a surface " + sdl.GetError()->ToString());
    }
}