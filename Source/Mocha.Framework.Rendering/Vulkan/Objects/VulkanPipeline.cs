using Silk.NET.Vulkan;

namespace Apparatus.Core.Rendering.Vulkan;

internal unsafe class VulkanPipeline : VulkanObject
{
	private Format GetVulkanFormat( VertexAttributeFormat format )
	{
		switch ( format )
		{
			case VertexAttributeFormat.Int:
				return Format.R32Sint;
			case VertexAttributeFormat.Float:
				return Format.R32Sfloat;
			case VertexAttributeFormat.Float2:
				return Format.R32G32Sfloat;
			case VertexAttributeFormat.Float3:
				return Format.R32G32B32Sfloat;
			case VertexAttributeFormat.Float4:
				return Format.R32G32B32A32Sfloat;
		}

		throw new ArgumentException( "Invalid pipeline format!" );
	}

	private uint GetSizeOf( VertexAttributeFormat format )
	{
		switch ( format )
		{
			case VertexAttributeFormat.Int:
				return sizeof( int );
			case VertexAttributeFormat.Float:
				return sizeof( float );
			case VertexAttributeFormat.Float2:
				return sizeof( float ) * 2;
			case VertexAttributeFormat.Float3:
				return sizeof( float ) * 3;
			case VertexAttributeFormat.Float4:
				return sizeof( float ) * 4;
		}

		throw new ArgumentException( "Invalid pipeline format!" );
	}

	public Silk.NET.Vulkan.Pipeline Pipeline;
	public Silk.NET.Vulkan.PipelineLayout Layout;

	private PipelineInfo _pipelineInfo;

	public VulkanPipeline() { }
	public VulkanPipeline( VulkanRenderContext parent, PipelineInfo pipelineInfo )
	{
		SetParent( parent );

		_pipelineInfo = pipelineInfo;

		Create();
	}

	public void Create()
	{
		if ( Pipeline.Handle != 0 )
		{
			Delete();
		}

		PipelineBuilder builder = new();
		var pipelineLayoutInfo = VKInit.PipelineLayoutCreateInfo();

		/* var pushConstant = new PushConstantRange();
		pushConstant.Offset = 0;
		pushConstant.Size = sizeof( RenderPushConstants );
		pushConstant.StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit;
		*/

		var setLayouts = new List<DescriptorSetLayout>();

		foreach ( var descriptor in _pipelineInfo.Descriptors )
		{
			var vkDescriptor = Parent.Descriptors.Get( descriptor.Handle );
			setLayouts.Add( vkDescriptor.DescriptorSetLayout );
		}

		fixed ( DescriptorSetLayout* setLayoutsPtr = setLayouts.ToArray() )
		{
			pipelineLayoutInfo.PSetLayouts = setLayoutsPtr;
			pipelineLayoutInfo.SetLayoutCount = (uint)setLayouts.Count;

			Parent.Vk.CreatePipelineLayout( Parent.Device, ref pipelineLayoutInfo, null, out builder.PipelineLayout );
			Layout = builder.PipelineLayout;
		}

		builder.Rasterizer = VKInit.PipelineRasterizationStateCreateInfo( PolygonMode.Fill );
		builder.Multisampling = VKInit.PipelineMultisampleStateCreateInfo();
		builder.ColorBlendAttachment = VKInit.PipelineColorBlendAttachmentState();

		var shader = new VulkanShader( Parent, _pipelineInfo.ShaderInfo );
		var shaderStages = new List<PipelineShaderStageCreateInfo>();

		shaderStages.Add( VKInit.PipelineShaderStageCreateInfo( ShaderStageFlags.VertexBit, shader.VertexShader ) );
		shaderStages.Add( VKInit.PipelineShaderStageCreateInfo( ShaderStageFlags.FragmentBit, shader.FragmentShader ) );

		builder.ShaderStages = shaderStages.ToArray();

		uint stride = 0;
		for ( int i = 0; i < _pipelineInfo.VertexAttributes.Count; i++ )
		{
			stride += GetSizeOf( (VertexAttributeFormat)_pipelineInfo.VertexAttributes[i].Format );
		}

		VulkanVertexInputDescription description = new();
		VertexInputBindingDescription mainBinding = new()
		{
			Binding = 0,
			Stride = stride,
			InputRate = VertexInputRate.Vertex
		};

		description.Bindings.Add( mainBinding );

		uint offset = 0;

		for ( int i = 0; i < _pipelineInfo.VertexAttributes.Count; ++i )
		{
			var attribute = _pipelineInfo.VertexAttributes[i];

			VertexInputAttributeDescription positionAttribute = new();
			positionAttribute.Binding = 0;
			positionAttribute.Location = (uint)i;
			positionAttribute.Format = GetVulkanFormat( (VertexAttributeFormat)attribute.Format );
			positionAttribute.Offset = offset;

			description.Attributes.Add( positionAttribute );

			offset += GetSizeOf( (VertexAttributeFormat)attribute.Format );
		}

		fixed ( VertexInputAttributeDescription* attributes = description.Attributes.ToArray() )
		{
			fixed ( VertexInputBindingDescription* bindings = description.Bindings.ToArray() )
			{
				builder.VertexInputInfo = VKInit.PipelineVertexInputStateCreateInfo();
				builder.VertexInputInfo.PVertexAttributeDescriptions = attributes;
				builder.VertexInputInfo.VertexAttributeDescriptionCount = (uint)description.Attributes.Count;

				builder.VertexInputInfo.PVertexBindingDescriptions = bindings;
				builder.VertexInputInfo.VertexBindingDescriptionCount = (uint)description.Bindings.Count;

				builder.InputAssembly = VKInit.PipelineInputAssemblyStateCreateInfo( PrimitiveTopology.TriangleList );
				builder.DepthStencil = VKInit.DepthStencilCreateInfo( !_pipelineInfo.IgnoreDepth, !_pipelineInfo.IgnoreDepth, CompareOp.LessOrEqual );
			}
		}

		if ( _pipelineInfo.RenderToSwapchain )
		{
			Pipeline = builder.Build( Parent, Parent.Device, Format.D32SfloatS8Uint, Parent.ColorTarget );
		}
		else
		{
			Pipeline = builder.Build( Parent, Parent.Device, Parent.DepthTarget.Format, Parent.ColorTarget );
		}
	}

	public override void Delete()
	{
		Parent.Vk.DestroyPipeline( Parent.Device, Pipeline, null );
		Parent.Vk.DestroyPipelineLayout( Parent.Device, Layout, null );
	}
}
