using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Common;
using Silk.NET.Core;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using StupidSimpleLogger;
using VulkanAbstraction.Common;

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

        var layerPtrs = new IntPtr[layers.Length];
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
}