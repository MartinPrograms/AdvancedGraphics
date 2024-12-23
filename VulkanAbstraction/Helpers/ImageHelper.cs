using Silk.NET.Vulkan;
using VulkanAbstraction.Common;

namespace VulkanAbstraction.Helpers;

public class ImageHelper
{
    // Function to transition image layout

    public static unsafe void TransitionImageLayout(CommandBuffer contextCommandBuffer, Image swapchainSwapchainImage, Format swapchainModeSwapchainImageFormat, ImageLayout undefined, ImageLayout colorAttachmentOptimal)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = undefined,
            NewLayout = colorAttachmentOptimal,
            SrcQueueFamilyIndex = Constants.QueueFamilyIgnored,
            DstQueueFamilyIndex = Constants.QueueFamilyIgnored,
            Image = swapchainSwapchainImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        AccessFlags sourceAccessMask;
        AccessFlags destinationAccessMask;

        if (undefined == ImageLayout.Undefined && colorAttachmentOptimal == ImageLayout.ColorAttachmentOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;

            sourceAccessMask = 0;
            destinationAccessMask = AccessFlags.ColorAttachmentWriteBit;
        }
        else if (undefined == ImageLayout.ColorAttachmentOptimal && colorAttachmentOptimal == ImageLayout.PresentSrcKhr)
        {
            barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
            barrier.DstAccessMask = AccessFlags.MemoryReadBit;

            sourceAccessMask = AccessFlags.ColorAttachmentWriteBit;
            destinationAccessMask = AccessFlags.MemoryReadBit;
        }
        else
        {
            throw new Exception("Unsupported layout transition");
        }

        vk.CmdPipelineBarrier(contextCommandBuffer, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.ColorAttachmentOutputBit, 0, 0, null, 0, null, 1, &barrier);
    }
}