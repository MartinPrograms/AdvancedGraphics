using System.Numerics;
using Common;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Common;
using VulkanAbstraction.Helpers;
using VulkanAbstraction.Pipelines;
using Window = Common.Window;

namespace VulkanAbstraction;

public unsafe class VaWindow : Window
{
    public VaContext Context { get; private set; }
    public VaVulkanVersion VulkanVersion { get; set; }

    public static List<Mesh> Meshes = new();
    
    public Extent2D Extent()
    {
        var extent = new Extent2D();
        extent.Width = (uint)_width;
        extent.Height = (uint)_height;

        extent = CommonHelper.ClampExtent(extent);
        
        return extent;
    }

    public VaWindow(int width, int height, string title, VaContext context, VaVulkanVersion vulkanVersion) : base(width, height, title, WindowFlags.Vulkan | WindowFlags.Resizable)
    {
        Context = context;
        context.Window = this;
        context.Sdl = _sdl;
        VulkanVersion = vulkanVersion;
    }
    
    private List<VaPipeline> _pipelines = new();

    public override void Load()
    {
        Logger.Info("Creating Vulkan Instance");
        
        // include the debug extension for message output
        Context.Instance = Helpers.CommonHelper.CreateInstance(this._title, new[]
            {
                "VK_EXT_debug_utils"
            },
            new string[0], 
            VulkanVersion, _window, _sdl, out var messengerExt);
        Context.DebugMessenger = messengerExt;
        
        Logger.Info("Vulkan Instance Created", $"Instance: {Context.Instance} with Vulkan Version: {VulkanVersion.Major}.{VulkanVersion.Minor}.{VulkanVersion.Patch}");
        
        Context.LoadDevices(out Context.Indices, "VK_KHR_swapchain"); 
        Logger.Info("Vulkan Devices Loaded", $"Devices: {Context.Device} with Physical Device: {Context.PhysicalDevice} with name {Context.PhysicalDevice.GetName()}");
        
        
        Context.Surface = Helpers.SurfaceHelper.CreateSurface(Context.Instance, this._window, _sdl);
        Logger.Info("Vulkan Surface Created", $"Surface: {Context.Surface}");

        Context.GraphicsQueue = Helpers.QueueHelper.GetDeviceQueue(Context.Device, Context.Indices);
        Context.PresentQueue = Helpers.QueueHelper.GetDeviceQueue(Context.Device, Context.Indices, false); 
        
        Logger.Info("Vulkan Queues Loaded", $"Graphics Queue: {Context.GraphicsQueue} Present Queue: {Context.PresentQueue}");
        
        // Create the command pool.
        Context.CommandPool = CommandHelper.CreateCommandPool(Context.Device, Context.Indices);
        Logger.Info("Vulkan Command Pool Created", $"Command Pool: {Context.CommandPool}");

        // Create the swapchain.
        Context.Swapchain = new VaSwapchain(Context.Surface, _sdl);
        Logger.Info("Vulkan Context Loaded", $"Context: {Context}");
        
        // Create a default pipeline.
        _pipelines.Add(new VaPipeline("default", "./Shaders/test.vert", "./Shaders/test.frag"));
        
        // Create a mesh.
        var meshResult = AssimpHelper.LoadModel("./Models/Duck.glb");
        Meshes.Add(new Mesh(meshResult.Item1, meshResult.Item2, "default"){Transform = new Transform(new Vector3(2,0,-2), Quaternion.Identity, new Vector3(0.005f))});
        Meshes.Add(new Mesh(meshResult.Item1, meshResult.Item2, "default"){Transform = new Transform(new Vector3(0,2,-2), Quaternion.Identity, new Vector3(0.005f))});
        Meshes.Add(new Mesh(meshResult.Item1, meshResult.Item2, "default"){Transform = new Transform(new Vector3(-1,2,-2), Quaternion.Identity, new Vector3(0.005f))});
        Meshes.Add(new Mesh(meshResult.Item1, meshResult.Item2, "default"){Transform = new Transform(new Vector3(2,-1,-2), Quaternion.Identity, new Vector3(0.005f))});

        
        Context.Swapchain.CreateCommandBuffers(); // This requires the pipelines to be created first, however the pipeline is also dependent on the swapchain, so we have to create the pipelines first.
    }

