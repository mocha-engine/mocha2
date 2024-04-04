using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Mocha.Rendering.Vulkan;

internal struct SwapchainDetails
{
	public SurfaceCapabilitiesKHR Capabilities;
	public SurfaceFormatKHR[] Formats;
	public PresentModeKHR[] PresentModes;
}

internal unsafe class VulkanBackend : IRenderingBackend
{
	private Window? _window;

	private KhrSurface? _khrSurface;
	private SurfaceKHR _surface;

	private Instance _instance;

	private ExtDebugUtils? _debugUtils;
	private DebugUtilsMessengerEXT _debugMessenger;

	private PhysicalDevice _physicalDevice;

	private Device _device;
	private Queue _graphicsQueue;
	private Queue _presentQueue;

	private Vk? _vk;

	private const int MaxFramesInFlight = 2;

	private Framebuffer[]? _swapchainFramebuffers;

	private RenderPass _renderPass;
	private PipelineLayout _pipelineLayout;
	private Pipeline _graphicsPipeline;

	private CommandPool _commandPool;
	private CommandBuffer[]? _commandBuffers;

	private Semaphore[]? _imageAvailableSemaphores;
	private Semaphore[]? _renderFinishedSemaphores;
	private Fence[]? _inFlightFences;
	private Fence[]? _imagesInFlight;
	private int _currentFrame = 0;

	private Buffer _vertexBuffer;
	private DeviceMemory _vertexBufferMemory;

	private Buffer _indexBuffer;
	private DeviceMemory _indexBufferMemory;

	// Rectangle
	private Vertex[] _vertices = new Vertex[]
	{
		new Vertex{ Position = new Vector2(-0.5f, -0.5f), Color = new Vector3(1.0f, 0.0f, 0.0f) },
		new Vertex{ Position = new Vector2(0.5f, -0.5f), Color = new Vector3(0.0f, 1.0f, 0.0f) },
		new Vertex{ Position = new Vector2(0.5f, 0.5f), Color = new Vector3(0.0f, 0.0f, 1.0f) },
		new Vertex{ Position = new Vector2(-0.5f, 0.5f), Color = new Vector3(1.0f, 1.0f, 0.0f) }
	};

	private uint[] _indices = new uint[]
	{
		0, 1, 2,
		2, 3, 0
	};

	private readonly string[] _requiredValidationLayers = new[]
	{
		"VK_LAYER_KHRONOS_validation"
	};

	private readonly string[] deviceExtensions = new[]
	{
		KhrSwapchain.ExtensionName
	};

	private KhrSwapchain? _khrSwapchain;
	private SwapchainKHR _swapchain;
	private Image[]? _swapchainImages;
	private ImageView[]? _swapchainImageViews;
	private Format _swapchainImageFormat;
	private Extent2D _swapchainExtent;

	public VulkanBackend( Window window )
	{
		if ( window.VkSurface is null )
		{
			throw new Exception( "Window has no Vulkan surface" );
		}

		IRenderingBackend.Current = this;

		_window = window;
		_vk = Vk.GetApi();

		InitInstance();
		InitSurface();

		InitPhysicalDevice();
		InitDevice();

		InitSwapchain();
		InitImageViews();

		InitRenderPass();
		InitGraphicsPipeline();
		InitFramebuffers();

		InitVertexBuffer();
		InitIndexBuffer();

		InitCommandPool();
		InitCommandBuffers();

		InitSyncObjects();

		_window!.Render += DrawFrame;

		Log.Trace( "Initialized Vulkan" );

		_window!.OnRendererInit();
	}

	public void Render()
	{
		_vk!.DeviceWaitIdle( _device );
	}

	private void VkCheck( Result result )
	{
		if ( result != Result.Success )
		{
			Log.Error( $"VkCheck got {result}" );
			throw new Exception( $"VkCheck got {result}" );
		}
	}

