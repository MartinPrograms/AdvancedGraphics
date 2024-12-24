using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Common.Other;
using VulkanAbstraction.Common.Other.Other;
using VulkanAbstraction.Globals;
using VulkanAbstraction.Helpers;
using VulkanAbstraction.Helpers.Vulkan.Other;

namespace VulkanAbstraction.Pipelines.DescriptorSets;

public class VaDescriptorSet
{
    
    public DescriptorSet VulkanSet;
    public DescriptorSetLayout VulkanLayout;
    public DescriptorPool DescriptorPool;
    
    public VaDescriptorSet()
    {
        DescriptorPool = DescriptorHelper.CreateDescriptorPool(VaContext.Current.Device);
        VulkanLayout = CreateDescriptorSetLayout();
    }
    
    private unsafe DescriptorSetLayout CreateDescriptorSetLayout()
    {
        // We have a ubo, so we need a descriptor set layout
        DescriptorSetLayoutBinding vboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
            PImmutableSamplers = null
        };
        
        DescriptorSetLayoutBinding indicesLayoutBinding = new()
        {
            Binding = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
            PImmutableSamplers = null
        };
        
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 3,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
            PImmutableSamplers = null
        };
        
        DescriptorSetLayoutBinding fragmentTextureLayoutBinding = new()
        {
            Binding = 5,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = Constants.MaxTextures,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null
        };
        
        DescriptorSetLayoutBinding fragmentSamplerMaterialLayoutBinding = new()
        {
            Binding = 6,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null
        };
        
        DescriptorSetLayoutBinding[] bindings = new[]
        {
            vboLayoutBinding,
            indicesLayoutBinding,
            uboLayoutBinding,
            fragmentTextureLayoutBinding,
            fragmentSamplerMaterialLayoutBinding
        };
        
        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)bindings.Length,   
            PBindings = (DescriptorSetLayoutBinding*)Marshal.UnsafeAddrOfPinnedArrayElement(bindings, 0).ToPointer()
        };
        
        DescriptorSetLayout descriptorSetLayout;
        var result = VaContext.Current.Vk.CreateDescriptorSetLayout(VaContext.Current.Device, &layoutInfo, null, out descriptorSetLayout);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create descriptor set layout: {result}");
        }
        
        VulkanLayout = descriptorSetLayout;
        // Q: how do we get the descriptor set?
        VulkanSet = DescriptorHelper.AllocateDescriptorSet(descriptorSetLayout, DescriptorPool);
        
        AttachMeshBuffers();
        
        return descriptorSetLayout;
    }

    private unsafe void AttachMeshBuffers()
    {
        Device device = VaContext.Current.Device;
        Vk vk = VaContext.Current.Vk;
        
        WriteDescriptorSet[] writes = new WriteDescriptorSet[3];
        DescriptorBufferInfo vertexBufferInfo = new()
        {
            Buffer = MeshManager.GlobalVertexBuffer.Buffer,
            Offset = 0,
            Range = MeshManager.GlobalVertexBuffer.Size
        };
        
        DescriptorBufferInfo indexBufferInfo = new()
        {
            Buffer = MeshManager.GlobalIndexBuffer.Buffer,
            Offset = 0,
            Range = MeshManager.GlobalIndexBuffer.Size
        };
        
        writes[0] = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = VulkanSet,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &vertexBufferInfo
        };
        
        writes[1] = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = VulkanSet,
            DstBinding = 1,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &indexBufferInfo
        };
        
        // Now we need to attach all the textures
        DescriptorImageInfo[] imageInfos = new DescriptorImageInfo[TextureManager.Textures.Count];
        var textures = TextureManager.GetTextures();
        for (var i = 0; i < textures.Length; i++)
        {
            imageInfos[i] = new DescriptorImageInfo
            {
                Sampler = textures[i].Sampler,
                ImageView = textures[i].ImageView,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
            };
        }
        
        writes[2] = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = VulkanSet,
            DstBinding = 5,
            DstArrayElement = 0,
            DescriptorCount = (uint)imageInfos.Length,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImageInfo = (DescriptorImageInfo*)Marshal.UnsafeAddrOfPinnedArrayElement(imageInfos, 0).ToPointer()
        };
        
        vk.UpdateDescriptorSets(device, (uint)writes.Length, writes, 0, (CopyDescriptorSet*)null);
        
        Logger.Info("Attached mesh buffers to descriptor set");
    }

}