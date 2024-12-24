using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Vulkan;

namespace VulkanAbstraction.Helpers.Vulkan.Other;

public unsafe class DeviceHelper
{
    public static PhysicalDevice LoadPhysicalDevice(Instance instance)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        uint deviceCount = 0;
        vk.EnumeratePhysicalDevices(instance, &deviceCount, null);
        
        if (deviceCount == 0)
        {
            throw new Exception("Failed to find GPUs with Vulkan support");
        }
        
        var devices = stackalloc PhysicalDevice[(int)deviceCount];
        vk.EnumeratePhysicalDevices(instance, &deviceCount, devices);
        
        // Now we get all the extensions and features of the device.
        int quality = 0;
        PhysicalDevice bestDevice = default;
        
        for (int i = 0; i < deviceCount; i++)
        {
            var device = devices[i];
            var props = device.GetProperties();
            Logger.Info("Physical Device", $"Name: {device.GetName()}, API Version: {new VaVulkanVersion(props.ApiVersion)}");
            
            int deviceQuality = GetDeviceQuality(device,props);
            if (deviceQuality > quality)
            {
                quality = deviceQuality;
                bestDevice = device;
            }
        }
        
        if (bestDevice.Handle == default)
        {
            throw new Exception("Failed to find a suitable GPU");
        }
        
        Logger.Info("Physical Device", $"Selected: {bestDevice.GetName()}");
        
        return bestDevice;
    }

    private static int GetDeviceQuality(PhysicalDevice d,PhysicalDeviceProperties device)
    {
        uint sum = 0;
        sum += device.DeviceType == PhysicalDeviceType.DiscreteGpu ? 1000u : 0;
        sum += device.Limits.MaxImageDimension2D;
        sum += device.Limits.MaxViewports;
        sum += device.Limits.MaxFramebufferWidth;
        sum += device.Limits.MaxFramebufferHeight;
        sum += device.Limits.MaxFramebufferLayers;
        sum += device.Limits.MaxDescriptorSetUniformBuffers;
        sum += device.Limits.MaxDescriptorSetStorageBuffers;
        sum += device.Limits.MaxDescriptorSetSampledImages;
        sum += device.Limits.MaxDescriptorSetSamplers;
        sum += device.Limits.MaxDescriptorSetStorageImages;
        sum += device.Limits.MaxDescriptorSetInputAttachments;
        sum += device.Limits.MaxVertexInputAttributes;
        sum += device.Limits.MaxVertexInputBindings;
        sum += device.Limits.MaxVertexInputAttributeOffset;
        sum += device.Limits.MaxVertexInputBindingStride;
        sum += device.Limits.MaxVertexOutputComponents;
        sum += device.Limits.MaxTessellationGenerationLevel;
        sum += device.Limits.MaxTessellationPatchSize;
        sum += device.Limits.MaxTessellationControlPerVertexInputComponents;
        sum += device.Limits.MaxTessellationControlPerVertexOutputComponents;
        sum += device.Limits.MaxTessellationControlPerPatchOutputComponents;
        sum += device.Limits.MaxTessellationControlTotalOutputComponents;
        sum += device.Limits.MaxTessellationEvaluationInputComponents;
        sum += device.Limits.MaxTessellationEvaluationOutputComponents;
        sum += device.Limits.MaxGeometryShaderInvocations;
        sum += device.Limits.MaxGeometryInputComponents;
        sum += device.Limits.MaxGeometryOutputComponents;
        sum += device.Limits.MaxGeometryOutputVertices;
        sum += device.Limits.MaxGeometryTotalOutputComponents;
        sum += device.Limits.MaxFragmentInputComponents;
        sum += device.Limits.MaxFragmentOutputAttachments;
        sum += device.Limits.MaxFragmentDualSrcAttachments;
        sum += device.Limits.MaxFragmentCombinedOutputResources;
        sum += device.Limits.MaxComputeSharedMemorySize;
        sum += device.Limits.MaxMemoryAllocationCount;
        sum += device.Limits.MaxSamplerAllocationCount;
        sum += (uint)new VaVulkanVersion(device.ApiVersion).Minor * 1000;
        sum += (uint)new VaVulkanVersion(device.ApiVersion).Patch * 100;
        sum += (uint)new VaVulkanVersion(device.ApiVersion).Major * 10000;
        
        Logger.Info("Device Quality", $"Device: {d.GetName()}, Quality: {sum}");

        return (int)sum;
    }

    public static Device LoadLogicalDevice(PhysicalDevice physicalDevice, out QueueFamilyIndices QueueFamilyIndices, params string[] extensions)
    {
        var vk = VaContext.Current?.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan API is not initialized");
        }
        
        var queueFamilyIndices = QueueFamilyIndices.FindQueueFamilies(physicalDevice, VaContext.Current!.Surface);
        
        float queuePriority = 1.0f;
        DeviceQueueCreateInfo queueCreateInfo = new()
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.GraphicsFamily,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };

        PhysicalDeviceDescriptorIndexingFeatures indexingFeatures = new PhysicalDeviceDescriptorIndexingFeatures()
        {
            SType = StructureType.PhysicalDeviceDescriptorIndexingFeatures,
            RuntimeDescriptorArray = true,
            ShaderInputAttachmentArrayDynamicIndexing = true,
            ShaderUniformTexelBufferArrayDynamicIndexing = true,
            ShaderStorageTexelBufferArrayDynamicIndexing = true,
            ShaderUniformBufferArrayNonUniformIndexing = true,
            DescriptorBindingPartiallyBound = true,
        };
        
        
        
        PhysicalDeviceFeatures deviceFeatures = new() // Some simple features that we want to enable.
        {
            GeometryShader = true,
            SamplerAnisotropy = true,
            DepthClamp = true,
        };
        
        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            PEnabledFeatures = &deviceFeatures,
            PNext = &indexingFeatures
        };
        
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        var extensionPtrs = new IntPtr[extensions.Length];
        for (int i = 0; i < extensions.Length; i++)
        {
            extensionPtrs[i] = Marshal.StringToHGlobalAnsi(extensions[i]);
        }
        createInfo.PpEnabledExtensionNames = (byte**)Unsafe.AsPointer(ref extensionPtrs[0]);
        
        if (vk.CreateDevice(physicalDevice, &createInfo, null, out var device) != Result.Success)
        {
            Logger.Error("Device", "Failed to create logical device, geometry shader available?");
            throw new Exception("Failed to create logical device");
        }
        
        QueueFamilyIndices = queueFamilyIndices;
        
        return device;
    }
}