using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanSwapchain : VulkanObject
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

	private Extent2D ChooseSwapExtent( Size2D size, SurfaceCapabilitiesKHR capabilities )
	{
		if ( capabilities.CurrentExtent.Width != uint.MaxValue )
		{
			return capabilities.CurrentExtent;
		}
		else
		{
			Extent2D actualExtent = new()
			{
				Width = size.Width,
				Height = size.Height
			};

			actualExtent.Width = Math.Clamp( actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width );
			actualExtent.Height = Math.Clamp( actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height );
			return actualExtent;
		}
	}

	struct SwapchainDetails
	{
		public SurfaceCapabilitiesKHR Capabilities;
		public SurfaceFormatKHR[] Formats;
		public PresentModeKHR[] PresentModes;
	}

	private SwapchainDetails QuerySwapChainSupport( PhysicalDevice physicalDevice )
	{
		var details = new SwapchainDetails();

		Parent.SurfaceExtension.GetPhysicalDeviceSurfaceCapabilities( physicalDevice, Parent.Surface, out details.Capabilities );

		uint formatCount = 0;
		Parent.SurfaceExtension.GetPhysicalDeviceSurfaceFormats( physicalDevice, Parent.Surface, ref formatCount, null );

		if ( formatCount != 0 )
		{
			details.Formats = new SurfaceFormatKHR[formatCount];
			fixed ( SurfaceFormatKHR* formatsPtr = details.Formats )
			{
				Parent.SurfaceExtension.GetPhysicalDeviceSurfaceFormats( physicalDevice, Parent.Surface, ref formatCount, formatsPtr );
			}
		}
		else
		{
			details.Formats = Array.Empty<SurfaceFormatKHR>();
		}

		uint presentModeCount = 0;
		Parent.SurfaceExtension.GetPhysicalDeviceSurfacePresentModes( physicalDevice, Parent.Surface, ref presentModeCount, null );

		if ( presentModeCount != 0 )
		{
			details.PresentModes = new PresentModeKHR[presentModeCount];
			fixed ( PresentModeKHR* formatsPtr = details.PresentModes )
			{
				Parent.SurfaceExtension.GetPhysicalDeviceSurfacePresentModes( physicalDevice, Parent.Surface, ref presentModeCount, formatsPtr );
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
		var extent = ChooseSwapExtent( size, swapChainSupport.Capabilities );
		var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;

		if ( swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount )
		{
			imageCount = swapChainSupport.Capabilities.MaxImageCount;
		}

		SwapchainCreateInfoKHR createInfo = new()
		{
			SType = StructureType.SwapchainCreateInfoKhr,
			Surface = Parent.Surface,
			MinImageCount = imageCount,
			ImageFormat = surfaceFormat.Format,
			ImageColorSpace = surfaceFormat.ColorSpace,
			ImageExtent = extent,
			ImageArrayLayers = 1,
			ImageUsage = ImageUsageFlags.ColorAttachmentBit,
		};

		var indices = Parent.Indices;
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
			OldSwapchain = Swapchain
		};

		Parent.SwapchainExtension.CreateSwapchain( Parent.Device, ref createInfo, null, out Swapchain );
		Parent.SwapchainExtension.GetSwapchainImages( Parent.Device, Swapchain, ref imageCount, null );

		var images = new Image[imageCount];

		fixed ( Image* swapChainImagesPtr = images )
		{
			Parent.SwapchainExtension.GetSwapchainImages( Parent.Device, Swapchain, ref imageCount, swapChainImagesPtr );
		}

		foreach ( var image in images )
		{
			var rt = new VulkanRenderTexture( Parent )
			{
				Image = image,
				Format = surfaceFormat.Format,
				Size = new Size2D( extent.Width, extent.Height )
			};

			var imageCreateInfo = new ImageViewCreateInfo
			{
				SType = StructureType.ImageViewCreateInfo,
				Image = image,
				ViewType = ImageViewType.Type2D,
				Format = surfaceFormat.Format,
				Components = new ComponentMapping
				{
					R = ComponentSwizzle.R,
					G = ComponentSwizzle.G,
					B = ComponentSwizzle.B,
					A = ComponentSwizzle.A
				},
				SubresourceRange = new ImageSubresourceRange
				{
					AspectMask = ImageAspectFlags.ColorBit,
					BaseMipLevel = 0,
					LevelCount = 1,
					BaseArrayLayer = 0,
					LayerCount = 1
				}
			};

			Parent.Vk.CreateImageView( Parent.Device, ref imageCreateInfo, null, out rt.ImageView );

			SwapchainTextures.Add( rt );
		}

		Extent = extent;
	}

	public Extent2D Extent;
	public SwapchainKHR Swapchain;
	public List<VulkanRenderTexture> SwapchainTextures = new();

	public uint AcquireSwapchainImageIndex( Device device, Semaphore presentSemaphore, VulkanCommandContext mainContext )
	{
		uint swapchainImageIndex = 0;

		Parent.SwapchainExtension.AcquireNextImage( Parent.Device, Swapchain, 1000000000, presentSemaphore, default, ref swapchainImageIndex );
		Parent.Vk.ResetCommandBuffer( mainContext.CommandBuffer, CommandBufferResetFlags.None );

		return swapchainImageIndex;
	}

	public VulkanSwapchain( VulkanRenderContext parent, Size2D size )
	{
		SetParent( parent );

		CreateMainSwapchain( size );
	}

	public void Update( Size2D newSize )
	{
		Delete();

		CreateMainSwapchain( newSize );
	}

	public override void Delete()
	{
		foreach ( var texture in SwapchainTextures )
		{
			Parent.Vk.DestroyImageView( Parent.Device, texture.ImageView, null );
		}

		Parent.SwapchainExtension.DestroySwapchain( Parent.Device, Swapchain, null );

		SwapchainTextures.Clear();
	}
}