	private void InitInstance()
	{
		var appInfo = new ApplicationInfo()
		{
			SType = StructureType.ApplicationInfo,
			PApplicationName = (byte*)Marshal.StringToHGlobalAnsi( "Frappe App" ),
			ApplicationVersion = new Version32( 1, 0, 0 ),
			PEngineName = (byte*)Marshal.StringToHGlobalAnsi( "Frappe App Framework" ),
			EngineVersion = new Version32( 1, 0, 0 ),
			ApiVersion = Vk.Version13
		};

		var createInfo = new InstanceCreateInfo()
		{
			SType = StructureType.InstanceCreateInfo,
			PApplicationInfo = &appInfo
		};

		var extensions = _window!.VkSurface!.GetRequiredExtensions( out var extensionCount );

		createInfo.EnabledExtensionCount = extensionCount;
		createInfo.PpEnabledExtensionNames = extensions;
		createInfo.EnabledLayerCount = (uint)_requiredValidationLayers.Length;
		createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToMemory( _requiredValidationLayers );

		DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
		PopulateDebugMessengerCreateInfo( ref debugCreateInfo );
		createInfo.PNext = &debugCreateInfo;

		VkCheck( _vk!.CreateInstance( createInfo, null, out _instance ) );

		Marshal.FreeHGlobal( (IntPtr)appInfo.PApplicationName );
		Marshal.FreeHGlobal( (IntPtr)appInfo.PEngineName );
		SilkMarshal.Free( (nint)createInfo.PpEnabledExtensionNames );
		SilkMarshal.Free( (nint)createInfo.PpEnabledLayerNames );
	}

	private void InitPhysicalDevice()
	{
		uint deviceCount = 0;

		_vk!.EnumeratePhysicalDevices( _instance, ref deviceCount, null );

		if ( deviceCount == 0 )
		{
			throw new Exception( "Failed to find GPUs with Vulkan support?" );
		}

		var devices = new PhysicalDevice[deviceCount];
		fixed ( PhysicalDevice* devicesPtr = devices )
		{
			_vk!.EnumeratePhysicalDevices( _instance, ref deviceCount, devicesPtr );
		}

		foreach ( var device in devices )
		{
			if ( IsDeviceSuitable( device ) )
			{
				_physicalDevice = device;
				break;
			}
		}

		if ( _physicalDevice.Handle == 0 )
		{
			throw new Exception( "Failed to find a suitable GPU?" );
		}
	}

	private void InitDevice()
	{
		var indices = FindQueueFamilies( _physicalDevice );

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

		PhysicalDeviceFeatures deviceFeatures = new();
		DeviceCreateInfo createInfo = new()
		{
			SType = StructureType.DeviceCreateInfo,
			QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
			PQueueCreateInfos = queueCreateInfos,
			PEnabledFeatures = &deviceFeatures,

			EnabledExtensionCount = (uint)deviceExtensions.Length,
			PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr( deviceExtensions )
		};

		createInfo.EnabledLayerCount = (uint)_requiredValidationLayers.Length;
		createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr( _requiredValidationLayers );

		VkCheck( _vk!.CreateDevice( _physicalDevice, in createInfo, null, out _device ) );

		_vk!.GetDeviceQueue( _device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue );
		_vk!.GetDeviceQueue( _device, indices.PresentFamily!.Value, 0, out _presentQueue );

		SilkMarshal.Free( (nint)createInfo.PpEnabledLayerNames );
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
		createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;
	}

	private void SetupDebugMessenger()
	{
		if ( !_vk!.TryGetInstanceExtension( _instance, out _debugUtils ) ) return;

		DebugUtilsMessengerCreateInfoEXT createInfo = new();
		PopulateDebugMessengerCreateInfo( ref createInfo );

		VkCheck( _debugUtils!.CreateDebugUtilsMessenger( _instance, in createInfo, null, out _debugMessenger ) );
	}

	private string[] GetRequiredExtensions()
	{
		var glfwExtensions = _window!.VkSurface!.GetRequiredExtensions( out var glfwExtensionCount );
		var extensions = SilkMarshal.PtrToStringArray( (nint)glfwExtensions, (int)glfwExtensionCount );

		return extensions.Append( ExtDebugUtils.ExtensionName ).ToArray();
	}

