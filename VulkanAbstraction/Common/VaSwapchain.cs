using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using StupidSimpleLogger;
using VulkanAbstraction.Helpers;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanAbstraction.Common;

/// <summary>
/// A class that holds all the information about the swapchain.
/// </summary>
public class VaSwapchain
{
    public SwapchainKHR Swapchain = default;
    public Image[] SwapchainImages = default;
    public ImageView[] SwapchainImageViews = default;
    public Extent2D SwapchainExtent = default;
    public SwapchainMode SwapchainMode = default;
    public uint ImageCount = default;
    public uint CurrentImage = default;
    public Semaphore[] ImageAvailableSemaphores = default;
    public Semaphore[] RenderFinishedSemaphores = default;
    public Fence[] InFlightFences = default;
    public Fence[] ImagesInFlight = default;
    public RenderPass RenderPass = default;
    public Framebuffer[] Framebuffers = default;
    public CommandBuffer[] CommandBuffers = default;
    
    public Image[] DepthImages = default;
    public ImageView[] DepthImageViews = default;
    public DeviceMemory[] DepthImageMemory = default;
    
    public KhrSwapchain SwapchainExtension = default;
    
    public unsafe VaSwapchain(SurfaceKHR window, Sdl sdl)
    {
        Create(window, sdl);
        
        Logger.Info("Swapchain", "Mesh uniform buffer created");
    }

    public uint MaxFramesInFlight => ImageCount;


    public unsafe void Create(SurfaceKHR surface, Sdl sdl)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        if (!vk.TryGetDeviceExtension<KhrSwapchain>(VaContext.Current.Instance, VaContext.Current.Device, out var swapchainExtension))
        {
            throw new Exception("Failed to get swapchain extension");
        }
        
        Init(surface, swapchainExtension);

        // Now we can create the swapchain.
        CreateSwapchain(surface, swapchainExtension);

        CreateImagesAndViews(swapchainExtension, vk);

        Logger.Info("Swapchain", "Swapchain created");
        
        CreateSemaphores(vk);

        Logger.Info("Swapchain", "Semaphores created");
        
        CreateFences(vk);

        Logger.Info("Swapchain", "Fences created");
        
        CreateDepthResources(vk);
        
        Logger.Info("Swapchain", "Depth resources created");
        
        // Create the render pass.
        CreateRenderPass(vk);

        Logger.Info("Swapchain", "Render pass created");
        
        CreateFramebuffers(vk);

        Logger.Info("Swapchain", "Framebuffers created");
        VaContext.Current.DeletionQueue.Push((a) =>
        {
            Destroy();
        });
        
