using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Mocha.Rendering.Vulkan;

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
