using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VMASharp;

namespace Mocha.Rendering.Vulkan;

internal unsafe partial class VulkanRenderContext : IRenderContext
{
	public bool HasInitialized { get; private set; }
	public bool RenderingActive { get; private set; }

	private bool IsRenderPassActive;

	private IWindow _window = null!;

	[WithProperty] private Vk _vk = null!;
	[WithProperty] private PhysicalDevice _chosenGPU;
	[WithProperty] private Device _device;
	[WithProperty] private SurfaceKHR _surface;
	[WithProperty] private Instance _instance;
	[WithProperty] private QueueFamilyIndices _indices;

	[WithProperty] private KhrSurface _surfaceExtension = null!;
	[WithProperty] private KhrSwapchain _swapchainExtension = null!;

	[WithProperty] private VulkanMemoryAllocator _allocator = null!;

	private Queue _graphicsQueue;
	private Queue _presentQueue;

	[WithProperty] private VulkanCommandContext _mainContext = null!;
	[WithProperty] private VulkanRenderTexture _depthTarget = null!;
	[WithProperty] private VulkanRenderTexture _colorTarget = null!;

#if DEBUG
	string[] _requiredValidationLayers = ["VK_LAYER_KHRONOS_validation"];
#else
	string[] _requiredValidationLayers = [];
#endif

	string[] _deviceExtensions = [KhrSwapchain.ExtensionName];
	string[] _instanceExtensions = [KhrSurface.ExtensionName, ExtDebugUtils.ExtensionName, KhrWin32Surface.ExtensionName];

	public HandleMap<VulkanBuffer> Buffers = new();
	public HandleMap<VulkanImageTexture> ImageTextures = new();
	public HandleMap<VulkanRenderTexture> RenderTextures = new();
	public HandleMap<VulkanDescriptor> Descriptors = new();
	public HandleMap<VulkanShader> Shaders = new();
	public HandleMap<VulkanPipeline> Pipelines = new();

	public DescriptorPool DescriptorPool;
	public uint GraphicsQueueFamily;

	private VulkanPipeline Pipeline = null!;

	public static void VkCheck( Result result )
	{
		if ( result != Result.Success )
		{
			Log.Error( $"VkCheck got {result}" );
			throw new Exception( $"VkCheck got {result}" );
		}
	}

	private uint DebugCallback( DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData )
	{
		Action<object> logAction = messageSeverity switch
		{
			DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => Log.Info,
			DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => Log.Warning,
			DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => Log.Error,
			_ => Log.Trace
		};

		logAction( $"VK: " + Marshal.PtrToStringAnsi( (nint)pCallbackData->PMessage ) );
		return Vk.False;
	}

	private void PopulateDebugMessengerCreateInfo( ref DebugUtilsMessengerCreateInfoEXT createInfo )
	{
		createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
		createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
									 DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
									 DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
		createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
								 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
								 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
		createInfo.PfnUserCallback = (PfnDebugUtilsMessengerCallbackEXT)DebugCallback;
	}

	private uint SwapchainImageIndex = 0;
	private VulkanRenderTexture SwapchainTarget = null!;

