using FlexLayoutSharp;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.InteropServices;
using VMASharp;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Mocha.Rendering.Vulkan;

internal class VulkanObject
{
	protected VulkanRenderContext Parent;

	protected void SetParent( VulkanRenderContext parent )
	{
		Parent = parent;
	}

	protected void SetDebugName( string name, ObjectType objectType, ulong handle )
	{

	}

	public virtual void Delete()
	{
		Log.Warning( $"Delete was called on {GetType().Name} but it hasn't been overridden!" );
	}
}

internal class VulkanSampler : VulkanObject
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

	}
}

internal class VulkanCommandContext : VulkanObject
{
	public CommandPool CommandPool;
	public CommandBuffer CommandBuffer;
	public Fence Fence;

	public VulkanCommandContext() { }
	public VulkanCommandContext( VulkanRenderContext parent ) { }

	public override void Delete()
	{
	}
}

internal unsafe class VulkanRenderTexture : VulkanObject
{
	private ImageUsageFlags GetUsageFlagBits( RenderTextureType type )
	{
		switch ( type )
		{
			case RenderTextureType.Color:
			case RenderTextureType.ColorOpaque:
				return ImageUsageFlags.ColorAttachmentBit;
			case RenderTextureType.Depth:
				return ImageUsageFlags.DepthStencilAttachmentBit;
		}

		throw new ArgumentException( "Invalid render texture type!" );
	}

	private Format GetFormat( RenderTextureType type )
	{
		switch ( type )
		{
			case RenderTextureType.Color:
			case RenderTextureType.ColorOpaque:
				return Format.B8G8R8A8Unorm;
			case RenderTextureType.Depth:
				return Format.D32SfloatS8Uint;
		}

		throw new ArgumentException( "Invalid render texture type!" );
	}

	private ImageAspectFlags GetAspectFlags( RenderTextureType type )
	{
		switch ( type )
		{
			case RenderTextureType.Color:
			case RenderTextureType.ColorOpaque:
				return ImageAspectFlags.ColorBit;
			case RenderTextureType.Depth:
				return ImageAspectFlags.DepthBit;
		}

		throw new ArgumentException( "Invalid render texture type!" );
	}

	public Image Image;
	public Allocation Allocation;
	public ImageView ImageView;
	public Format Format;

	public Size2D Size;

	public VulkanRenderTexture( VulkanRenderContext parent )
	{
		SetParent( parent );
	}

	public VulkanRenderTexture( VulkanRenderContext parent, RenderTextureInfo textureInfo )
	{
		SetParent( parent );

		Size = new Size2D( textureInfo.Width, textureInfo.Height );

		var depthImageExtent = new Extent3D( textureInfo.Width, textureInfo.Height, 1 );
		Format = GetFormat( textureInfo.Type );
		var imageCreateInfo = VKInit.ImageCreateInfo( Format, GetUsageFlagBits( textureInfo.Type ) | ImageUsageFlags.SampledBit, depthImageExtent, 1 );

		AllocationCreateInfo allocInfo = new();
		allocInfo.Usage = MemoryUsage.GPU_Only;
		allocInfo.RequiredFlags = MemoryPropertyFlags.DeviceLocalBit;

		Image = Parent.Allocator.CreateImage( imageCreateInfo, allocInfo, out Allocation );

		ImageViewCreateInfo viewInfo = VKInit.ImageViewCreateInfo( Format, Image, GetAspectFlags( textureInfo.Type ), 1 );
		Parent.Vk.CreateImageView( Parent.Device, viewInfo, null, out ImageView );

		SetDebugName( "RenderTexture Image", ObjectType.Image, Image.Handle );
		SetDebugName( "RenderTexture Image View", ObjectType.ImageView, ImageView.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyImageView( Parent.Device, ImageView, null );
		Allocation.Dispose();
		Parent.Vk.DestroyImage( Parent.Device, Image, null );
	}
}

internal unsafe class VulkanImageTexture : VulkanObject
{
	private DescriptorSet _imGuiDescriptorSet;

