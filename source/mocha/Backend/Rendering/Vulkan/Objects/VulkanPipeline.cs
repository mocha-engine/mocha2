using Silk.NET.Vulkan;

namespace Mocha.Rendering.Vulkan;

internal class VulkanPipeline : VulkanObject
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

	public Pipeline Pipeline;
	public PipelineLayout Layout;

	public VulkanPipeline() { }
	public VulkanPipeline( VulkanRenderContext parent, PipelineInfo pipelineInfo )
	{
		SetParent( parent );
	}

	public override void Delete()
	{
	}
}