    public override void Update()
    {
        
    }
    
    public override void Render()
    {
        var vk = Context.Vk;
        uint currentFrame = Context.Swapchain.CurrentImage % Context.Swapchain.MaxFramesInFlight;
        Context.Swapchain.CurrentImage = currentFrame;
        uint index = 0;

        // Per-frame synchronization primitives
        var imageAvailableSemaphore = Context.Swapchain.ImageAvailableSemaphores[currentFrame];
        var renderFinishedSemaphore = Context.Swapchain.RenderFinishedSemaphores[currentFrame];
        var inFlightFence = Context.Swapchain.InFlightFences[currentFrame];

        // Wait for the current frame's fence to ensure the frame has finished
        vk.WaitForFences(Context.Device, 1, new[] { inFlightFence }, true, ulong.MaxValue);
        vk.ResetFences(Context.Device, 1, new[] { inFlightFence });

        // Acquire the next image
        var acquireResult = Context.Swapchain.SwapchainExtension.AcquireNextImage(
            Context.Device,
            Context.Swapchain.Swapchain,
            ulong.MaxValue,
            imageAvailableSemaphore,
            default,
            &index
        );

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            Context.Swapchain.Recreate();
            return;
        }
        
        if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire swapchain image!");
        }
        
        var commandBuffer = Context.Swapchain.CommandBuffers[index];
        
        CommandHelper.PopulateCommandBuffer(commandBuffer, Context.Swapchain, (int)index);
        
        // Submit command buffer
        var waitDstStageMask = PipelineStageFlags.ColorAttachmentOutputBit;

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &imageAvailableSemaphore,
            PWaitDstStageMask = &waitDstStageMask,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &renderFinishedSemaphore
        };

        vk.QueueSubmit(Context.GraphicsQueue, 1, &submitInfo, inFlightFence);

        // Present the image
        var swapchain = Context.Swapchain.Swapchain;

        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &renderFinishedSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &index
        };

        var presentResult = Context.Swapchain.SwapchainExtension.QueuePresent(Context.PresentQueue, &presentInfo);

        if (presentResult == Result.ErrorOutOfDateKhr)
        {
            Context.Swapchain.Recreate();
            return;
        }
        
        if (presentResult != Result.Success && presentResult != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to present swapchain image!");
        }

        Context.Swapchain.CurrentImage++;
    }

    public override void Unload()
    {
        Logger.Warning("Closing","Program is closing, starting deletion queue");
        Context.Vk.DeviceWaitIdle(Context.Device);
        // Start the deletion queue.
        while (Context.DeletionQueue.Count > 0)
        {
            var action = Context.DeletionQueue.Pop();
            action(Context);
            
            Logger.Info("Deletion Queue", "Action Completed");
        }
        
        Logger.Info("Deletion Queue Finished");
        
        Context.Vk!.DestroyCommandPool(Context.Device, Context.CommandPool, null);
        
        #if DEBUG
        if (!Context.Vk.TryGetInstanceExtension<Silk.NET.Vulkan.Extensions.EXT.ExtDebugUtils>(Context.Instance, out var debugUtilsExtension))
        {
            throw new Exception("Failed to get debug utils extension");
        }

        debugUtilsExtension.DestroyDebugUtilsMessenger(Context.Instance, Context.DebugMessenger, null);
        Logger.Info("Debug Messenger Destroyed");
        #endif

        
        Context.Vk!.DestroyDevice(Context.Device, null);
        Logger.Info("Vulkan Device Destroyed");

        Context.Surface.Destroy();
        Logger.Info("Vulkan Surface Destroyed");
        
        Context.Vk!.DestroyInstance(Context.Instance, null);
        Logger.Info("Vulkan Instance Destroyed");
        
        Logger.Info("Vulkan Context Unloaded");
    }


}