	private uint GetBytesPerPixel( Format format )
	{
		switch ( format )
		{
			case Format.R8G8B8A8Srgb:
			case Format.R8G8B8A8Unorm:
				return 4; // 32 bits (4 bytes)
			case Format.BC3SrgbBlock:
			case Format.BC3UnormBlock:
			case Format.BC5UnormBlock:
			case Format.BC5SNormBlock:
				return 1; // 128-bits = 4x4 pixels - 8 bits (1 byte)
			default:
				throw new NotSupportedException( "Format is not supported." );
		}
	}

	private void GetMipDimensions( uint inWidth, uint inHeight, uint mipLevel, out uint outWidth, out uint outHeight )
	{
		outWidth = inWidth >> (int)mipLevel;
		outHeight = inHeight >> (int)mipLevel;
	}

	private uint CalcMipSize( uint inWidth, uint inHeight, uint mipLevel, Format format )
	{
		GetMipDimensions( inWidth, inHeight, mipLevel, out uint outWidth, out uint outHeight );
		if ( format == Format.BC3SrgbBlock || format == Format.BC3UnormBlock ||
			format == Format.BC5UnormBlock || format == Format.BC5SNormBlock )
		{
			outWidth = Math.Max( outWidth, 4 );
			outHeight = Math.Max( outHeight, 4 );
		}

		return outWidth * outHeight * GetBytesPerPixel( format );
	}

	private void TransitionLayout( CommandBuffer cmd, ImageLayout newLayout, AccessFlags newAccessFlags, PipelineStageFlags stageFlags )
	{
		ImageSubresourceRange range = new()
		{
			AspectMask = ImageAspectFlags.ColorBit,
			BaseMipLevel = 0,
			LevelCount = (~0U), // VK_REMAINING_MIP_LEVELS
			BaseArrayLayer = 0,
			LayerCount = 1
		};

		ImageMemoryBarrier barrier = new()
		{
			SType = StructureType.ImageMemoryBarrier,
			OldLayout = CurrentLayout,
			NewLayout = newLayout,
			Image = Image,
			SubresourceRange = range,
			SrcAccessMask = CurrentAccessMask,
			DstAccessMask = newAccessFlags
		};

		Parent.Vk.CmdPipelineBarrier( cmd, CurrentStageMask, stageFlags, 0, 0, null, 0, null, 1, barrier );

		CurrentStageMask = stageFlags;
		CurrentLayout = newLayout;
		CurrentAccessMask = newAccessFlags;
	}

	public AccessFlags CurrentAccessMask = 0;
	public PipelineStageFlags CurrentStageMask = PipelineStageFlags.TopOfPipeBit;
	public ImageLayout CurrentLayout = ImageLayout.Undefined;

	public Image Image;
	public Allocation Allocation;
	public ImageView ImageView;
	public Format Format;

	public ImageTextureInfo TextureInfo;

	public VulkanImageTexture( VulkanRenderContext parent, ImageTextureInfo textureInfo )
	{
		SetParent( parent );

		TextureInfo = textureInfo;
	}

