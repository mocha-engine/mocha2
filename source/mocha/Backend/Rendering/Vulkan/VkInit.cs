using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

internal static unsafe class VKInit
{
	public static PipelineShaderStageCreateInfo PipelineShaderStageCreateInfo( ShaderStageFlags stage, ShaderModule shaderModule )
	{
		return new PipelineShaderStageCreateInfo
		{
			SType = StructureType.PipelineShaderStageCreateInfo,
			Stage = stage,
			Module = shaderModule,
			PName = (byte*)SilkMarshal.StringToPtr( "main" )
		};
	}

	public static PipelineVertexInputStateCreateInfo PipelineVertexInputStateCreateInfo()
	{
		return new PipelineVertexInputStateCreateInfo
		{
			SType = StructureType.PipelineVertexInputStateCreateInfo
		};
	}

	public static PipelineInputAssemblyStateCreateInfo PipelineInputAssemblyStateCreateInfo( PrimitiveTopology topology )
	{
		return new PipelineInputAssemblyStateCreateInfo
		{
			SType = StructureType.PipelineInputAssemblyStateCreateInfo,
			Topology = topology,
			PrimitiveRestartEnable = Vk.False
		};
	}

	public static PipelineRasterizationStateCreateInfo PipelineRasterizationStateCreateInfo( PolygonMode polygonMode )
	{
		return new PipelineRasterizationStateCreateInfo
		{
			SType = StructureType.PipelineRasterizationStateCreateInfo,
			DepthClampEnable = Vk.False,
			RasterizerDiscardEnable = Vk.False,
			PolygonMode = polygonMode,
			LineWidth = 1.0f,
			CullMode = CullModeFlags.BackBit,
			FrontFace = FrontFace.Clockwise,
			DepthBiasEnable = Vk.False
		};
	}

	public static PipelineMultisampleStateCreateInfo PipelineMultisampleStateCreateInfo()
	{
		return new PipelineMultisampleStateCreateInfo
		{
			SType = StructureType.PipelineMultisampleStateCreateInfo,
			SampleShadingEnable = Vk.False,
			RasterizationSamples = SampleCountFlags.Count1Bit
		};
	}

	public static PipelineColorBlendAttachmentState PipelineColorBlendAttachmentState()
	{
		return new PipelineColorBlendAttachmentState
		{
			ColorWriteMask = ColorComponentFlags.RBit |
							 ColorComponentFlags.GBit |
							 ColorComponentFlags.BBit |
							 ColorComponentFlags.ABit,
			BlendEnable = Vk.True,
			SrcColorBlendFactor = BlendFactor.SrcAlpha,
			DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
			ColorBlendOp = BlendOp.Add,
			SrcAlphaBlendFactor = BlendFactor.One,
			DstAlphaBlendFactor = BlendFactor.Zero,
			AlphaBlendOp = BlendOp.Add
		};
	}

	public static PipelineLayoutCreateInfo PipelineLayoutCreateInfo()
	{
		return new PipelineLayoutCreateInfo
		{
			SType = StructureType.PipelineLayoutCreateInfo
		};
	}

	public static RenderingAttachmentInfo RenderingAttachmentInfo( ImageView imageView, ImageLayout imageLayout )
	{
		RenderingAttachmentInfo renderingAttachmentInfo = new();
		renderingAttachmentInfo.SType = StructureType.RenderingAttachmentInfo;
		renderingAttachmentInfo.PNext = null;

		renderingAttachmentInfo.ImageLayout = imageLayout;
		renderingAttachmentInfo.ResolveMode = ResolveModeFlags.None;
		renderingAttachmentInfo.LoadOp = AttachmentLoadOp.Clear;
		renderingAttachmentInfo.StoreOp = AttachmentStoreOp.Store;
		renderingAttachmentInfo.ImageView = imageView;

		return renderingAttachmentInfo;
	}

	public static RenderingInfo RenderingInfo( RenderingAttachmentInfo* colorAttachmentInfo, RenderingAttachmentInfo* depthAttachmentInfo, Extent2D extent )
	{
		var renderInfo = new RenderingInfo
		{
			SType = StructureType.RenderingInfo,
			LayerCount = 1,
			RenderArea = new Rect2D { Offset = new Offset2D(), Extent = extent },
			ColorAttachmentCount = 1,
			PColorAttachments = colorAttachmentInfo,
			PDepthAttachment = depthAttachmentInfo,
			PStencilAttachment = depthAttachmentInfo
		};

		// Note: Directly pointing to structs like this requires unsafe code block or fixed size buffers.
		// Alternatively, use managed arrays or collections where applicable in Silk.NET.

		return renderInfo;
	}

	public static SubmitInfo SubmitInfo( CommandBuffer* commandBuffer )
	{
		var submitInfo = new SubmitInfo
		{
			SType = StructureType.SubmitInfo,
			CommandBufferCount = 1,
			PCommandBuffers = commandBuffer
		};

		return submitInfo;
	}

	public static PresentInfoKHR PresentInfo( SwapchainKHR* swapchain, Semaphore* waitSemaphore, uint imageIndex )
	{
		var presentInfo = new PresentInfoKHR
		{
			SType = StructureType.PresentInfoKhr,
			SwapchainCount = 1,
			PSwapchains = swapchain,
			PWaitSemaphores = waitSemaphore,
			WaitSemaphoreCount = 1,
			PImageIndices = &imageIndex
		};

		return presentInfo;
	}