	private bool CheckValidationLayerSupport()
	{
		uint layerCount = 0;
		_vk!.EnumerateInstanceLayerProperties( ref layerCount, null );
		var availableLayers = new LayerProperties[layerCount];
		fixed ( LayerProperties* availableLayersPtr = availableLayers )
		{
			_vk!.EnumerateInstanceLayerProperties( ref layerCount, availableLayersPtr );
		}

		var availableLayerNames = availableLayers.Select( layer => Marshal.PtrToStringAnsi( (IntPtr)layer.LayerName ) ).ToHashSet();

		return _requiredValidationLayers.All( availableLayerNames.Contains );
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

	private bool IsDeviceSuitable( PhysicalDevice device )
	{
		var indices = FindQueueFamilies( device );
		bool extensionsSupported = CheckDeviceExtensionsSupport( device );
		bool swapChainAdequate = false;

		if ( extensionsSupported )
		{
			var swapChainSupport = QuerySwapChainSupport( device );
			swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
		}

		return indices.IsComplete && extensionsSupported && swapChainAdequate;
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
		return deviceExtensions.All( availableExtensionNames.Contains );
	}

	private void InitSurface()
	{
		if ( !_vk!.TryGetInstanceExtension<KhrSurface>( _instance, out _khrSurface ) )
		{
			throw new NotSupportedException( "KHR_surface extension not found." );
		}

		_surface = _window!.VkSurface!.Create<AllocationCallbacks>( _instance.ToHandle(), null ).ToSurface();
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

			_khrSurface!.GetPhysicalDeviceSurfaceSupport( device, i, _surface, out var presentSupport );

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

	private void InitSwapchain()
	{
		var swapChainSupport = QuerySwapChainSupport( _physicalDevice );
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
			OldSwapchain = default
		};

		if ( !_vk!.TryGetDeviceExtension( _instance, _device, out _khrSwapchain ) )
		{
			throw new NotSupportedException( "VK_KHR_swapchain extension not found." );
		}

		VkCheck( _khrSwapchain!.CreateSwapchain( _device, createInfo, null, out _swapchain ) );

		_khrSwapchain.GetSwapchainImages( _device, _swapchain, ref imageCount, null );
		_swapchainImages = new Image[imageCount];

		fixed ( Image* swapChainImagesPtr = _swapchainImages )
		{
			_khrSwapchain.GetSwapchainImages( _device, _swapchain, ref imageCount, swapChainImagesPtr );
		}

		_swapchainImageFormat = surfaceFormat.Format;
		_swapchainExtent = extent;
	}

	private void InitImageViews()
	{
		_swapchainImageViews = new ImageView[_swapchainImages!.Length];

		for ( int i = 0; i < _swapchainImages.Length; i++ )
		{
			ImageViewCreateInfo createInfo = new()
			{
				SType = StructureType.ImageViewCreateInfo,
				Image = _swapchainImages[i],
				ViewType = ImageViewType.Type2D,
				Format = _swapchainImageFormat,
				Components =
				{
					R = ComponentSwizzle.Identity,
					G = ComponentSwizzle.Identity,
					B = ComponentSwizzle.Identity,
					A = ComponentSwizzle.Identity,
				},
				SubresourceRange =
				{
					AspectMask = ImageAspectFlags.ColorBit,
					BaseMipLevel = 0,
					LevelCount = 1,
					BaseArrayLayer = 0,
					LayerCount = 1,
				}
			};

			VkCheck( _vk!.CreateImageView( _device, createInfo, null, out _swapchainImageViews[i] ) );
		}
	}

	private void InitRenderPass()
	{
		AttachmentDescription colorAttachment = new()
		{
			Format = _swapchainImageFormat,
			Samples = SampleCountFlags.Count1Bit,
			LoadOp = AttachmentLoadOp.Clear,
			StoreOp = AttachmentStoreOp.Store,
			StencilLoadOp = AttachmentLoadOp.DontCare,
			InitialLayout = ImageLayout.Undefined,
			FinalLayout = ImageLayout.PresentSrcKhr,
		};

		AttachmentReference colorAttachmentRef = new()
		{
			Attachment = 0,
			Layout = ImageLayout.ColorAttachmentOptimal,
		};

		SubpassDescription subpass = new()
		{
			PipelineBindPoint = PipelineBindPoint.Graphics,
			ColorAttachmentCount = 1,
			PColorAttachments = &colorAttachmentRef,
		};

		SubpassDependency dependency = new()
		{
			SrcSubpass = Vk.SubpassExternal,
			DstSubpass = 0,
			SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
			SrcAccessMask = 0,
			DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
			DstAccessMask = AccessFlags.ColorAttachmentWriteBit
		};

		RenderPassCreateInfo renderPassInfo = new()
		{
			SType = StructureType.RenderPassCreateInfo,
			AttachmentCount = 1,
			PAttachments = &colorAttachment,
			SubpassCount = 1,
			PSubpasses = &subpass,
			DependencyCount = 1,
			PDependencies = &dependency,
		};

		VkCheck( _vk!.CreateRenderPass( _device, renderPassInfo, null, out _renderPass ) );
	}

	private void InitGraphicsPipeline()
	{
		var shaderSources = new VulkanShaderSources()
		{
			Vertex = "shaders/vert.glsl",
			Fragment = "shaders/frag.glsl"
		};

		var (vertexData, fragmentData) = VulkanShaderCompiler.Compile( shaderSources );

		var vertShaderModule = CreateShaderModule( vertexData );
		var fragShaderModule = CreateShaderModule( fragmentData );

		PipelineShaderStageCreateInfo vertShaderStageInfo = new()
		{
			SType = StructureType.PipelineShaderStageCreateInfo,
			Stage = ShaderStageFlags.VertexBit,
			Module = vertShaderModule,
			PName = (byte*)SilkMarshal.StringToPtr( "main" )
		};

		PipelineShaderStageCreateInfo fragShaderStageInfo = new()
		{
			SType = StructureType.PipelineShaderStageCreateInfo,
			Stage = ShaderStageFlags.FragmentBit,
			Module = fragShaderModule,
			PName = (byte*)SilkMarshal.StringToPtr( "main" )
		};

		var shaderStages = stackalloc[]
		{
			vertShaderStageInfo,
			fragShaderStageInfo
		};

		var bindingDescription = Vertex.GetBindingDescription();
		var attributeDescriptions = Vertex.GetAttributeDescriptions();

		fixed ( VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions )
		{
			PipelineVertexInputStateCreateInfo vertexInputInfo = new()
			{
				SType = StructureType.PipelineVertexInputStateCreateInfo,
				VertexBindingDescriptionCount = 1,
				VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
				PVertexBindingDescriptions = &bindingDescription,
				PVertexAttributeDescriptions = attributeDescriptionsPtr
			};

			PipelineInputAssemblyStateCreateInfo inputAssembly = new()
			{
				SType = StructureType.PipelineInputAssemblyStateCreateInfo,
				Topology = PrimitiveTopology.TriangleList,
				PrimitiveRestartEnable = false,
			};

			Viewport viewport = new()
			{
				X = 0,
				Y = 0,
				Width = _swapchainExtent.Width,
				Height = _swapchainExtent.Height,
				MinDepth = 0,
				MaxDepth = 1,
			};

			Rect2D scissor = new()
			{
				Offset = { X = 0, Y = 0 },
				Extent = _swapchainExtent,
			};

			PipelineViewportStateCreateInfo viewportState = new()
			{
				SType = StructureType.PipelineViewportStateCreateInfo,
				ViewportCount = 1,
				PViewports = &viewport,
				ScissorCount = 1,
				PScissors = &scissor,
			};

			PipelineRasterizationStateCreateInfo rasterizer = new()
			{
				SType = StructureType.PipelineRasterizationStateCreateInfo,
				DepthClampEnable = false,
				RasterizerDiscardEnable = false,
				PolygonMode = PolygonMode.Fill,
				LineWidth = 1,
				CullMode = CullModeFlags.BackBit,
				FrontFace = FrontFace.Clockwise,
				DepthBiasEnable = false,
			};

			PipelineMultisampleStateCreateInfo multisampling = new()
			{
				SType = StructureType.PipelineMultisampleStateCreateInfo,
				SampleShadingEnable = false,
				RasterizationSamples = SampleCountFlags.Count1Bit,
			};

			PipelineColorBlendAttachmentState colorBlendAttachment = new()
			{
				ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
				BlendEnable = false,
			};

			PipelineColorBlendStateCreateInfo colorBlending = new()
			{
				SType = StructureType.PipelineColorBlendStateCreateInfo,
				LogicOpEnable = false,
				LogicOp = LogicOp.Copy,
				AttachmentCount = 1,
				PAttachments = &colorBlendAttachment,
			};

			colorBlending.BlendConstants[0] = 0;
			colorBlending.BlendConstants[1] = 0;
			colorBlending.BlendConstants[2] = 0;
			colorBlending.BlendConstants[3] = 0;

			PipelineLayoutCreateInfo pipelineLayoutInfo = new()
			{
				SType = StructureType.PipelineLayoutCreateInfo,
				SetLayoutCount = 0,
				PushConstantRangeCount = 0,
			};

			VkCheck( _vk!.CreatePipelineLayout( _device, pipelineLayoutInfo, null, out _pipelineLayout ) );

			GraphicsPipelineCreateInfo pipelineInfo = new()
			{
				SType = StructureType.GraphicsPipelineCreateInfo,
				StageCount = 2,
				PStages = shaderStages,
				PVertexInputState = &vertexInputInfo,
				PInputAssemblyState = &inputAssembly,
				PViewportState = &viewportState,
				PRasterizationState = &rasterizer,
				PMultisampleState = &multisampling,
				PColorBlendState = &colorBlending,
				Layout = _pipelineLayout,
				RenderPass = _renderPass,
				Subpass = 0,
				BasePipelineHandle = default
			};

			VkCheck( _vk!.CreateGraphicsPipelines( _device, default, 1, pipelineInfo, null, out _graphicsPipeline ) );
		}

		_vk!.DestroyShaderModule( _device, fragShaderModule, null );
		_vk!.DestroyShaderModule( _device, vertShaderModule, null );

		SilkMarshal.Free( (nint)vertShaderStageInfo.PName );
		SilkMarshal.Free( (nint)fragShaderStageInfo.PName );
	}

	private void InitFramebuffers()
	{
		_swapchainFramebuffers = new Framebuffer[_swapchainImageViews!.Length];

		for ( int i = 0; i < _swapchainImageViews.Length; i++ )
		{
			var attachment = _swapchainImageViews[i];

			FramebufferCreateInfo framebufferInfo = new()
			{
				SType = StructureType.FramebufferCreateInfo,
				RenderPass = _renderPass,
				AttachmentCount = 1,
				PAttachments = &attachment,
				Width = _swapchainExtent.Width,
				Height = _swapchainExtent.Height,
				Layers = 1,
			};

			VkCheck( _vk!.CreateFramebuffer( _device, framebufferInfo, null, out _swapchainFramebuffers[i] ) );
		}
	}

	private void InitCommandPool()
	{
		var queueFamiliyIndices = FindQueueFamilies( _physicalDevice );

		CommandPoolCreateInfo poolInfo = new()
		{
			SType = StructureType.CommandPoolCreateInfo,
			QueueFamilyIndex = queueFamiliyIndices.GraphicsFamily!.Value,
		};

		VkCheck( _vk!.CreateCommandPool( _device, poolInfo, null, out _commandPool ) );
	}

	private void InitVertexBuffer()
	{
		BufferCreateInfo bufferInfo = new()
		{
			SType = StructureType.BufferCreateInfo,
			Size = (ulong)(sizeof( Vertex ) * _vertices.Length),
			Usage = BufferUsageFlags.VertexBufferBit,
			SharingMode = SharingMode.Exclusive,
		};

		fixed ( Buffer* vertexBufferPtr = &_vertexBuffer )
		{
			if ( _vk!.CreateBuffer( _device, bufferInfo, null, vertexBufferPtr ) != Result.Success )
			{
				throw new Exception( "failed to create vertex buffer!" );
			}
		}

		MemoryRequirements memRequirements = new();
		_vk!.GetBufferMemoryRequirements( _device, _vertexBuffer, out memRequirements );

		MemoryAllocateInfo allocateInfo = new()
		{
			SType = StructureType.MemoryAllocateInfo,
			AllocationSize = memRequirements.Size,
			MemoryTypeIndex = FindMemoryType( memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit ),
		};

		fixed ( DeviceMemory* vertexBufferMemoryPtr = &_vertexBufferMemory )
		{
			if ( _vk!.AllocateMemory( _device, allocateInfo, null, vertexBufferMemoryPtr ) != Result.Success )
			{
				throw new Exception( "failed to allocate vertex buffer memory!" );
			}
		}

		_vk!.BindBufferMemory( _device, _vertexBuffer, _vertexBufferMemory, 0 );

		void* data;
		_vk!.MapMemory( _device, _vertexBufferMemory, 0, bufferInfo.Size, 0, &data );
		_vertices.AsSpan().CopyTo( new Span<Vertex>( data, _vertices.Length ) );
		_vk!.UnmapMemory( _device, _vertexBufferMemory );
	}

	private void InitIndexBuffer()
	{
		BufferCreateInfo bufferInfo = new()
		{
			SType = StructureType.BufferCreateInfo,
			Size = (ulong)(sizeof( uint ) * _indices.Length),
			Usage = BufferUsageFlags.IndexBufferBit,
			SharingMode = SharingMode.Exclusive,
		};

		fixed ( Buffer* indexBufferPtr = &_indexBuffer )
		{
			if ( _vk!.CreateBuffer( _device, bufferInfo, null, indexBufferPtr ) != Result.Success )
			{
				throw new Exception( "failed to create index buffer!" );
			}
		}

		MemoryRequirements memRequirements = new();
		_vk!.GetBufferMemoryRequirements( _device, _indexBuffer, out memRequirements );

		MemoryAllocateInfo allocateInfo = new()
		{
			SType = StructureType.MemoryAllocateInfo,
			AllocationSize = memRequirements.Size,
			MemoryTypeIndex = FindMemoryType( memRequirements.MemoryTypeBits, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit ),
		};

		fixed ( DeviceMemory* indexBufferMemoryPtr = &_indexBufferMemory )
		{
			if ( _vk!.AllocateMemory( _device, allocateInfo, null, indexBufferMemoryPtr ) != Result.Success )
			{
				throw new Exception( "failed to allocate index buffer memory!" );
			}
		}

		_vk!.BindBufferMemory( _device, _indexBuffer, _indexBufferMemory, 0 );

		void* data;
		_vk!.MapMemory( _device, _indexBufferMemory, 0, bufferInfo.Size, 0, &data );
		_indices.AsSpan().CopyTo( new Span<uint>( data, _indices.Length ) );
		_vk!.UnmapMemory( _device, _indexBufferMemory );
	}

	private uint FindMemoryType( uint typeFilter, MemoryPropertyFlags properties )
	{
		_vk!.GetPhysicalDeviceMemoryProperties( _physicalDevice, out PhysicalDeviceMemoryProperties memProperties );
		for ( int i = 0; i < memProperties.MemoryTypeCount; i++ )
		{
			if ( (typeFilter & (1 << i)) != 0 && (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties )
			{
				return (uint)i;
			}
		}
		throw new Exception( "failed to find suitable memory type!" );
	}

	private void InitCommandBuffers()
	{
		_commandBuffers = new CommandBuffer[_swapchainFramebuffers!.Length];

		CommandBufferAllocateInfo allocInfo = new()
		{
			SType = StructureType.CommandBufferAllocateInfo,
			CommandPool = _commandPool,
			Level = CommandBufferLevel.Primary,
			CommandBufferCount = (uint)_commandBuffers.Length,
		};

		fixed ( CommandBuffer* commandBuffersPtr = _commandBuffers )
		{
			VkCheck( _vk!.AllocateCommandBuffers( _device, allocInfo, commandBuffersPtr ) );
		}

		for ( int i = 0; i < _commandBuffers.Length; i++ )
		{
			CommandBufferBeginInfo beginInfo = new()
			{
				SType = StructureType.CommandBufferBeginInfo,
			};

			VkCheck( _vk!.BeginCommandBuffer( _commandBuffers[i], beginInfo ) );

			RenderPassBeginInfo renderPassInfo = new()
			{
				SType = StructureType.RenderPassBeginInfo,
				RenderPass = _renderPass,
				Framebuffer = _swapchainFramebuffers[i],
				RenderArea =
				{
					Offset = { X = 0, Y = 0 },
					Extent = _swapchainExtent,
				}
			};

			ClearValue clearColor = new()
			{
				Color = new() { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
			};

			renderPassInfo.ClearValueCount = 1;
			renderPassInfo.PClearValues = &clearColor;

			_vk!.CmdBeginRenderPass( _commandBuffers[i], &renderPassInfo, SubpassContents.Inline );

			_vk!.CmdBindPipeline( _commandBuffers[i], PipelineBindPoint.Graphics, _graphicsPipeline );

			var vertexBuffers = new Buffer[] { _vertexBuffer };
			var offsets = new ulong[] { 0 };

			fixed ( ulong* offsetsPtr = offsets )
			fixed ( Buffer* vertexBuffersPtr = vertexBuffers )
			{
				_vk!.CmdBindVertexBuffers( _commandBuffers[i], 0, 1, vertexBuffersPtr, offsetsPtr );
			}

			_vk!.CmdBindIndexBuffer( _commandBuffers[i], _indexBuffer, 0, IndexType.Uint32 );
			_vk!.CmdDrawIndexed( _commandBuffers[i], (uint)_indices.Length, 1, 0, 0, 0 );

			_vk!.CmdEndRenderPass( _commandBuffers[i] );

			VkCheck( _vk!.EndCommandBuffer( _commandBuffers[i] ) );
		}
	}

	private void InitSyncObjects()
	{
		_imageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
		_renderFinishedSemaphores = new Semaphore[MaxFramesInFlight];
		_inFlightFences = new Fence[MaxFramesInFlight];
		_imagesInFlight = new Fence[_swapchainImages!.Length];

		SemaphoreCreateInfo semaphoreInfo = new()
		{
			SType = StructureType.SemaphoreCreateInfo,
		};

		FenceCreateInfo fenceInfo = new()
		{
			SType = StructureType.FenceCreateInfo,
			Flags = FenceCreateFlags.SignaledBit,
		};

		for ( var i = 0; i < MaxFramesInFlight; i++ )
		{
			VkCheck( _vk!.CreateSemaphore( _device, semaphoreInfo, null, out _imageAvailableSemaphores[i] ) );
			VkCheck( _vk!.CreateSemaphore( _device, semaphoreInfo, null, out _renderFinishedSemaphores[i] ) );
			VkCheck( _vk!.CreateFence( _device, fenceInfo, null, out _inFlightFences[i] ) );
		}
	}

	private void DrawFrame( double delta )
	{
		_vk!.WaitForFences( _device, 1, _inFlightFences![_currentFrame], true, ulong.MaxValue );

		uint imageIndex = 0;
		_khrSwapchain!.AcquireNextImage( _device, _swapchain, ulong.MaxValue, _imageAvailableSemaphores![_currentFrame], default, ref imageIndex );

		if ( _imagesInFlight![imageIndex].Handle != default )
		{
			_vk!.WaitForFences( _device, 1, _imagesInFlight[imageIndex], true, ulong.MaxValue );
		}
		_imagesInFlight[imageIndex] = _inFlightFences[_currentFrame];

		SubmitInfo submitInfo = new()
		{
			SType = StructureType.SubmitInfo,
		};

		var waitSemaphores = stackalloc[] { _imageAvailableSemaphores[_currentFrame] };
		var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

		var buffer = _commandBuffers![imageIndex];

		submitInfo = submitInfo with
		{
			WaitSemaphoreCount = 1,
			PWaitSemaphores = waitSemaphores,
			PWaitDstStageMask = waitStages,

			CommandBufferCount = 1,
			PCommandBuffers = &buffer
		};

		var signalSemaphores = stackalloc[] { _renderFinishedSemaphores![_currentFrame] };
		submitInfo = submitInfo with
		{
			SignalSemaphoreCount = 1,
			PSignalSemaphores = signalSemaphores,
		};

		_vk!.ResetFences( _device, 1, _inFlightFences[_currentFrame] );

		VkCheck( _vk!.QueueSubmit( _graphicsQueue, 1, submitInfo, _inFlightFences[_currentFrame] ) );

		var swapChains = stackalloc[] { _swapchain };
		PresentInfoKHR presentInfo = new()
		{
			SType = StructureType.PresentInfoKhr,

			WaitSemaphoreCount = 1,
			PWaitSemaphores = signalSemaphores,

			SwapchainCount = 1,
			PSwapchains = swapChains,

			PImageIndices = &imageIndex
		};

		VkCheck( _khrSwapchain.QueuePresent( _presentQueue, presentInfo ) );

		_currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
	}

	private ShaderModule CreateShaderModule( byte[] code )
	{
		ShaderModuleCreateInfo createInfo = new()
		{
			SType = StructureType.ShaderModuleCreateInfo,
			CodeSize = (nuint)code.Length,
		};

		ShaderModule shaderModule;

		fixed ( byte* codePtr = code )
		{
			createInfo.PCode = (uint*)codePtr;

			VkCheck( _vk!.CreateShaderModule( _device, createInfo, null, out shaderModule ) );
		}

		return shaderModule;
	}

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
			var framebufferSize = _window!.FramebufferSize!.Value;

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

	public void DrawRect( RectangleF rect )
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		_vk!.DestroyDevice( _device, null );

		// _debugUtils!.DestroyDebugUtilsMessenger( _instance, _debugMessenger, null );
		_khrSurface!.DestroySurface( _instance, _surface, null );

		_vk!.DestroyInstance( _instance, null );
		_vk!.Dispose();

		Log.Trace( "Cleaned up Vulkan" );
	}
}
