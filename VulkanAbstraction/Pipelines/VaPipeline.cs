using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using StupidSimpleLogger;
using VulkanAbstraction.Helpers;
using VulkanAbstraction.Pipelines.DescriptorSets;

namespace VulkanAbstraction.Pipelines;

/// <summary>
/// This is a pipeline, used to render objects to the screen.
/// </summary>
public class VaPipeline
{
    /// <summary>
    /// Each pipeline is identified by a name.
    /// </summary>
    public static Dictionary<string, VaPipeline> Pipelines = new();
    
    public static VaPipeline GetPipeline(string name)
    {
        if (Pipelines.ContainsKey(name))
        {
            return Pipelines[name];
        }
        
        throw new Exception($"Pipeline {name} not found");
        return null;
    }

    
    public string Name { get; set; }
    public Pipeline Pipeline;
    public VaShader Shader { get; set; }
    public PipelineLayout PipelineLayout { get; set; }

    public VaDescriptorSet DescriptorSet { get; set; }
    
    private string VertexPath { get; set; }
    private string FragmentPath { get; set; }

    public unsafe VaPipeline(string name, string vertexPath, string fragmentPath, bool addToPipelines = true)
    {
        Name = name;
        if (addToPipelines)
        Pipelines.Add(name, this);
        
        VertexPath = vertexPath;
        FragmentPath = fragmentPath;
        
        Shader = new VaShader(File.ReadAllText(vertexPath), File.ReadAllText(fragmentPath));
        
        var vertexInfo = VaShader.CreateShaderInfo(ShaderStageFlags.VertexBit, Shader.VertexModule, "main");
        var fragmentInfo = VaShader.CreateShaderInfo(ShaderStageFlags.FragmentBit, Shader.FragmentModule, "main");
        
        var shaderStages = new PipelineShaderStageCreateInfo[]
        {
            vertexInfo,
            fragmentInfo
        };
        
            CreatePipeline(shaderStages);
    }

    private unsafe void CreatePipeline(PipelineShaderStageCreateInfo[] shaderStages)
    {
        Logger.Info("Creating Pipeline", $"Name: {Name}");
        Logger.Info("Creating Pipeline", $"Shader Stages: {shaderStages.Length}");

        DescriptorSet = new VaDescriptorSet(); 
        
        PipelineInputAssemblyStateCreateInfo inputAssemblyState = new()
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
        };
        
        PipelineRasterizationStateCreateInfo rasterizationState = new()
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,   
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false
        };
        
        PipelineMultisampleStateCreateInfo multisampleState = new()
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
            MinSampleShading = 1
        };
        
        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false
        };
        
        PipelineColorBlendStateCreateInfo colorBlendState = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };

        Result result;

        var layout = DescriptorSet.VulkanLayout;
        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &layout
        };

        result =
            VaContext.Current.Vk.CreatePipelineLayout(VaContext.Current.Device, &pipelineLayoutInfo, null,
                out var pipelineLayout);
        
        PipelineLayout = pipelineLayout;
        
        if (result != Result.Success)
        {
            throw new Exception($"Failed to create pipeline layout: {result}");
        }

        fixed (PipelineShaderStageCreateInfo* shaderStagesPtr = &shaderStages[0])
        {
            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 0,
                VertexAttributeDescriptionCount = 0
            };
            
            DynamicState[] dynamicStates = new[]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };

            PipelineDynamicStateCreateInfo dynamicStateInfo = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = (uint)dynamicStates.Length,
                PDynamicStates = (DynamicState*)Marshal.UnsafeAddrOfPinnedArrayElement(dynamicStates, 0).ToPointer()
            };

            PipelineViewportStateCreateInfo viewportState = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                PViewports = null,
                ScissorCount = 1,
                PScissors = null
            };
            
            PipelineDepthStencilStateCreateInfo depthStencilState = new()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };
            
            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = (uint)shaderStages.Length,
                PStages = shaderStagesPtr,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssemblyState,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizationState,
                PMultisampleState = &multisampleState,
                PDepthStencilState = &depthStencilState,
                PColorBlendState = &colorBlendState,
                PDynamicState = &dynamicStateInfo,
                Layout = pipelineLayout,
                RenderPass = VaContext.Current.Swapchain.RenderPass,
                Subpass = 0,
                BasePipelineHandle = default,
                BasePipelineIndex = -1
            };
            
            fixed (Pipeline* a = &this.Pipeline)
                VaContext.Current.Vk.CreateGraphicsPipelines(VaContext.Current.Device, default, 1, &pipelineInfo, null, a);
            
            VaContext.Current.DeletionQueue.Push((c) =>
            {
                c.Vk.DestroyDescriptorSetLayout(c.Device, DescriptorSet.VulkanLayout, null);
                c.Vk.DestroyDescriptorPool(c.Device, DescriptorSet.DescriptorPool, null);
                c.Vk.DestroyPipeline(c.Device, Pipeline, null);
                c.Vk.DestroyPipelineLayout(c.Device, pipelineLayout, null);
            });
        }
    }
    
    public VaPipeline Clone()
    {
        return new VaPipeline(Name, VertexPath, FragmentPath, false);
    }
}