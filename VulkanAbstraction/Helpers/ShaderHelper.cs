using System.Runtime.InteropServices;
using Common;
using Silk.NET.Vulkan;
using Silk.NET.Shaderc;
using VulkanAbstraction.Common;

namespace VulkanAbstraction.Helpers;

/// <summary>
/// This class takes in a GLSL shader and compiles it to SPIR-V.
/// And also has an option to create a vulkan shader module.
/// </summary>
public class ShaderHelper
{
    public static unsafe byte[] CompileShader(string source, ShaderKind kind)
    {
        var shaderc = Shaderc.GetApi();
        var compiler = shaderc.CompilerInitialize();
        var compileOptions = shaderc.CompileOptionsInitialize();
        
        shaderc.CompileOptionsSetOptimizationLevel(compileOptions, OptimizationLevel.Performance);
        shaderc.CompileOptionsSetSourceLanguage(compileOptions, SourceLanguage.Glsl);
        shaderc.CompileOptionsSetTargetEnv(compileOptions, TargetEnv.Vulkan, new VaVulkanVersion(1,0,0));
        shaderc.CompileOptionsSetTargetSpirv(compileOptions, SpirvVersion.Shaderc10);
        
        File.WriteAllText("cache.glsl", source); // because shaderc is stupid and doesn't accept strings
        var result = shaderc.CompileIntoSpv(compiler, source.ToPointer(), new UIntPtr((uint)source.Length), kind, "cache.glsl".ToPointer(),
            "main".ToPointer(), compileOptions);
        
        var resultStatus = shaderc.ResultGetCompilationStatus(result);
        if (resultStatus != CompilationStatus.Success)
        {
            var error = shaderc.ResultGetErrorMessage(result);
            throw new Exception($"Failed to compile shader: {UnsafeExtensions.ToString(error)}");
        }
        
        var bytes = shaderc.ResultGetBytes(result);
        var length = (int)shaderc.ResultGetLength(result);
        var output = new byte[length];
        for (var i = 0; i < length; i++)
        {
            output[i] = bytes[i];
        }
        
        shaderc.ResultRelease(result);
        shaderc.CompilerRelease(compiler);
        shaderc.CompileOptionsRelease(compileOptions);
        return output;
    }
    
    public static unsafe ShaderModule CreateShaderModule(Device device, byte[] shader)
    {
        var vk = VaContext.Current.Vk;
        if (vk == null)
        {
            throw new Exception("Vulkan is not initialized");
        }
        
        var createInfo = new ShaderModuleCreateInfo();
        createInfo.SType = StructureType.ShaderModuleCreateInfo;
        createInfo.CodeSize = new UIntPtr((uint)shader.Length);
        createInfo.PCode = (uint*)Marshal.UnsafeAddrOfPinnedArrayElement(shader, 0).ToPointer();
        
        var result = vk.CreateShaderModule(device, createInfo, null, out var module);
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create shader module: {result}");
        }
        
        return module;
    }
}