	public void SetData( TextureData textureData )
	{
		// Destroy old image
		Delete();

		Format imageFormat = (Format)textureData.ImageFormat;
		uint imageSize = 0;

		for ( uint i = 0; i < textureData.MipCount; ++i )
		{
			imageSize += CalcMipSize( textureData.Width, textureData.Height, i, imageFormat );
		}

		BufferInfo bufferInfo = new();
		bufferInfo.size = imageSize;
		bufferInfo.Type = BufferType.Staging;
		bufferInfo.Usage = BufferUsageFlags.TransferSrc;

		Handle bufferHandle;
		Parent.CreateBuffer( bufferInfo, out bufferHandle );

		VulkanBuffer stagingBuffer = Parent.Buffers.Get( bufferHandle );

		var mappedData = stagingBuffer.allocation.Map();
		Marshal.Copy( textureData.MipData, 0, mappedData, textureData.MipData.Length );
		stagingBuffer.allocation.Unmap();

		Extent3D imageExtent = new( textureData.Width, textureData.Height, 1 );

		var usageFlags = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit;
		var imageCreateInfo = VKInit.ImageCreateInfo( imageFormat, usageFlags, imageExtent, textureData.MipCount );

		var allocInfo = new AllocationCreateInfo();
		allocInfo.Usage = MemoryUsage.GPU_Only;

		Image = Parent.Allocator.CreateImage( imageCreateInfo, allocInfo, out Allocation );

		Parent.ImmediateSubmit( ( CommandBuffer cmd ) =>
		{
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( 0, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			List<BufferImageCopy> mipRegions = [];

			for ( uint mip = 0; mip < textureData.MipCount; ++mip )
			{
				ulong bufferOffset = 0;

				for ( uint i = 0; i < mip; ++i )
				{
					uint mipWidth, mipHeight;
					GetMipDimensions( textureData.Width, textureData.Height, i, out mipWidth, out mipHeight );
					bufferOffset += CalcMipSize( textureData.Width, textureData.Height, i, imageFormat );
				}

				Extent3D mipExtent;
				GetMipDimensions( textureData.Width, textureData.Height, mip, out mipExtent.Width, out mipExtent.Height );
				mipExtent.Depth = 1;

				BufferImageCopy copyRegion = new();
				copyRegion.BufferOffset = bufferOffset;
				copyRegion.BufferRowLength = 0;
				copyRegion.BufferImageHeight = 0;

				copyRegion.ImageSubresource.AspectMask = ImageAspectFlags.ColorBit;
				copyRegion.ImageSubresource.MipLevel = mip;
				copyRegion.ImageSubresource.BaseArrayLayer = 0;
				copyRegion.ImageSubresource.LayerCount = 1;
				copyRegion.ImageExtent = mipExtent;

				mipRegions.Add( copyRegion );
			}

			Parent.Vk.CmdCopyBufferToImage( cmd, stagingBuffer.buffer, Image, ImageLayout.TransferDstOptimal, mipRegions );

			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.TransferWriteBit, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			return RenderStatus.Ok;
		} );

		ImageViewCreateInfo viewCreateInfo = VKInit.ImageViewCreateInfo( imageFormat, Image, ImageAspectFlags.ColorBit, textureData.MipCount );
		Parent.Vk.CreateImageView( Parent.Device, viewCreateInfo, null, out ImageView );

		SetDebugName( TextureInfo.Name, ObjectType.Image, Image.Handle );
		SetDebugName( TextureInfo.Name + " View", ObjectType.ImageView, ImageView.Handle );
	}

	public void Copy( TextureCopyData copyData )
	{
		Parent.ImmediateSubmit( ( CommandBuffer cmd ) =>
		{
			var src = Parent.ImageTextures.Get( copyData.Source.Handle );

			//
			// Transition source image to VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ShaderReadBit, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferSrcOptimal, src.Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			//
			// Transition destination image to VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ShaderReadBit, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.TransferDstOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			ImageSubresourceLayers srcSubresource = new();
			srcSubresource.AspectMask = ImageAspectFlags.ColorBit;
			srcSubresource.MipLevel = 0;
			srcSubresource.BaseArrayLayer = 0;
			srcSubresource.LayerCount = 1;

			ImageSubresourceLayers dstSubresource = srcSubresource;

			Offset3D srcOffset = new( (int)copyData.SourceX, (int)copyData.SourceY, 0 );
			Offset3D dstOffset = new( (int)copyData.DestX, (int)copyData.DestY, 0 );

			Offset3D extent = new( (int)copyData.Width, (int)copyData.Height, 1 );

			ImageBlit region = new();
			region.SrcSubresource = srcSubresource;
			region.SrcOffsets[0] = srcOffset;
			region.SrcOffsets[1] = new Offset3D( srcOffset.X + (int)copyData.Width, srcOffset.Y + (int)copyData.Height, 1 );
			region.DstSubresource = dstSubresource;
			region.DstOffsets[0] = dstOffset;
			region.DstOffsets[1] = new Offset3D( dstOffset.X + (int)copyData.Width, dstOffset.Y + (int)copyData.Height, 1 );

			Parent.Vk.CmdBlitImage( cmd, src.Image, ImageLayout.TransferSrcOptimal, Image, ImageLayout.TransferDstOptimal, [region], Filter.Nearest );

			//
			// Return images to initial layouts
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.TransferWriteBit, ImageLayout.TransferSrcOptimal, ImageLayout.ShaderReadOnlyOptimal, src.Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			//
			// Transition destination image to VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL
			//
			{
				ImageMemoryBarrier transitionBarrier = VKInit.ImageMemoryBarrier( AccessFlags.TransferWriteBit, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, Image );
				Parent.Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.FragmentShaderBit, 0, 0, null, 0, null, 1, &transitionBarrier );
			}

			return RenderStatus.Ok;
		} );
	}

