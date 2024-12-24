using System.Runtime.InteropServices;
using Silk.NET.SDL;

namespace VulkanAbstraction.Helpers.Other;

public class SdlHelpers
{
    public static unsafe string[] GetExtensions(Window* window, Sdl sdl)
    {
        uint extensionCount = 0;

        var sdlR = sdl.VulkanGetInstanceExtensions(window, &extensionCount, (byte**)null);
        if (sdlR != SdlBool.True)
        {
            throw new Exception("Failed to get SDL Vulkan extensions");
        }

        IntPtr[] extensionPtrs = new IntPtr[extensionCount];
        string[] extensions = new string[extensionCount];

        fixed (IntPtr* extensionPtrsPtr = extensionPtrs)
        {
            if (sdl.VulkanGetInstanceExtensions(window, &extensionCount, (byte**)extensionPtrsPtr) != SdlBool.True)
            {
                throw new Exception("Failed to get SDL Vulkan extensions");
            }

            for (int i = 0; i < extensionCount; i++)
            {
                extensions[i] = Marshal.PtrToStringAnsi(extensionPtrs[i])!;
            }
        }

        return extensions;
    }
}