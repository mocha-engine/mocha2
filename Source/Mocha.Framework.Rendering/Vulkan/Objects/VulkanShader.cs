using Silk.NET.Vulkan;

namespace Apparatus.Core.Rendering.Vulkan;

internal unsafe class VulkanShader : VulkanObject
{
	private RenderStatus LoadShaderModule( uint[] shaderData, ShaderType shaderType, out ShaderModule outShaderModule )
	{
		outShaderModule = default;

		ShaderModuleCreateInfo createInfo = new()
		{
			SType = StructureType.ShaderModuleCreateInfo,
			CodeSize = (nuint)shaderData.Length * sizeof( uint ),
		};

		fixed ( uint* codePtr = shaderData )
		{
			createInfo.PCode = codePtr;

			Parent.Vk!.CreateShaderModule( Parent.Device, ref createInfo, null, out outShaderModule );
		}

		return RenderStatus.Ok;
	}

	public ShaderModule VertexShader;
	public ShaderModule FragmentShader;

	public VulkanShader( VulkanRenderContext parent, ShaderInfo shaderInfo )
	{
		SetParent( parent );

		if ( LoadShaderModule( shaderInfo.FragmentData, ShaderType.Fragment, out FragmentShader ) != RenderStatus.Ok )
			throw new Exception( "Failed to compile fragment shader" );
		if ( LoadShaderModule( shaderInfo.VertexData, ShaderType.Vertex, out VertexShader ) != RenderStatus.Ok )
			throw new Exception( "Failed to compile vertex shader" );

		SetDebugName( shaderInfo.Name + " fragment", ObjectType.ShaderModule, FragmentShader.Handle );
		SetDebugName( shaderInfo.Name + " vertex", ObjectType.ShaderModule, VertexShader.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyShaderModule( Parent.Device, FragmentShader, null );
		Parent.Vk.DestroyShaderModule( Parent.Device, VertexShader, null );
	}
}