	public object GetImGuiTextureID()
	{
		// Implementation needed
		return null;
	}

	public override void Delete()
	{
		Parent.Vk.DestroyImageView( Parent.Device, ImageView, null );
		Allocation.Dispose();
		Parent.Vk.DestroyImage( Parent.Device, Image, null );
	}
}

internal class VulkanSwapchain : VulkanObject
{
	private SurfaceFormatKHR ChooseSwapSurfaceFormat( IReadOnlyList<SurfaceFormatKHR> availableFormats )
	{
		foreach ( var availableFormat in availableFormats )
		{
			if ( availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr )
			{
				return availableFormat;
			}
		}

		return availableFormats[0];
	}

	private PresentModeKHR ChoosePresentMode( IReadOnlyList<PresentModeKHR> availablePresentModes )
	{
		foreach ( var availablePresentMode in availablePresentModes )
		{
			if ( availablePresentMode == PresentModeKHR.MailboxKhr )
			{
				return availablePresentMode;
			}
		}

		return PresentModeKHR.FifoKhr;
	}

	private Extent2D ChooseSwapExtent( SurfaceCapabilitiesKHR capabilities )
	{
		if ( capabilities.CurrentExtent.Width != uint.MaxValue )
		{
			return capabilities.CurrentExtent;
		}
		else
		{
			var framebufferSize = Parent._window!.FramebufferSize!.Value;

			Extent2D actualExtent = new()
			{
				Width = (uint)framebufferSize.X,
				Height = (uint)framebufferSize.Y
			};

			actualExtent.Width = Math.Clamp( actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width );
			actualExtent.Height = Math.Clamp( actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height );
			return actualExtent;
		}
	}

	private SwapchainDetails QuerySwapChainSupport( PhysicalDevice physicalDevice )
	{
		var details = new SwapchainDetails();
		_khrSurface!.GetPhysicalDeviceSurfaceCapabilities( physicalDevice, _surface, out details.Capabilities );

		uint formatCount = 0;
		_khrSurface.GetPhysicalDeviceSurfaceFormats( physicalDevice, _surface, ref formatCount, null );

		if ( formatCount != 0 )
		{
			details.Formats = new SurfaceFormatKHR[formatCount];
			fixed ( SurfaceFormatKHR* formatsPtr = details.Formats )
			{
				_khrSurface.GetPhysicalDeviceSurfaceFormats( physicalDevice, _surface, ref formatCount, formatsPtr );
			}
		}
		else
		{
			details.Formats = Array.Empty<SurfaceFormatKHR>();
		}

		uint presentModeCount = 0;
		_khrSurface.GetPhysicalDeviceSurfacePresentModes( physicalDevice, _surface, ref presentModeCount, null );

		if ( presentModeCount != 0 )
		{
			details.PresentModes = new PresentModeKHR[presentModeCount];
			fixed ( PresentModeKHR* formatsPtr = details.PresentModes )
			{
				_khrSurface.GetPhysicalDeviceSurfacePresentModes( physicalDevice, _surface, ref presentModeCount, formatsPtr );
			}
		}
		else
		{
			details.PresentModes = Array.Empty<PresentModeKHR>();
		}

		return details;
	}