        Logger.Info("Swapchain", "Swapchain finished");
    }

    private void CreateDepthResources(Vk vk)
    {
        var depthFormat = CommonHelper.FindDepthFormat(VaContext.Current.PhysicalDevice);
        
        DepthImages = new Image[SwapchainImages.Length];
        DepthImageViews = new ImageView[SwapchainImages.Length];
        DepthImageMemory = new DeviceMemory[SwapchainImages.Length];
        
        for (var i = 0; i < SwapchainImages.Length; i++)
        {
            ImageHelper.CreateImage(SwapchainExtent.Width, SwapchainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachmentBit, MemoryPropertyFlags.DeviceLocalBit, out DepthImages[i], out DepthImageMemory[i]);
            DepthImageViews[i] = ImageHelper.CreateImageView(DepthImages[i], depthFormat, ImageAspectFlags.DepthBit);
        }
    }

    #region Creation
    public void CreateCommandBuffers()
    {
        CommandBuffers = new CommandBuffer[SwapchainImageViews.Length];

        for (int i = 0; i < ImageCount; i++)
        {
            CommandBuffers[i] =
                CommandHelper.CreateCommandBuffer(VaContext.Current.Device, VaContext.Current.CommandPool);
            
            Logger.Info("Swapchain", "Command buffer created");
            
            CommandHelper.PopulateCommandBuffer(CommandBuffers[i], this, i);
        }
    }

    private unsafe void CreateFramebuffers(Vk vk)
    {
        Framebuffers = new Framebuffer[SwapchainImageViews.Length];
        for (var i = 0; i < SwapchainImageViews.Length; i++)
        {
            var attachments = stackalloc ImageView[2] {SwapchainImageViews[i], DepthImageViews[i]};
            var framebufferCreateInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = RenderPass,
                AttachmentCount = 2,
                PAttachments = attachments,
                Width = SwapchainExtent.Width,
                Height = SwapchainExtent.Height,
                Layers = 1
            };
            
            var resultFramebuffer = vk.CreateFramebuffer(VaContext.Current.Device, &framebufferCreateInfo, null, out Framebuffers[i]);
            if (resultFramebuffer != Result.Success)
            {
                throw new Exception("Failed to create framebuffer");
            }
        }
    }

    private unsafe void CreateRenderPass(Vk vk)
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = SwapchainMode.SwapchainImageFormat,
            Samples = SampleCountFlags.SampleCount1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };
        
        var depthAttachment = new AttachmentDescription
        {
            Format = CommonHelper.FindDepthFormat(VaContext.Current.PhysicalDevice),
            Samples = SampleCountFlags.SampleCount1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
        };
        
        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };
        
        var depthAttachmentRef = new AttachmentReference
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };
        
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef
        };
        
        var dependency = new SubpassDependency
        {
            SrcSubpass = UInt32.MaxValue,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask = AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit
        };
        
        var attachments = new AttachmentDescription[2] {colorAttachment, depthAttachment};
        var subpasses = new SubpassDescription[1] {subpass};
        var dependencies = new SubpassDependency[1] {dependency};
        
        var renderPassInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = (uint)attachments.Length,
            PAttachments = (AttachmentDescription*)Unsafe.AsPointer(ref attachments[0]),
            SubpassCount = (uint)subpasses.Length,
            PSubpasses = (SubpassDescription*)Unsafe.AsPointer(ref subpasses[0]),
            DependencyCount = (uint)dependencies.Length,
            PDependencies = (SubpassDependency*)Unsafe.AsPointer(ref dependencies[0])
        };
        
        var resultRenderPass = vk.CreateRenderPass(VaContext.Current.Device, &renderPassInfo, null, out RenderPass);
        if (resultRenderPass != Result.Success)
        {
            throw new Exception("Failed to create render pass");
        }
    }

    private unsafe void CreateFences(Vk vk)
    {
        InFlightFences = new Fence[SwapchainImages.Length];
        ImagesInFlight = new Fence[SwapchainImages.Length];
        
        for (var i = 0; i < SwapchainImages.Length; i++)
        {
            var fenceCreateInfo = new FenceCreateInfo
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit
            };
            
            var resultFence = vk.CreateFence(VaContext.Current.Device, &fenceCreateInfo, null, out InFlightFences[i]);
            if (resultFence != Result.Success)
            {
                throw new Exception("Failed to create in flight fence");
            }
        }
    }

    private unsafe void CreateSemaphores(Vk vk)
    {
        var semaphoreCreateInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
            Flags = 0
        };
        
        ImageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
        RenderFinishedSemaphores = new Semaphore[MaxFramesInFlight];
        
        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            var resultSemaphore = vk.CreateSemaphore(VaContext.Current.Device, &semaphoreCreateInfo, null, out ImageAvailableSemaphores[i]);
            if (resultSemaphore != Result.Success)
            {
                throw new Exception("Failed to create image available semaphore");
            }
            
            resultSemaphore = vk.CreateSemaphore(VaContext.Current.Device, &semaphoreCreateInfo, null, out RenderFinishedSemaphores[i]);
            if (resultSemaphore != Result.Success)
            {
                throw new Exception("Failed to create render finished semaphore");
            }
        }
    }

    private unsafe void CreateImagesAndViews(KhrSwapchain swapchainExtension, Vk vk)
    {
        fixed (uint* imageCount = &ImageCount)
        {
            swapchainExtension.GetSwapchainImages(VaContext.Current.Device, Swapchain, imageCount, null);
        }
        
        SwapchainImages = new Image[ImageCount];
        Image* images = stackalloc Image[SwapchainImages.Length];
        fixed (uint* imageCount = &ImageCount)
        {
            swapchainExtension.GetSwapchainImages(VaContext.Current.Device, Swapchain, imageCount, images);
        }

        for (var i = 0; i < ImageCount; i++)
        {
            SwapchainImages[i] = images[i];
        }
        
        SwapchainImageViews = new ImageView[SwapchainImages.Length];
        for (var i = 0; i < SwapchainImages.Length; i++)
        {
            var createInfoView = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = SwapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = SwapchainMode.SwapchainImageFormat,
                Components = new ComponentMapping
                {
                    R = ComponentSwizzle.R,
                    G = ComponentSwizzle.G,
                    B = ComponentSwizzle.B,
                    A = ComponentSwizzle.A
                },
                SubresourceRange = new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.ImageAspectColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            var resultView = vk.CreateImageView(VaContext.Current.Device, &createInfoView, null, out SwapchainImageViews[i]);
            if (resultView != Result.Success)
            {
                throw new Exception("Failed to create image view");
            }
        }
    }

    private unsafe void CreateSwapchain(SurfaceKHR surface, KhrSwapchain swapchainExtension)
    {
        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = ImageCount,
            ImageFormat = SwapchainMode.SwapchainImageFormat,
            ImageColorSpace = SwapchainMode.SwapchainColorSpace,
            ImageExtent = SwapchainExtent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = SurfaceTransformFlagsKHR.IdentityBitKhr,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = SwapchainMode.SwapchainPresentMode,
            Clipped = true,
            OldSwapchain = default
        };

        var result = swapchainExtension.CreateSwapchain(VaContext.Current.Device, &createInfo, null, out Swapchain);
        if (result != Result.Success)
        {
            throw new Exception("Failed to create swapchain");
        }
    }

    private void Init(SurfaceKHR surface, KhrSwapchain swapchainExtension)
    {
        SwapchainExtension = swapchainExtension;
        
        SwapchainMode = SwapchainHelper.GetSwapchainFormat(surface, VaContext.Current.PhysicalDevice); // Colorspace and format
        SwapchainExtent = VaContext.Current!.Window!.Extent(); // Extent
        SwapchainMode.SwapchainPresentMode = PresentModeKHR.ImmediateKhr;
        
        ImageCount = 3; // Image count
    }
    #endregion

    public unsafe void Destroy()
    {
        Logger.Info("Swapchain", "Destroying swapchain");
        
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }

        vk.DeviceWaitIdle(VaContext.Current.Device);

        foreach (var imageView in SwapchainImageViews)
        {
            vk.DestroyImageView(VaContext.Current.Device, imageView, null);
            Logger.Info("Swapchain", "Image view destroyed");
        }

        var a = vk.TryGetDeviceExtension<KhrSwapchain>(VaContext.Current.Instance, VaContext.Current.Device, out var swapchainExtension);
        swapchainExtension.DestroySwapchain(VaContext.Current.Device, Swapchain, null);
        
        vk.DestroyRenderPass(VaContext.Current.Device, RenderPass, null);
        
        foreach (var framebuffer in Framebuffers)
        {
            vk.DestroyFramebuffer(VaContext.Current.Device, framebuffer, null);
        }
        
        foreach (var semaphore in ImageAvailableSemaphores)
        {
            vk.DestroySemaphore(VaContext.Current.Device, semaphore, null);
        }
        
        foreach (var semaphore in RenderFinishedSemaphores)
        {
            vk.DestroySemaphore(VaContext.Current.Device, semaphore, null);
        }
        
        foreach (var fence in InFlightFences)
        {
            vk.DestroyFence(VaContext.Current.Device, fence, null);
        }
        
        foreach (var fence in ImagesInFlight)
        {
            vk.DestroyFence(VaContext.Current.Device, fence, null);
        }
        
        foreach (var commandBuffer in CommandBuffers)
        {
            vk.FreeCommandBuffers(VaContext.Current.Device, VaContext.Current.CommandPool, 1, &commandBuffer);
        }
        
        Logger.Info("Swapchain", "Swapchain destroyed");
    }

    public unsafe void Recreate()
    {
        // First we need to wait for the device to finish.
        VaContext.Current.Vk.DeviceWaitIdle(VaContext.Current.Device);

        SwapchainExtent = VaContext.Current.Window!.Extent(); // if this is null, what?
        
        // Destroy the image views.
        foreach (var imageView in SwapchainImageViews)
        {
            VaContext.Current.Vk.DestroyImageView(VaContext.Current.Device, imageView, null);
        }
        
        // Destroy the framebuffers.
        foreach (var framebuffer in Framebuffers)
        {
            VaContext.Current.Vk.DestroyFramebuffer(VaContext.Current.Device, framebuffer, null);
        }
        
        // Destroy the command buffers.
        foreach (var commandBuffer in CommandBuffers)
        {
            VaContext.Current.Vk.FreeCommandBuffers(VaContext.Current.Device, VaContext.Current.CommandPool, 1, &commandBuffer);
        }
        
        // Destroy the depth resources.
        for (var i = 0; i < DepthImages.Length; i++)
        {
            VaContext.Current.Vk.DestroyImageView(VaContext.Current.Device, DepthImageViews[i], null);
            VaContext.Current.Vk.DestroyImage(VaContext.Current.Device, DepthImages[i], null);
            VaContext.Current.Vk.FreeMemory(VaContext.Current.Device, DepthImageMemory[i], null);
        }
        
        // Destroy the render pass.
        VaContext.Current.Vk.DestroyRenderPass(VaContext.Current.Device, RenderPass, null);
        
        // Destroy the swapchain.
        VaContext.Current.Vk.TryGetDeviceExtension<KhrSwapchain>(VaContext.Current.Instance, VaContext.Current.Device, out var swapchainExtension);
        swapchainExtension.DestroySwapchain(VaContext.Current.Device, Swapchain, null);
        
        // Recreate the swapchain.
        CreateSwapchain(VaContext.Current.Surface, swapchainExtension);
        
        // Recreate the images and views.
        CreateImagesAndViews(swapchainExtension, VaContext.Current.Vk);
        
        // Create the depth resources.
        CreateDepthResources(VaContext.Current.Vk);
        
        // Recreate the render pass.
        CreateRenderPass(VaContext.Current.Vk);
        
        // Recreate the framebuffers.
        CreateFramebuffers(VaContext.Current.Vk);
        
        CreateCommandBuffers();
        
        Console.WriteLine($"Swapchain recreated with new size {VaContext.Current.Window.Width}x{VaContext.Current.Window.Height}");
        Logger.Info("Swapchain", "Swapchain recreated with new size " + VaContext.Current.Window.Width + "x" + VaContext.Current.Window.Height);
    }
}