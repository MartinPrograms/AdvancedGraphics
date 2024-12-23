﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Common;
using Silk.NET.Core;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using StupidSimpleLogger;
using VulkanAbstraction.Common;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanAbstraction.Helpers;

public class CommonHelper
{
    public unsafe static Instance CreateInstance(string name, string[] extensions, string[] layers,
        VaVulkanVersion vulkanVersion, Silk.NET.SDL.Window* window, Sdl sdl, out DebugUtilsMessengerEXT messengerExt, VaVulkanVersion? appVersion = null,
        VaVulkanVersion? engineVersion = null)
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = name.ToPointer(),
            ApplicationVersion = appVersion ?? VaVulkanVersion.Default,
            EngineVersion = engineVersion ?? VaVulkanVersion.Default,
            ApiVersion = vulkanVersion
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var sdlExtensions = SdlHelpers.GetExtensions(window, sdl);
        var combinedExtensions = new string[extensions.Length + sdlExtensions.Length];
        extensions.CopyTo(combinedExtensions, 0);
        sdlExtensions.CopyTo(combinedExtensions, extensions.Length);
        
        var extensionPtrs = new IntPtr[combinedExtensions.Length];
        for (int i = 0; i < combinedExtensions.Length; i++)
        {
            extensionPtrs[i] = Marshal.StringToHGlobalAnsi(combinedExtensions[i]);
        }
        
        #if DEBUG
        layers = layers.Append("VK_LAYER_KHRONOS_validation").ToArray();
        #endif

        var layerPtrs = new IntPtr[layers.Length + 1];
        for (int i = 0; i < layers.Length; i++)
        {
            layerPtrs[i] = Marshal.StringToHGlobalAnsi(layers[i]);
        }
        
        createInfo.EnabledExtensionCount = (uint)combinedExtensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)Unsafe.AsPointer(ref extensionPtrs[0]);
        createInfo.EnabledLayerCount = (uint)layers.Length;
        createInfo.PpEnabledLayerNames = (byte**)Unsafe.AsPointer(ref layerPtrs[0]);

        foreach(string ext in combinedExtensions)
        {
            Logger.Info("Extension", ext);
        }
        
        foreach(string layer in layers)
        {
            Logger.Info("Layer", layer);
        }
        
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        Instance instance;
        if (vk.CreateInstance(&createInfo, null, &instance) != Result.Success)
        {
            throw new Exception("Failed to create a Vulkan instance");
        }
        
        #if DEBUG
        
        // Setup debug messenger
        var debugCreateInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.InfoBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.WarningBitExt | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                          DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.GeneralBitExt,
            PfnUserCallback = DebugMessenger.DebugMessengerCallback,
            PUserData = null
        };
        
        if (!vk.TryGetInstanceExtension<Silk.NET.Vulkan.Extensions.EXT.ExtDebugUtils>(instance, out var debugUtilsExtension))
        {
            throw new Exception("Failed to get debug utils extension");
        }
        
        if (debugUtilsExtension.CreateDebugUtilsMessenger(instance, &debugCreateInfo, null, out var messenger) != Result.Success)
        {
            throw new Exception("Failed to create debug messenger");
        }
        
        messengerExt = messenger;
        Logger.Info("Debug", "Debug messenger created");
        
        #endif
        
        #if RELEASE
        messengerExt = default;
        #endif
        
        return instance;
    }

    public static Extent2D ClampExtent(Extent2D extent)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        if(!vk.TryGetInstanceExtension<KhrSurface>(VaContext.Current.Instance, out var surfaceExtension))
        {
            throw new Exception("KHR Surface extension is not available");
        }
        var capabilities = surfaceExtension.GetPhysicalDeviceSurfaceCapabilities(VaContext.Current.PhysicalDevice,
            VaContext.Current.Surface, out var khr);
        
        if (capabilities != Result.Success)
        {
            throw new Exception("Failed to get surface capabilities");
        }
        
        extent.Width = Math.Clamp(extent.Width, khr.MinImageExtent.Width, khr.MaxImageExtent.Width);
        extent.Height = Math.Clamp(extent.Height, khr.MinImageExtent.Height, khr.MaxImageExtent.Height);
        
        return extent;
    }

    public static unsafe void CreateBuffer(uint size, BufferUsageFlags bufferUsageUniformBufferBit, MemoryPropertyFlags flags, out Buffer buffer, out DeviceMemory memory)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        BufferCreateInfo bufferCreateInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = bufferUsageUniformBufferBit,
            SharingMode = SharingMode.Exclusive
        };
        
        if (vk.CreateBuffer(VaContext.Current.Device, &bufferCreateInfo, null, out buffer) != Result.Success)
        {
            throw new Exception("Failed to create buffer");
        }
        
        MemoryRequirements memoryRequirements;
        vk.GetBufferMemoryRequirements(VaContext.Current.Device, buffer, &memoryRequirements);
        
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = VaContext.Current.FindMemoryType(memoryRequirements.MemoryTypeBits, flags)
        };
        
        if (vk.AllocateMemory(VaContext.Current.Device, &allocateInfo, null, out memory) != Result.Success)
        {
            throw new Exception("Failed to allocate memory");
        }
        
        vk.BindBufferMemory(VaContext.Current.Device, buffer, memory, 0);
    }

    public static unsafe void CopyBuffer(Buffer stagingBuffer, Buffer buffer, uint size)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        CommandBuffer commandBuffer = CommandHelper.BeginSingleTimeCommands(VaContext.Current.Device, VaContext.Current.CommandPool);
        
        BufferCopy copyRegion = new()
        {
            Size = size
        };
        
        vk.CmdCopyBuffer(commandBuffer, stagingBuffer, buffer, 1, &copyRegion);
        
        CommandHelper.EndSingleTimeCommands(VaContext.Current.Device, VaContext.Current.GraphicsQueue, VaContext.Current.CommandPool, commandBuffer);
    }

    public static Format FindDepthFormat(PhysicalDevice currentPhysicalDevice)
    {
        return FindSupportedFormat(new[]
        {
            Format.D32SfloatS8Uint,
            Format.D32Sfloat,
            Format.D24UnormS8Uint
        }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit, currentPhysicalDevice);
    }

    private static Format FindSupportedFormat(Format[] formats, ImageTiling optimal, FormatFeatureFlags depthStencilAttachmentBit, PhysicalDevice currentPhysicalDevice)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        foreach (var format in formats)
        {
            FormatProperties props = vk.GetPhysicalDeviceFormatProperties(currentPhysicalDevice, format);
            if (optimal == ImageTiling.Linear && (props.LinearTilingFeatures & depthStencilAttachmentBit) == depthStencilAttachmentBit)
            {
                return format;
            }
            else if (optimal == ImageTiling.Optimal && (props.OptimalTilingFeatures & depthStencilAttachmentBit) == depthStencilAttachmentBit)
            {
                return format;
            }
        }
        
        throw new Exception("Failed to find supported format");
    }

}