	private void CreateMainSwapchain( Size2D size )
	{
		var swapChainSupport = QuerySwapChainSupport( Parent.ChosenGPU );
		var surfaceFormat = ChooseSwapSurfaceFormat( swapChainSupport.Formats );

		var presentMode = ChoosePresentMode( swapChainSupport.PresentModes );
		var extent = ChooseSwapExtent( swapChainSupport.Capabilities );
		var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;

		if ( swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount )
		{
			imageCount = swapChainSupport.Capabilities.MaxImageCount;
		}

		SwapchainCreateInfoKHR createInfo = new()
		{
			SType = StructureType.SwapchainCreateInfoKhr,
			Surface = _surface,
			MinImageCount = imageCount,
			ImageFormat = surfaceFormat.Format,
			ImageColorSpace = surfaceFormat.ColorSpace,
			ImageExtent = extent,
			ImageArrayLayers = 1,
			ImageUsage = ImageUsageFlags.ColorAttachmentBit,
		};

		var indices = FindQueueFamilies( _physicalDevice );
		_graphicsQueueFamily = indices.GraphicsFamily!.Value;

		var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

		if ( indices.GraphicsFamily != indices.PresentFamily )
		{
			createInfo = createInfo with
			{
				ImageSharingMode = SharingMode.Concurrent,
				QueueFamilyIndexCount = 2,
				PQueueFamilyIndices = queueFamilyIndices,
			};
		}
		else
		{
			createInfo.ImageSharingMode = SharingMode.Exclusive;
		}

		createInfo = createInfo with
		{
			PreTransform = swapChainSupport.Capabilities.CurrentTransform,
			CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
			PresentMode = presentMode,
			Clipped = true,
			OldSwapchain = _swapchain
		};

		if ( !Parent.Vk.TryGetDeviceExtension( Parent.Instance, Parent.Device, out Parent.KhrSwapchain ) )
		{
			throw new NotSupportedException( "VK_KHR_swapchain extension not found." );
		}

		Parent.KhrSwapchain.CreateSwapchain( Parent.Device, createInfo, null, out Swapchain );

		Parent.KhrSwapchain.GetSwapchainImages( Parent.Device, Swapchain, ref imageCount, null );
		_swapchainImages = new Image[imageCount];

		fixed ( Image* swapChainImagesPtr = _swapchainImages )
		{
			_khrSwapchain.GetSwapchainImages( _device, _swapchain, ref imageCount, swapChainImagesPtr );
		}

		_swapchainImageFormat = surfaceFormat.Format;
		_swapchainExtent = extent;
	}

	public SwapchainKHR Swapchain = 0;
	public List<VulkanRenderTexture> SwapchainTextures;

	public uint AcquireSwapchainImageIndex( Device device, Semaphore presentSemaphore, VulkanCommandContext mainContext )
	{
		uint swapchainImageIndex = 0;
		// VK_CHECK and implementation needed
		return swapchainImageIndex;
	}

	public VulkanSwapchain( VulkanRenderContext parent, Size2D size )
	{
		SetParent( parent );

		CreateMainSwapchain( size );
	}

	public void Update( Size2D newSize )
	{
		CreateMainSwapchain( newSize );
	}

	public override void Delete()
	{
		// todo
	}
}

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

	public SamplerType SamplerType;

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
				DescriptorType = GetDescriptorType( descriptorInfo.Bindings[i].Type ),
				StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.VertexBit,
				PImmutableSamplers = null
			};

			bindings.Add( binding );
		}

		var layoutInfo = VKInit.DescriptorSetLayoutCreateInfo( [.. bindings] );
		Parent.Vk.CreateDescriptorSetLayout( Parent.Device, layoutInfo, null, out DescriptorSetLayout );

		var allocInfo = VKInit.DescriptorSetAllocateInfo( Parent.DescriptorPool, [DescriptorSetLayout] );
		Parent.Vk.AllocateDescriptorSets( Parent.Device, allocInfo, out DescriptorSet );

		SetDebugName( descriptorInfo.Name, ObjectType.DescriptorSet, DescriptorSet.Handle );
		SetDebugName( descriptorInfo.Name + " Layout", ObjectType.DescriptorSetLayout, DescriptorSetLayout.Handle );
	}

	public override void Delete()
	{
		Parent.Vk.DestroyDescriptorSetLayout( Parent.Device, DescriptorSetLayout, null );
	}
}

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

			Parent.Vk!.CreateShaderModule( Parent.Device, createInfo, null, out outShaderModule ) );
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
