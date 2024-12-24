using System.Runtime.CompilerServices;
using Common;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;
using VulkanAbstraction.Helpers;
using VulkanAbstraction.Helpers.Other;

namespace VulkanAbstraction.Pipelines;

public class VaShader
{
    public ShaderModule VertexModule;
    public ShaderModule FragmentModule;
    
    /// <summary>
    /// Expects the source code for the vertex and fragment shaders. in GLSL.
    /// </summary>
    /// <param name="vertexSource"></param>
    /// <param name="fragmentSource"></param>
    public unsafe VaShader(string vertexSource, string fragmentSource)
    {
        VertexModule = CreateShaderModule(vertexSource, ShaderKind.VertexShader);
        FragmentModule = CreateShaderModule(fragmentSource, ShaderKind.FragmentShader);

        VaContext.Current.DeletionQueue.Push((c) =>
        {
            c.Vk.DestroyShaderModule(c.Device, VertexModule, null);
            c.Vk.DestroyShaderModule(c.Device, FragmentModule, null);
        });
    }
    
    private ShaderModule CreateShaderModule(string source, ShaderKind kind)
    {
        return ShaderHelper.CreateShaderModule(VaContext.Current!.Device, ShaderHelper.CompileShader(source, kind));
    }
    
    public static unsafe PipelineShaderStageCreateInfo CreateShaderInfo(ShaderStageFlags flags, ShaderModule module, string name)
    {
        return new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = flags,
            Module = module,
            PName = name.ToPointer()
        };
    }
}