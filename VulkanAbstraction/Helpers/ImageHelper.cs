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

    public static unsafe void CreateImage(uint swapchainExtentWidth, uint swapchainExtentHeight, Format depthFormat, ImageTiling optimal, ImageUsageFlags depthStencilAttachmentBit, MemoryPropertyFlags deviceLocalBit, out Image depthImage, out DeviceMemory depthImageMemory)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D
            {
                Width = swapchainExtentWidth,
                Height = swapchainExtentHeight,
                Depth = 1
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Format = depthFormat,
            Tiling = optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = depthStencilAttachmentBit,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive
        };

        vk.CreateImage(VaContext.Current.Device, &imageInfo, null, out depthImage);

        MemoryRequirements memoryRequirements;
        vk.GetImageMemoryRequirements(VaContext.Current.Device, depthImage, &memoryRequirements);

        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = VaContext.Current.FindMemoryType(memoryRequirements.MemoryTypeBits, deviceLocalBit)
        };

        vk.AllocateMemory(VaContext.Current.Device, &allocateInfo, null, out depthImageMemory);
        vk.BindImageMemory(VaContext.Current.Device, depthImage, depthImageMemory, 0);
    }

    public static unsafe ImageView CreateImageView(Image depthImage, Format depthFormat, ImageAspectFlags imageAspectDepthBit)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = depthImage,
            ViewType = ImageViewType.Type2D,
            Format = depthFormat,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = imageAspectDepthBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        ImageView imageView;
        vk.CreateImageView(VaContext.Current.Device, &viewInfo, null, &imageView);
        return imageView;
    }
}