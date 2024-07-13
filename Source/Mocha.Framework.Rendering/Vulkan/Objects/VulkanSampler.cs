using Silk.NET.Vulkan;

namespace Apparatus.Core.Rendering.Vulkan;

internal unsafe class VulkanSampler : VulkanObject
{
	private SamplerCreateInfo GetCreateInfo( SamplerType samplerType )
	{
		if ( samplerType == SamplerType.Point )
			return VKInit.SamplerCreateInfo( Filter.Nearest, SamplerAddressMode.Repeat, false );
		if ( samplerType == SamplerType.Linear )
			return VKInit.SamplerCreateInfo( Filter.Linear, SamplerAddressMode.Repeat, false );
		if ( samplerType == SamplerType.Anisotropic )
			return VKInit.SamplerCreateInfo( Filter.Linear, SamplerAddressMode.Repeat, true );

		throw new ArgumentException( "Invalid sampler type!" );
	}

	public Sampler Sampler;

	public VulkanSampler( VulkanRenderContext parent, SamplerType samplerType )
	{
		SetParent( parent );

		var createInfo = GetCreateInfo( samplerType );
		Parent.Vk.CreateSampler( Parent.Device, ref createInfo, null, out Sampler );
	}
}
