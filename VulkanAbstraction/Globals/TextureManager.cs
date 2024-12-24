using Silk.NET.Vulkan;
using StbImageSharp;
using VulkanAbstraction.Helpers.Other;
using VulkanAbstraction.Helpers.Vulkan;
using VulkanAbstraction.Helpers.Vulkan.Visual;

namespace VulkanAbstraction.Globals;

/// <summary>
/// Singular class to rule them all.
/// </summary>
public class TextureManager
{
    public static Dictionary<string, VaTexture> Textures = new();

    private static bool _initialized = false;
    public static void LoadTexture(string name, string textureFilePath)
    {
        if (!_initialized)
        {
            VaContext.Current.DeletionQueue.Push((c) =>
            {
                Cleanup();
            });
            _initialized = true;
        }
        if (Textures.ContainsKey(name))
        {
            return; // Prevent reloading of the same texture
        }

        // Load texture data using StbImageSharp
        var image = ImageResult.FromMemory(StbImageLoader.Load(textureFilePath), ColorComponents.RedGreenBlueAlpha);
        if (image == null || image.Data == null)
        {
            Console.WriteLine($"Failed to load texture from {textureFilePath}");
            return;
        }

        // Create texture object
        var texture = new VaTexture
        {
            Width = image.Width,
            Height = image.Height,
            Layout = ImageLayout.ShaderReadOnlyOptimal,
            Usage = ImageUsageFlags.ImageUsageSampledBit | ImageUsageFlags.ImageUsageTransferDstBit,
            Aspect = ImageAspectFlags.ImageAspectColorBit,
            Format = Format.R8G8B8A8Unorm,
            Tiling = ImageTiling.Optimal,
            Type = ImageType.Type2D,
            Flags = ImageCreateFlags.None,
            MipLevels = 1,
            Layers = 1,
            CurrentMipLevel = 0,
            CurrentLayer = 0
        };

        // Create Vulkan image, memory, and image view
        texture.Image = ImageHelper.CreateImage((uint)texture.Width, (uint)texture.Height, texture.MipLevels, texture.Layers, texture.Format, texture.Tiling, texture.Usage, texture.Flags, texture.Type);
        texture.Memory = CommonHelper.AllocateAndBindMemory(texture.Image, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
        texture.ImageView = ImageHelper.CreateImageView(texture.Image, texture.Format, texture.Aspect, texture.MipLevels, texture.Layers);
        texture.Sampler = ImageHelper.CreateSampler();

        // Copy image data to GPU
        CommonHelper.CopyBufferToImage(image.Data, texture.Image, (uint)image.Width, (uint)image.Height);

        // Add texture to the manager
        Textures.Add(name, texture);
    }

    public static void Cleanup()
    {
        foreach (var texture in Textures.Values)
        {
            texture.Dispose();
        }
        Textures.Clear();
    }

    public static VaTexture []GetTextures()
    {
        return Textures.Values.ToArray();
    }
}

public class VaTexture
{
    public int Width;
    public int Height;
    public Image Image;
    public ImageView ImageView;
    public Sampler Sampler;
    public DeviceMemory Memory;
    public ImageLayout Layout;
    public ImageUsageFlags Usage;
    public ImageAspectFlags Aspect;
    public Format Format;
    public ImageTiling Tiling;
    public ImageType Type;
    public ImageCreateFlags Flags;
    public uint MipLevels;
    public uint Layers;
    public uint CurrentMipLevel;
    public uint CurrentLayer;

    public VaTexture()
    {
        Image = default;
        ImageView = default;
        Sampler = default;
        Memory = default;
        Layout = default;
        Usage = default;
        Aspect = default;
        Format = default;
        Tiling = default;
        Type = default;
        Flags = default;
        MipLevels = default;
        Layers = default;
        CurrentMipLevel = default;
        CurrentLayer = default;
    }

    public unsafe void Dispose()
    {
        VaContext.Current.Vk.DestroyImageView(VaContext.Current.Device, ImageView, null);
        VaContext.Current.Vk.DestroyImage(VaContext.Current.Device, Image, null);
        VaContext.Current.Vk.FreeMemory(VaContext.Current.Device, Memory, null);
        VaContext.Current.Vk.DestroySampler(VaContext.Current.Device, Sampler, null);
    }
}
