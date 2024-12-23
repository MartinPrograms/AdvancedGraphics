using Silk.NET.Vulkan;

namespace VulkanAbstraction.Common;

public class SwapchainMode
{
    public Format SwapchainImageFormat = default;
    public ColorSpaceKHR SwapchainColorSpace = default;
    public PresentModeKHR SwapchainPresentMode = default;
    public SwapchainMode(Format format, ColorSpaceKHR colorSpace)
    {
        SwapchainImageFormat = format;
        SwapchainColorSpace = colorSpace;
    }
}