	public static CommandBufferBeginInfo CommandBufferBeginInfo( CommandBufferUsageFlags flags )
	{
		return new CommandBufferBeginInfo
		{
			SType = StructureType.CommandBufferBeginInfo,
			Flags = flags
		};
	}

	public static ImageCreateInfo ImageCreateInfo( Format format, ImageUsageFlags usageFlags, Extent3D extent, uint mipLevels, SampleCountFlags sampleCount = SampleCountFlags.Count1Bit )
	{
		return new ImageCreateInfo
		{
			SType = StructureType.ImageCreateInfo,
			ImageType = ImageType.Type2D,
			Format = format,
			Extent = extent,
			MipLevels = mipLevels,
			ArrayLayers = 1,
			Samples = sampleCount,
			Tiling = ImageTiling.Optimal,
			Usage = usageFlags,
			SharingMode = SharingMode.Exclusive,
			InitialLayout = ImageLayout.Undefined
		};
	}

	public static ImageViewCreateInfo ImageViewCreateInfo( Format format, Image image, ImageAspectFlags aspectFlags, uint mipLevels )
	{
		return new ImageViewCreateInfo
		{
			SType = StructureType.ImageViewCreateInfo,
			Image = image,
			ViewType = ImageViewType.Type2D,
			Format = format,
			SubresourceRange = new ImageSubresourceRange
			{
				AspectMask = aspectFlags,
				BaseMipLevel = 0,
				LevelCount = mipLevels,
				BaseArrayLayer = 0,
				LayerCount = 1
			}
		};
	}

	public static PipelineDepthStencilStateCreateInfo DepthStencilCreateInfo( bool depthTest, bool depthWrite, CompareOp compareOp )
	{
		return new PipelineDepthStencilStateCreateInfo
		{
			SType = StructureType.PipelineDepthStencilStateCreateInfo,
			DepthTestEnable = depthTest ? Vk.True : Vk.False,
			DepthWriteEnable = depthWrite ? Vk.True : Vk.False,
			DepthCompareOp = depthTest ? compareOp : CompareOp.Always,
			DepthBoundsTestEnable = Vk.False,
			StencilTestEnable = Vk.False
		};
	}

	public static ImageMemoryBarrier ImageMemoryBarrier( AccessFlags srcAccessMask, ImageLayout oldLayout, ImageLayout newLayout, Image image )
	{
		return new ImageMemoryBarrier
		{
			SType = StructureType.ImageMemoryBarrier,
			SrcAccessMask = srcAccessMask,
			OldLayout = oldLayout,
			NewLayout = newLayout,
			Image = image,
			SubresourceRange = new ImageSubresourceRange
			{
				AspectMask = ImageAspectFlags.ColorBit,
				BaseMipLevel = 0,
				LevelCount = Vk.RemainingMipLevels,
				BaseArrayLayer = 0,
				LayerCount = Vk.RemainingArrayLayers
			}
		};
	}

	public static DescriptorSetLayoutCreateInfo DescriptorSetLayoutCreateInfo( DescriptorSetLayoutBinding[] bindings )
	{
		fixed ( DescriptorSetLayoutBinding* pBindings = bindings )
		{
			return new DescriptorSetLayoutCreateInfo
			{
				SType = StructureType.DescriptorSetLayoutCreateInfo,
				BindingCount = (uint)bindings.Length,
				PBindings = pBindings // Requires fixed or pinned
			};
		}
	}

	public static DescriptorSetAllocateInfo DescriptorSetAllocateInfo( DescriptorPool descriptorPool, DescriptorSetLayout[] descriptorSetLayouts )
	{
		fixed ( DescriptorSetLayout* pSetLayouts = descriptorSetLayouts )
		{
			return new DescriptorSetAllocateInfo
			{
				SType = StructureType.DescriptorSetAllocateInfo,
				DescriptorPool = descriptorPool,
				DescriptorSetCount = (uint)descriptorSetLayouts.Length,
				PSetLayouts = pSetLayouts // Requires fixed or pinned
			};
		}
	}

	public static SamplerCreateInfo SamplerCreateInfo( Filter filters, SamplerAddressMode samplerAddressMode, bool anisotropyEnabled = false )
	{
		return new SamplerCreateInfo
		{
			SType = StructureType.SamplerCreateInfo,
			MagFilter = filters,
			MinFilter = filters,
			AddressModeU = samplerAddressMode,
			AddressModeV = samplerAddressMode,
			AddressModeW = samplerAddressMode,
			AnisotropyEnable = anisotropyEnabled ? Vk.True : Vk.False,
			MaxAnisotropy = anisotropyEnabled ? 16.0f : 0.0f,
			MipmapMode = SamplerMipmapMode.Nearest,
			MinLod = 0.0f,
			MaxLod = 5.0f
		};
	}

	public static WriteDescriptorSet WriteDescriptorImage( DescriptorType type, DescriptorSet dstSet, DescriptorImageInfo[] imageInfo, uint binding )
	{
		fixed ( DescriptorImageInfo* pImageInfo = imageInfo )
		{
			return new WriteDescriptorSet
			{
				SType = StructureType.WriteDescriptorSet,
				DstBinding = binding,
				DstSet = dstSet,
				DescriptorCount = 1,
				DescriptorType = type,
				PImageInfo = pImageInfo // Requires fixed or pinned
			};
		}
	}

	// Note: The use of fixed or pinned pointers requires the `unsafe` keyword and context in C#.
	// This is necessary for handling direct memory locations, which is common in Vulkan and Silk.NET's low-level operations.
	// For safer and higher-level abstractions, consider using managed arrays or collections and adapt the API usage accordingly.
}