	public RenderStatus BeginRendering()
	{
		if ( RenderingActive )
		{
			Log.Error( $"{nameof( BeginRendering )} called more than once before {nameof( EndRendering )}!" );
			return RenderStatus.BeginEndMismatch;
		}

		// _window.Show();

		Vk.WaitForFences( Device, [_mainContext.Fence], true, 1000000000 );
		Vk.ResetFences( Device, [_mainContext.Fence] );

		SwapchainImageIndex = Swapchain.AcquireSwapchainImageIndex( Device, presentSemaphore, _mainContext );
		SwapchainTarget = Swapchain.SwapchainTextures[(int)SwapchainImageIndex];

		var cmd = _mainContext.CommandBuffer;
		var beginInfo = VKInit.CommandBufferBeginInfo( CommandBufferUsageFlags.OneTimeSubmitBit );

		Vk.BeginCommandBuffer( cmd, ref beginInfo );

		Viewport viewport = new()
		{
			MinDepth = 0,
			MaxDepth = 1,
			Width = Swapchain.Extent.Width,
			Height = Swapchain.Extent.Height
		};

		Rect2D scissor = new()
		{
			Extent = Swapchain.Extent,
			Offset = new( 0, 0 )
		};

		Vk.CmdSetScissor( cmd, 0, [scissor] );
		Vk.CmdSetViewport( cmd, 0, [viewport] );

		var writeToColorTargetBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ShaderReadBit, ImageLayout.ShaderReadOnlyOptimal, ImageLayout.ColorAttachmentOptimal, _colorTarget.Image );
		Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.FragmentShaderBit, PipelineStageFlags.ColorAttachmentOutputBit, 0, 0, null, 0, null, [writeToColorTargetBarrier] );

		var colorClear = new ClearValue { Color = new ClearColorValue( 0.0f, 0.0f, 0.0f, 1.0f ) };
		var depthClear = new ClearValue { DepthStencil = new ClearDepthStencilValue( 1.0f, 0 ) };

		var colorAttachmentInfo = VKInit.RenderingAttachmentInfo( _colorTarget.ImageView, ImageLayout.ColorAttachmentOptimal );
		colorAttachmentInfo.ClearValue = colorClear;

		var depthAtachmentInfo = VKInit.RenderingAttachmentInfo( _depthTarget.ImageView, ImageLayout.DepthStencilAttachmentOptimal );
		depthAtachmentInfo.ClearValue = depthClear;

		var fbSize = _window?.FramebufferSize ?? new Vector2Int( 1, 1 );

		var renderInfo = VKInit.RenderingInfo( &colorAttachmentInfo, &depthAtachmentInfo, new Extent2D()
		{
			Width = (uint)fbSize.X,
			Height = (uint)fbSize.Y
		} );

		Vk.CmdBeginRendering( cmd, ref renderInfo );

		RenderingActive = true;
		IsRenderPassActive = true;

		return RenderStatus.Ok;
	}

	public RenderStatus EndRendering()
	{
		if ( !RenderingActive )
		{
			Log.Error( $"{nameof( EndRendering )} called more than once before {nameof( BeginRendering )}!" );
			return RenderStatus.BeginEndMismatch;
		}

		var cmd = _mainContext.CommandBuffer;

		if ( IsRenderPassActive )
		{
			Vk.CmdEndRendering( cmd );
			IsRenderPassActive = false;
		}

		var endRenderBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ColorAttachmentWriteBit, ImageLayout.Undefined, ImageLayout.PresentSrcKhr, SwapchainTarget.Image );
		Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TopOfPipeBit, 0, 0, null, 0, null, [endRenderBarrier] );

		{
			// Source (_colorTarget)
			var srcBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ColorAttachmentWriteBit, ImageLayout.Undefined, ImageLayout.TransferSrcOptimal, _colorTarget.Image );
			Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TopOfPipeBit, 0, 0, null, 0, null, [srcBarrier] );

			// Destination (SwapchainTarget)
			var dstBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ColorAttachmentWriteBit, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, SwapchainTarget.Image );
			Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TopOfPipeBit, 0, 0, null, 0, null, [dstBarrier] );

			ImageCopy imageCopy = new()
			{
				DstOffset = new Offset3D( 0, 0, 0 ),
				DstSubresource = new ImageSubresourceLayers( ImageAspectFlags.ColorBit, 0, 0, 1 ),
				Extent = new Extent3D( SwapchainTarget.Size.Width, SwapchainTarget.Size.Height, 1 ),
				SrcOffset = new Offset3D( 0, 0, 0 ),
				SrcSubresource = new ImageSubresourceLayers( ImageAspectFlags.ColorBit, 0, 0, 1 )
			};

			Vk.CmdCopyImage( cmd, _colorTarget.Image, ImageLayout.TransferSrcOptimal, SwapchainTarget.Image, ImageLayout.TransferDstOptimal, 1, [imageCopy] );

			// Transition source back to shader_read_only
			srcBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ColorAttachmentWriteBit, ImageLayout.TransferSrcOptimal, ImageLayout.ShaderReadOnlyOptimal, _colorTarget.Image );
			Vk.CmdPipelineBarrier( cmd, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TopOfPipeBit, 0, 0, null, 0, null, [srcBarrier] );
		}

		Vk.EndCommandBuffer( cmd );

		var waitStage = PipelineStageFlags.ColorAttachmentOutputBit;
		var submit = VKInit.SubmitInfo( &cmd );

		fixed ( Silk.NET.Vulkan.Semaphore* presentSemaphore = &this.presentSemaphore )
		{
			fixed ( Silk.NET.Vulkan.Semaphore* renderSemaphore = &this.renderSemaphore )
			{
				submit.PWaitDstStageMask = &waitStage;
				submit.WaitSemaphoreCount = 1;
				submit.PWaitSemaphores = presentSemaphore;

				submit.SignalSemaphoreCount = 1;
				submit.PSignalSemaphores = renderSemaphore;

				VkCheck( Vk.QueueSubmit( _graphicsQueue, [submit], _mainContext.Fence ) );

				fixed ( SwapchainKHR* swapchain = &Swapchain.Swapchain )
				{
					var presentInfo = VKInit.PresentInfo( swapchain, renderSemaphore, SwapchainImageIndex );
					SwapchainExtension.QueuePresent( _graphicsQueue, ref presentInfo );
				}
			}
		}

		FrameDeletionQueue.Flush();

		RenderingActive = false;
		return RenderStatus.Ok;
	}
	public RenderStatus CreateRenderTexture( RenderTextureInfo info, out Handle handle )
	{
		handle = Handle.Invalid;

		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var renderTexture = new VulkanRenderTexture( this, info );
		RenderTextures.Add( renderTexture );
		handle = new Handle( (uint)RenderTextures.Count - 1 );

		return RenderStatus.Ok;
	}

	public RenderStatus SetImageTextureData( Handle handle, TextureData textureData )
	{
		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var imageTexture = ImageTextures[(int)handle.Value];
		imageTexture.SetData( textureData );
		return RenderStatus.Ok;
	}

	public RenderStatus CopyImageTexture( Handle handle, TextureCopyData textureCopyData )
	{
		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var imageTexture = ImageTextures[(int)handle.Value];
		imageTexture.Copy( textureCopyData );
		return RenderStatus.Ok;
	}

	public RenderStatus CreateBuffer( BufferInfo info, out Handle handle )
	{
		handle = Handle.Invalid;

		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var buffer = new VulkanBuffer();
		buffer.Create( info );
		Buffers.Add( buffer );
		handle = new Handle( (uint)Buffers.Count - 1 );

		return RenderStatus.Ok;
	}

	public RenderStatus CreateVertexBuffer( BufferInfo info, out Handle handle )
	{
		handle = Handle.Invalid;

		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var vertexBuffer = new VulkanBuffer();
		vertexBuffer.Create( info );
		Buffers.Add( vertexBuffer );
		handle = new Handle( (uint)Buffers.Count - 1 );

		return RenderStatus.Ok;
	}

	public RenderStatus CreateIndexBuffer( BufferInfo info, out Handle handle )
	{
		handle = Handle.Invalid;

		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var indexBuffer = new VulkanBuffer();
		indexBuffer.Create( info );
		Buffers.Add( indexBuffer );
		handle = new Handle( (uint)Buffers.Count - 1 );

		return RenderStatus.Ok;
	}

	public RenderStatus UploadBuffer( Handle handle, BufferUploadInfo uploadInfo )
	{
		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var buffer = Buffers[(int)handle.Value];
		buffer.Upload( uploadInfo );
		return RenderStatus.Ok;
	}

	public RenderStatus Shutdown()
	{
		if ( !HasInitialized )
		{
			Log.Error( $"Can't shut down if {nameof( Startup )} wasn't already called!" );
			return RenderStatus.NotInitialized;
		}

		HasInitialized = false;

		return RenderStatus.Ok;
	}

	private Instance CreateInstanceAndSurface()
	{
		var appInfo = new ApplicationInfo()
		{
			SType = StructureType.ApplicationInfo,
			PApplicationName = (byte*)Marshal.StringToHGlobalAnsi( "Mocha App" ),
			ApplicationVersion = new Version32( 1, 0, 0 ),
			PEngineName = (byte*)Marshal.StringToHGlobalAnsi( "Mocha App Framework" ),
			EngineVersion = new Version32( 1, 0, 0 ),
			ApiVersion = Vk.Version13
		};

		var createInfo = new InstanceCreateInfo()
		{
			SType = StructureType.InstanceCreateInfo,
			PApplicationInfo = &appInfo
		};

		var pExtensions = _window!.VkSurface!.GetRequiredExtensions( out var extensionCount );
		//var extensions = SilkMarshal.PtrToStringArray( pExtensions, extensionCount );
		//extensions = extensions.Concat( _deviceExtensions ).ToArray();

		createInfo.EnabledExtensionCount = (uint)_instanceExtensions.Length;
		createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr( _instanceExtensions );
		createInfo.EnabledLayerCount = (uint)_requiredValidationLayers.Length;
		createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToMemory( _requiredValidationLayers );

		DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
		PopulateDebugMessengerCreateInfo( ref debugCreateInfo );
		createInfo.PNext = &debugCreateInfo;

		VkCheck( _vk!.CreateInstance( ref createInfo, null, out var instance ) );

		Marshal.FreeHGlobal( (IntPtr)appInfo.PApplicationName );
		Marshal.FreeHGlobal( (IntPtr)appInfo.PEngineName );
		SilkMarshal.Free( (nint)createInfo.PpEnabledExtensionNames );
		SilkMarshal.Free( (nint)createInfo.PpEnabledLayerNames );

		if ( !_vk!.TryGetInstanceExtension( instance, out _surfaceExtension ) )
		{
			throw new NotSupportedException( "KHR_surface extension not found." );
		}

		_surface = _window!.VkSurface!.Create<AllocationCallbacks>( instance.ToHandle(), null ).ToSurface();
		_instance = instance;

		return instance;
	}

	private QueueFamilyIndices FindQueueFamilies( PhysicalDevice device )
	{
		var indices = new QueueFamilyIndices();
		uint queueFamilyCount = 0;
		_vk!.GetPhysicalDeviceQueueFamilyProperties( device, ref queueFamilyCount, null );

		var queueFamilies = new QueueFamilyProperties[queueFamilyCount];

		fixed ( QueueFamilyProperties* queueFamiliesPtr = queueFamilies )
		{
			_vk!.GetPhysicalDeviceQueueFamilyProperties( device, ref queueFamilyCount, queueFamiliesPtr );
		}

		uint i = 0;

		foreach ( var queueFamily in queueFamilies )
		{
			if ( queueFamily.QueueFlags.HasFlag( QueueFlags.GraphicsBit ) )
			{
				indices.GraphicsFamily = i;
			}

			_surfaceExtension!.GetPhysicalDeviceSurfaceSupport( device, i, _surface, out var presentSupport );

			if ( presentSupport )
			{
				indices.PresentFamily = i;
			}

			if ( indices.IsComplete )
			{
				break;
			}

			i++;
		}
		return indices;
	}

	private bool CheckDeviceExtensionsSupport( PhysicalDevice device )
	{
		uint extentionsCount = 0;
		_vk!.EnumerateDeviceExtensionProperties( device, (byte*)null, ref extentionsCount, null );

		var availableExtensions = new ExtensionProperties[extentionsCount];
		fixed ( ExtensionProperties* availableExtensionsPtr = availableExtensions )
		{
			_vk!.EnumerateDeviceExtensionProperties( device, (byte*)null, ref extentionsCount, availableExtensionsPtr );
		}

		var availableExtensionNames = availableExtensions.Select( extension => Marshal.PtrToStringAnsi( (IntPtr)extension.ExtensionName ) ).ToHashSet();
		return _deviceExtensions.All( availableExtensionNames.Contains );
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
		_surfaceExtension!.GetPhysicalDeviceSurfaceCapabilities( physicalDevice, _surface, out details.Capabilities );

		uint formatCount = 0;
		_surfaceExtension.GetPhysicalDeviceSurfaceFormats( physicalDevice, _surface, ref formatCount, null );

		if ( formatCount != 0 )
		{
			details.Formats = new SurfaceFormatKHR[formatCount];
			fixed ( SurfaceFormatKHR* formatsPtr = details.Formats )
			{
				_surfaceExtension.GetPhysicalDeviceSurfaceFormats( physicalDevice, _surface, ref formatCount, formatsPtr );
			}
		}
		else
		{
			details.Formats = [];
		}

		uint presentModeCount = 0;
		_surfaceExtension.GetPhysicalDeviceSurfacePresentModes( physicalDevice, _surface, ref presentModeCount, null );

		if ( presentModeCount != 0 )
		{
			details.PresentModes = new PresentModeKHR[presentModeCount];
			fixed ( PresentModeKHR* formatsPtr = details.PresentModes )
			{
				_surfaceExtension.GetPhysicalDeviceSurfacePresentModes( physicalDevice, _surface, ref presentModeCount, formatsPtr );
			}
		}
		else
		{
			details.PresentModes = [];
		}

		return details;
	}

	private bool IsDeviceSuitable( QueueFamilyIndices indices, PhysicalDevice device )
	{
		bool extensionsSupported = CheckDeviceExtensionsSupport( device );
		bool swapChainAdequate = false;

		if ( extensionsSupported )
		{
			var swapChainSupport = QuerySwapChainSupport( device );
			swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
		}

		return indices.IsComplete && extensionsSupported && swapChainAdequate;
	}

	private PhysicalDevice? CreatePhysicalDevice( Instance instance )
	{
		PhysicalDevice? physicalDevice = null;

		uint deviceCount = 0;

		_vk!.EnumeratePhysicalDevices( instance, ref deviceCount, null );

		if ( deviceCount == 0 )
		{
			throw new Exception( "Failed to find GPUs with Vulkan support?" );
		}

		var devices = new PhysicalDevice[deviceCount];
		fixed ( PhysicalDevice* devicesPtr = devices )
		{
			_vk!.EnumeratePhysicalDevices( instance, ref deviceCount, devicesPtr );
		}

		foreach ( var device in devices )
		{
			var indices = FindQueueFamilies( device );

			if ( IsDeviceSuitable( indices, device ) )
			{
				_indices = indices;
				physicalDevice = device;
				break;
			}
		}

		if ( physicalDevice?.Handle == 0 )
		{
			throw new Exception( "Failed to find a suitable GPU?" );
		}

		_chosenGPU = physicalDevice!.Value;

		return physicalDevice;
	}

	private void FinalizeAndCreateDevice( PhysicalDevice physicalDevice )
	{
		var indices = FindQueueFamilies( physicalDevice );

		var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
		uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

		using var mem = GlobalMemory.Allocate( uniqueQueueFamilies.Length * sizeof( DeviceQueueCreateInfo ) );
		var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer( ref mem.GetPinnableReference() );

		float queuePriority = 1.0f;

		for ( int i = 0; i < uniqueQueueFamilies.Length; i++ )
		{
			queueCreateInfos[i] = new()
			{
				SType = StructureType.DeviceQueueCreateInfo,
				QueueFamilyIndex = indices.GraphicsFamily!.Value,
				QueueCount = 1,
				PQueuePriorities = &queuePriority
			};
		}


		PhysicalDeviceVulkan13Features requiredFeatures13 = new()
		{
			SType = StructureType.PhysicalDeviceVulkan13Features,
			PNext = null,
			DynamicRendering = true
		};

		PhysicalDeviceVulkan12Features requiredFeatures12 = new()
		{
			SType = StructureType.PhysicalDeviceVulkan12Features,
			PNext = &requiredFeatures13,
			DescriptorIndexing = true,
			BufferDeviceAddress = true
		};

		PhysicalDeviceVulkan11Features requiredFeatures11 = new()
		{
			SType = StructureType.PhysicalDeviceVulkan11Features,
			PNext = &requiredFeatures12
		};

		PhysicalDeviceFeatures requiredFeatures = new()
		{
			SamplerAnisotropy = true,
		};

		PhysicalDeviceFeatures2 requiredFeatures2 = new()
		{
			SType = StructureType.PhysicalDeviceFeatures2,
			PNext = &requiredFeatures11,
			Features = requiredFeatures,
		};

		DeviceCreateInfo createInfo = new()
		{
			SType = StructureType.DeviceCreateInfo,
			PNext = &requiredFeatures2,
			QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
			PQueueCreateInfos = queueCreateInfos,
			PEnabledFeatures = null,

			EnabledExtensionCount = (uint)_deviceExtensions.Length,
			PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr( _deviceExtensions )
		};

		createInfo.EnabledLayerCount = (uint)_requiredValidationLayers.Length;
		createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr( _requiredValidationLayers );

		VkCheck( _vk!.CreateDevice( physicalDevice, in createInfo, null, out _device ) );

		_vk!.GetDeviceQueue( _device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue );
		_vk!.GetDeviceQueue( _device, indices.PresentFamily!.Value, 0, out _presentQueue );

		GraphicsQueueFamily = indices.GraphicsFamily!.Value;

		if ( !_vk!.TryGetDeviceExtension( Instance, Device, out _swapchainExtension ) )
		{
			throw new NotSupportedException( "KHR_swapchain extension not found." );
		}

		SilkMarshal.Free( (nint)createInfo.PpEnabledLayerNames );
	}

	private void CreateAllocator()
	{
		var allocatorCreateInfo = new VulkanMemoryAllocatorCreateInfo()
		{
			LogicalDevice = _device,
			Instance = _instance,
			PhysicalDevice = _chosenGPU,
			VulkanAPIObject = _vk,
			VulkanAPIVersion = Vk.Version13
		};

		_allocator = new VulkanMemoryAllocator( allocatorCreateInfo );
	}

	private VulkanSampler PointSampler = null!;
	private VulkanSampler AnisoSampler = null!;

	private void CreateSamplers()
	{
		PointSampler = new VulkanSampler( this, SamplerType.Point );
		AnisoSampler = new VulkanSampler( this, SamplerType.Anisotropic );
	}

	private VulkanSwapchain Swapchain = null!;

	private void CreateSwapchain()
	{
		var fbSize = _window.FramebufferSize ?? new Vector2Int( 1, 1 );
		var size = new Size2D( (uint)fbSize.X, (uint)fbSize.Y );

		Swapchain = new VulkanSwapchain( this, size );

		_window.OnResize += () =>
		{
			var fbSize = _window.FramebufferSize ?? new Vector2Int( 1, 1 );
			var size = new Size2D( (uint)fbSize.X, (uint)fbSize.Y );

			Swapchain.Update( size );
			CreateRenderTargets();
		};
	}

	Silk.NET.Vulkan.Semaphore presentSemaphore;
	Silk.NET.Vulkan.Semaphore renderSemaphore;

	private void CreateSyncStructures()
	{
		var semaphoreCreateInfo = new SemaphoreCreateInfo()
		{
			SType = StructureType.SemaphoreCreateInfo,
			PNext = null,
			Flags = 0
		};

		Vk.CreateSemaphore( Device, ref semaphoreCreateInfo, null, out presentSemaphore );
		Vk.CreateSemaphore( Device, ref semaphoreCreateInfo, null, out renderSemaphore );
	}

	private void CreateDescriptors()
	{
		DescriptorPoolSize[] poolSizes = [
			new DescriptorPoolSize( DescriptorType.UniformBuffer, 1000 ),
			new DescriptorPoolSize( DescriptorType.CombinedImageSampler, 1000 ),
			new DescriptorPoolSize( DescriptorType.StorageBuffer, 1000 ),
			new DescriptorPoolSize( DescriptorType.StorageImage, 1000 ),
			new DescriptorPoolSize( DescriptorType.UniformTexelBuffer, 1000 ),
			new DescriptorPoolSize( DescriptorType.StorageTexelBuffer, 1000 ),
			new DescriptorPoolSize( DescriptorType.UniformBufferDynamic, 1000 ),
			new DescriptorPoolSize( DescriptorType.StorageBufferDynamic, 1000 ),
			new DescriptorPoolSize( DescriptorType.InputAttachment, 1000 )
		];

		fixed ( DescriptorPoolSize* poolSizesPtr = poolSizes )
		{
			DescriptorPoolCreateInfo poolInfo = new()
			{
				SType = StructureType.DescriptorPoolCreateInfo,
				PNext = null,
				Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit,
				MaxSets = 1000,
				PoolSizeCount = (uint)poolSizes.Length,
				PPoolSizes = poolSizesPtr
			};

			Vk.CreateDescriptorPool( Device, ref poolInfo, null, out DescriptorPool );
		}
	}

	private VulkanDeletionQueue FrameDeletionQueue = null!;

	private void CreateRenderTargets()
	{
		// Are we re-creating render targets? If so, queue the originals for deletion
		if ( _colorTarget != null && _colorTarget.Image.Handle != 0 )
		{
			var colorTargetCopy = _colorTarget;
			var depthTargetCopy = _depthTarget;

			FrameDeletionQueue.Enqueue( () =>
			{
				colorTargetCopy.Delete();
				depthTargetCopy.Delete();
			} );
		}

		var fbSize = _window.FramebufferSize ?? new Vector2Int( 1, 1 );
		var size = new Size2D( (uint)fbSize.X, (uint)fbSize.Y );

		RenderTextureInfo renderTextureInfo = new()
		{
			Name = "Main render target",
			Width = size.Width,
			Height = size.Height
		};

		renderTextureInfo.Type = RenderTextureType.Depth;
		_depthTarget = new VulkanRenderTexture( this, renderTextureInfo );

		renderTextureInfo.Type = RenderTextureType.Color;
		_colorTarget = new VulkanRenderTexture( this, renderTextureInfo );
	}

	public RenderStatus Startup( IWindow window )
	{
		if ( HasInitialized )
		{
			Log.Error( $"Can't start up if {nameof( Startup )} was already called!" );
			return RenderStatus.AlreadyInitialized;
		}

		_window = window;
		_vk = Vk.GetApi();

		var instance = CreateInstanceAndSurface();
		var physicalDevice = CreatePhysicalDevice( instance );
		FinalizeAndCreateDevice( physicalDevice!.Value );
		CreateAllocator();

		HasInitialized = true;

		FrameDeletionQueue = new();

		CreateCommands();
		CreateSamplers();
		CreateSwapchain();
		CreateSyncStructures();
		CreateDescriptors();
		// CreateImGui();
		CreateRenderTargets();

		window.OnRendererInit();

		return RenderStatus.Ok;
	}

	private void CreateCommands()
	{
		_mainContext = new VulkanCommandContext( this );
	}

	ConcurrentDictionary<int, VulkanCommandContext> uploadContexts = new();
	private VulkanCommandContext GetUploadContext( int threadId )
	{
		VulkanCommandContext? ctx;

		if ( !uploadContexts.TryGetValue( threadId, out ctx ) )
		{
			Log.Trace( $"No upload context for thread {threadId}, spinning up new one" );
			ctx = new VulkanCommandContext( this );
			uploadContexts[threadId] = ctx;
		}

		return ctx;
	}

	public void ImmediateSubmit( Func<CommandBuffer, RenderStatus> func )
	{
		if ( !HasInitialized )
		{
			throw new Exception();
		}

		RenderStatus status;

		VulkanCommandContext context = GetUploadContext( Thread.CurrentThread.ManagedThreadId );
		var cmd = context.CommandBuffer;
		var cmdBeginInfo = VKInit.CommandBufferBeginInfo( CommandBufferUsageFlags.OneTimeSubmitBit );

		VkCheck( Vk.BeginCommandBuffer( cmd, ref cmdBeginInfo ) );

		status = func.Invoke( cmd );

		VkCheck( Vk.EndCommandBuffer( cmd ) );

		var submitInfo = VKInit.SubmitInfo( &cmd );
		VkCheck( Vk.QueueSubmit( _graphicsQueue, [submitInfo], context.Fence ) );

		VkCheck( Vk.WaitForFences( Device, [context.Fence], true, 9999999999 ) );
		VkCheck( Vk.ResetFences( Device, [context.Fence] ) );

		VkCheck( Vk.ResetCommandPool( Device, context.CommandPool, 0 ) );
	}

	public RenderStatus CreateImageTexture( ImageTextureInfo info, out Handle handle )
	{
		handle = Handle.Invalid;

		if ( !HasInitialized )
			return RenderStatus.NotInitialized;

		var imageTexture = new VulkanImageTexture( this, info );
		handle = ImageTextures.Add( imageTexture );

		return RenderStatus.Ok;
	}

	public RenderStatus CreatePipeline( PipelineInfo info, out Handle handle )
	{
		var pipeline = new VulkanPipeline( this, info );
		handle = Pipelines.Add( pipeline );

		return RenderStatus.Ok;
	}

	public RenderStatus CreateDescriptor( DescriptorInfo info, out Handle handle )
	{
		var descriptor = new VulkanDescriptor( this, info );
		handle = Descriptors.Add( descriptor );

		return RenderStatus.Ok;
	}

	public RenderStatus CreateShader( ShaderInfo info, out Handle handle )
	{
		var shader = new VulkanShader( this, info );
		handle = Shaders.Add( shader );

		return RenderStatus.Ok;
	}

	public RenderStatus BindPipeline( Pipeline p )
	{
		var pipeline = Pipelines.Get( p.Handle );
		Pipeline = pipeline;

		Vk.CmdBindPipeline( _mainContext.CommandBuffer, PipelineBindPoint.Graphics, Pipeline.Pipeline );
		return RenderStatus.Ok;
	}

	public RenderStatus BindDescriptor( Descriptor d )
	{
		var descriptor = Descriptors.Get( d.Handle );
		Vk.CmdBindDescriptorSets( _mainContext.CommandBuffer, PipelineBindPoint.Graphics, Pipeline.Layout, 0, 0, ref descriptor.DescriptorSet, 0, null );

		return RenderStatus.Ok;
	}

	public RenderStatus UpdateDescriptor( Descriptor d, DescriptorUpdateInfo updateInfo )
	{
		BindDescriptor( d );

		var descriptor = Descriptors.Get( d.Handle );
		var texture = ImageTextures.Get( updateInfo.Source.Handle );

		DescriptorImageInfo imageInfo = new()
		{
			ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
			ImageView = texture.ImageView,
			Sampler = updateInfo.SamplerType == SamplerType.Anisotropic ? AnisoSampler.Sampler : PointSampler.Sampler
		};

		var descriptorWrite = VKInit.WriteDescriptorImage( DescriptorType.CombinedImageSampler, descriptor.DescriptorSet, [imageInfo], (uint)updateInfo.Binding );

		Vk.UpdateDescriptorSets( Device, [descriptorWrite], [] );

		return RenderStatus.Ok;
	}

	public RenderStatus BindVertexBuffer( VertexBuffer vb )
	{
		var buffer = Buffers.Get( vb.Handle );
		Vk.CmdBindVertexBuffers( _mainContext.CommandBuffer, 0, [buffer.buffer], [0] );

		return RenderStatus.Ok;
	}

	public RenderStatus BindIndexBuffer( IndexBuffer ib )
	{
		var buffer = Buffers.Get( ib.Handle );
		Vk.CmdBindIndexBuffer( _mainContext.CommandBuffer, buffer.buffer, 0, IndexType.Uint32 );

		return RenderStatus.Ok;
	}

	public RenderStatus Draw( int vertexCount, int indexCount, int instanceCount )
	{
		Vk.CmdDrawIndexed( _mainContext.CommandBuffer, (uint)indexCount, (uint)instanceCount, 0, 0, 0 );

		return RenderStatus.Ok;
	}

	public RenderStatus BindRenderTarget( RenderTexture rt )
	{
		if ( IsRenderPassActive )
		{
			Vk.CmdEndRendering( _mainContext.CommandBuffer );
		}

		var renderTexture = RenderTextures.Get( rt.Handle );

		var startImageMemoryBarrier = VKInit.ImageMemoryBarrier( AccessFlags.ColorAttachmentWriteBit, ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal, renderTexture.Image );

		Vk.CmdPipelineBarrier( _mainContext.CommandBuffer, PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.TopOfPipeBit, 0, 0, default, 0, default, 1, ref startImageMemoryBarrier );

		var colorClear = new ClearValue() { Color = new ClearColorValue( 0f, 0f, 0f, 1f ) };
		var depthClear = new ClearValue();
		depthClear.DepthStencil = new ClearDepthStencilValue( 1.0f, 0 );

		var colorAttachmentInfo = VKInit.RenderingAttachmentInfo( renderTexture.ImageView, ImageLayout.ColorAttachmentOptimal );
		colorAttachmentInfo.ClearValue = colorClear;

		var depthAttachmentInfo = VKInit.RenderingAttachmentInfo( _depthTarget.ImageView, ImageLayout.DepthStencilAttachmentOptimal );
		depthAttachmentInfo.ClearValue = depthClear;

		var renderInfo = VKInit.RenderingInfo( &colorAttachmentInfo, &depthAttachmentInfo, new Extent2D( renderTexture.Size.Width, renderTexture.Size.Height ) );
		Vk.CmdBeginRendering( _mainContext.CommandBuffer, ref renderInfo );

		return RenderStatus.Ok;
	}
}
