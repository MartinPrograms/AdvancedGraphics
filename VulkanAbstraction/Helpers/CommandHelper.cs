using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using VulkanAbstraction.Common;
using VulkanAbstraction.Pipelines;

namespace VulkanAbstraction.Helpers;

public class CommandHelper
{
    public static unsafe CommandPool CreateCommandPool(Device contextDevice, QueueFamilyIndices contextIndices)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = contextIndices.GraphicsFamily,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit
        };
        
        CommandPool commandPool = default;
        vk.CreateCommandPool(contextDevice, &poolInfo, null, &commandPool);
        return commandPool;
        
    }

    public static unsafe CommandBuffer CreateCommandBuffer(Device contextDevice, CommandPool contextCommandPool)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = contextCommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        
        CommandBuffer commandBuffer = default;
        vk.AllocateCommandBuffers(contextDevice, &allocInfo, &commandBuffer);
        return commandBuffer;
    }

    public static unsafe void PopulateCommandBuffer(CommandBuffer contextCommandBuffer, VaSwapchain contextSwapchain, int index)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        // This clears the screen with a color.
        ClearValue colorClear = new()
        {
            Color = new ClearColorValue(0.2f, 0.3f, 0.5f, 1.0f),
        };
        
        ClearValue depthClear = new()
        {
            DepthStencil = new ClearDepthStencilValue(1.0f, 0)
        };
        
        ClearValue[] clearValues = new ClearValue[]
        {
            colorClear,
            depthClear
        };
        
        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = contextSwapchain.RenderPass,
            Framebuffer = contextSwapchain.Framebuffers[index],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = contextSwapchain.SwapchainExtent
            },
            ClearValueCount = 2,
            PClearValues = (ClearValue*)Unsafe.AsPointer(ref clearValues[0])
        };
        // This command buffer will be reused.
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        
        vk.BeginCommandBuffer(contextCommandBuffer, &beginInfo);
        
        // This is where we render.
        // First, we transition the image to a color attachment.
        ImageHelper.TransitionImageLayout(contextCommandBuffer,
            contextSwapchain.SwapchainImages[index],
            contextSwapchain.SwapchainMode.SwapchainImageFormat,
            ImageLayout.Undefined,
            ImageLayout.ColorAttachmentOptimal
        );
        
        vk.CmdBeginRenderPass(contextCommandBuffer, &renderPassInfo, SubpassContents.Inline);
        
        // Here we should do rendering and what not.
        
        // BEGIN RENDERING HERE!
        
        Viewport viewport = new()
        {
            X = 0.0f,
            Y = 0.0f,
            Width = contextSwapchain.SwapchainExtent.Width,
            Height = contextSwapchain.SwapchainExtent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        
        Rect2D scissor = new()
        {
            Offset = new Offset2D(0, 0),
            Extent = contextSwapchain.SwapchainExtent
        };
        
        vk.CmdSetViewport(contextCommandBuffer, 0, 1, &viewport);
        vk.CmdSetScissor(contextCommandBuffer, 0, 1, &scissor);
        
        foreach (var mesh in VaWindow.Meshes)
        {
            mesh.Draw(contextCommandBuffer);
        }
        
        // STOPS HERE!
        
        // There is nothing to render, so we just end the render pass.
        vk.CmdEndRenderPass(contextCommandBuffer);
        
        if (vk.EndCommandBuffer(contextCommandBuffer) != Result.Success)
        {
            throw new Exception("Failed to record command buffer");
        }
    }

    public static unsafe CommandBuffer BeginSingleTimeCommands(Device device, CommandPool commandPool)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };
        
        CommandBuffer commandBuffer = default;
        vk.AllocateCommandBuffers(device, &allocInfo, &commandBuffer);
        
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        
        vk.BeginCommandBuffer(commandBuffer, &beginInfo);
        
        return commandBuffer;
    }

    public static unsafe void EndSingleTimeCommands(Device device, Queue queue, CommandPool commandPool, CommandBuffer commandBuffer)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        vk.EndCommandBuffer(commandBuffer);
        
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };
        
        vk.QueueSubmit(queue, 1, &submitInfo, default);
        vk.QueueWaitIdle(queue);
        
        vk.FreeCommandBuffers(device, commandPool, 1, &commandBuffer);
    }

    public static void ResetCommandBuffer(CommandBuffer commandBuffer)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        vk.ResetCommandBuffer(commandBuffer, CommandBufferResetFlags.None);
    }

    public static unsafe void CreateCommandBuffers(Device currentDevice, CommandPool currentCommandPool, int length, CommandBuffer[] commandBuffers)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = currentCommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)length
        };
        
        fixed (CommandBuffer* commandBuffer = commandBuffers)
        {
            vk.AllocateCommandBuffers(currentDevice, &allocInfo, commandBuffer);
        }
    }
}