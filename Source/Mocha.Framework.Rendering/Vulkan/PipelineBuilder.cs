using Silk.NET.Vulkan;

namespace Mocha.Rendering.Vulkan;

internal unsafe class PipelineBuilder
{
	public PipelineShaderStageCreateInfo[] ShaderStages = [];
	public PipelineVertexInputStateCreateInfo VertexInputInfo = new();
	public PipelineInputAssemblyStateCreateInfo InputAssembly = new();

	public PipelineRasterizationStateCreateInfo Rasterizer = new();
	public PipelineColorBlendAttachmentState ColorBlendAttachment = new();
	public PipelineMultisampleStateCreateInfo Multisampling = new();
	public PipelineLayout PipelineLayout = new();

	public PipelineDepthStencilStateCreateInfo DepthStencil = new();

	public Silk.NET.Vulkan.Pipeline Build( VulkanRenderContext Parent, Device device, Format depthFormat, Format colorFormat )
	{
		PipelineViewportStateCreateInfo viewportState = new()
		{
			PNext = null,
			SType = StructureType.PipelineViewportStateCreateInfo
		};

		Viewport viewport = new()
		{
			X = 0,
			Y = 0,
			Width = 1280,
			Height = 720,
			MinDepth = 0,
			MaxDepth = 1
		};

		Rect2D scissor = new( new( 0, 0 ), new( 1280, 720 ) );

		viewportState.ViewportCount = 1;
		viewportState.PViewports = &viewport;
		viewportState.ScissorCount = 1;
		viewportState.PScissors = &scissor;

		PipelineDynamicStateCreateInfo dynamicStateInfo = new();
		DynamicState[] dynamicStates = [DynamicState.Viewport, DynamicState.Scissor];

		fixed ( DynamicState* dynamicStatesPtr = dynamicStates )
		{
			dynamicStateInfo.SType = StructureType.PipelineDynamicStateCreateInfo;
			dynamicStateInfo.PNext = null;
			dynamicStateInfo.DynamicStateCount = 2;
			dynamicStateInfo.PDynamicStates = dynamicStatesPtr;

			var colorBlendAttachment = ColorBlendAttachment;
			PipelineColorBlendStateCreateInfo colorBlending = new()
			{
				SType = StructureType.PipelineColorBlendStateCreateInfo,
				PNext = null,

				LogicOpEnable = false,
				LogicOp = LogicOp.Copy,

				AttachmentCount = 1,
				PAttachments = &colorBlendAttachment
			};

			PipelineRenderingCreateInfo pipelineCreate = new()
			{
				SType = StructureType.PipelineRenderingCreateInfo,
				PNext = null,
				ColorAttachmentCount = 1,
				PColorAttachmentFormats = &colorFormat,
				DepthAttachmentFormat = depthFormat,
				StencilAttachmentFormat = depthFormat
			};

			var shaderStages = ShaderStages;
			var vertexInputInfo = VertexInputInfo;
			var inputAssembly = InputAssembly;
			var rasterizer = Rasterizer;
			var multisampling = Multisampling;
			var depthStencil = DepthStencil;

			fixed ( PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages )
			{
				GraphicsPipelineCreateInfo pipelineInfo = new()
				{
					SType = StructureType.GraphicsPipelineCreateInfo,
					PNext = &pipelineCreate,

					StageCount = (uint)ShaderStages.Length,
					PStages = shaderStagesPtr,
					PVertexInputState = &vertexInputInfo,
					PInputAssemblyState = &inputAssembly,
					PViewportState = &viewportState,
					PRasterizationState = &rasterizer,
					PMultisampleState = &multisampling,
					PColorBlendState = &colorBlending,
					PDepthStencilState = &depthStencil,
					PDynamicState = &dynamicStateInfo,
					Layout = PipelineLayout,
					Subpass = 0,
					BasePipelineHandle = default,
					RenderPass = default
				};

				Parent.Vk.CreateGraphicsPipelines( device, default, 1, ref pipelineInfo, null, out var newPipeline );

				return newPipeline;
			}
		}
	}
}
