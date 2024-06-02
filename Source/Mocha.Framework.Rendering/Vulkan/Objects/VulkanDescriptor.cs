using Silk.NET.Vulkan;

namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanDescriptor : VulkanObject
{
	private DescriptorType GetDescriptorType( DescriptorBindingType type )
	{
		switch ( type )
		{
			case DescriptorBindingType.Image:
				return DescriptorType.CombinedImageSampler;
		}

		throw new ArgumentException( "Invalid descriptor type!" );
	}

	public DescriptorSet DescriptorSet;
	public DescriptorSetLayout DescriptorSetLayout;
	public SamplerType SamplerType = SamplerType.Anisotropic;

	public VulkanDescriptor() { }
	public VulkanDescriptor( VulkanRenderContext parent, DescriptorInfo descriptorInfo )
	{
		SetParent( parent );

		var bindings = new List<DescriptorSetLayoutBinding>();

		for ( uint i = 0; i < descriptorInfo.Bindings.Count; ++i )
		{
			var binding = new DescriptorSetLayoutBinding()
			{
				Binding = i,
				DescriptorCount = 1,
				DescriptorType = GetDescriptorType( descriptorInfo.Bindings[(int)i].Type ),
				StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.VertexBit,
				PImmutableSamplers = null
			};

			bindings.Add( binding );
		}

		var layoutInfo = VKInit.DescriptorSetLayoutCreateInfo( [.. bindings] );
		Parent.Vk.CreateDescriptorSetLayout( Parent.Device, ref layoutInfo, null, out DescriptorSetLayout );

		var allocInfo = VKInit.DescriptorSetAllocateInfo( Parent.DescriptorPool, [DescriptorSetLayout] );
		Parent.Vk.AllocateDescriptorSets( Parent.Device, ref allocInfo, out DescriptorSet );

		SetDebugName( descriptorInfo.Name, ObjectType.DescriptorSet, DescriptorSet.Handle );
		SetDebugName( descriptorInfo.Name + " Layout", ObjectType.DescriptorSetLayout, DescriptorSetLayout.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyDescriptorSetLayout( Parent.Device, DescriptorSetLayout, null );
	}
}
