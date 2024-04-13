using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VMASharp;

namespace Mocha.Rendering.Vulkan;

internal unsafe partial class VulkanRenderContext : IRenderContext
{
	public bool HasInitialized { get; private set; }
	public bool RenderingActive { get; private set; }

	private Window _window = null!;

	[WithProperty]
	private Vk _vk = null!;

	[WithProperty]
	private SurfaceKHR _surface;

	private KhrSurface _surfaceExtension = null!;

	[WithProperty]
	private PhysicalDevice _chosenGPU;

	[WithProperty]
	private Device _device;

	private Queue _graphicsQueue;
	private Queue _presentQueue;

	private Instance _instance;

	[WithProperty]
	private VulkanMemoryAllocator _allocator;

#if DEBUG
	string[] _requiredValidationLayers = ["VK_LAYER_KHRONOS_validation"];
#else
	string[] _requiredValidationLayers = [];
#endif

	string[] _deviceExtensions = [KhrSwapchain.ExtensionName];

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

	public RenderStatus BeginRendering()
	{
		if ( RenderingActive )
		{
			Log.Error( $"{nameof( BeginRendering )} called more than once before {nameof( EndRendering )}!" );
			return RenderStatus.BeginEndMismatch;
		}

		RenderingActive = true;

		return RenderStatus.Ok;
	}

	public RenderStatus EndRendering()
	{
		if ( !RenderingActive )
		{
			Log.Error( $"{nameof( EndRendering )} called more than once before {nameof( BeginRendering )}!" );
			return RenderStatus.BeginEndMismatch;
		}

		RenderingActive = false;

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

		var extensions = _window!.VkSurface!.GetRequiredExtensions( out var extensionCount );

		createInfo.EnabledExtensionCount = extensionCount;
		createInfo.PpEnabledExtensionNames = extensions;
		createInfo.EnabledLayerCount = (uint)_requiredValidationLayers.Length;
		createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToMemory( _requiredValidationLayers );

		DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
		PopulateDebugMessengerCreateInfo( ref debugCreateInfo );
		createInfo.PNext = &debugCreateInfo;

		VkCheck( _vk!.CreateInstance( createInfo, null, out var instance ) );

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
			details.Formats = Array.Empty<SurfaceFormatKHR>();
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
			details.PresentModes = Array.Empty<PresentModeKHR>();
		}

		return details;
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
			if ( IsDeviceSuitable( device ) )
			{
				physicalDevice = device;
				break;
			}
		}

		if ( physicalDevice?.Handle == 0 )
		{
			throw new Exception( "Failed to find a suitable GPU?" );
		}

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

		PhysicalDeviceFeatures deviceFeatures = new();
		DeviceCreateInfo createInfo = new()
		{
			SType = StructureType.DeviceCreateInfo,
			QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
			PQueueCreateInfos = queueCreateInfos,
			PEnabledFeatures = &deviceFeatures,

			EnabledExtensionCount = (uint)_deviceExtensions.Length,
			PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr( _deviceExtensions )
		};

		createInfo.EnabledLayerCount = (uint)_requiredValidationLayers.Length;
		createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr( _requiredValidationLayers );

		VkCheck( _vk!.CreateDevice( physicalDevice, in createInfo, null, out _device ) );

		_vk!.GetDeviceQueue( _device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue );
		_vk!.GetDeviceQueue( _device, indices.PresentFamily!.Value, 0, out _presentQueue );

		SilkMarshal.Free( (nint)createInfo.PpEnabledLayerNames );
	}

	private void CreateAllocator()
	{
		var allocatorCreateInfo = new VulkanMemoryAllocatorCreateInfo()
		{
			LogicalDevice = _device,
			Instance = _instance,
			VulkanAPIObject = _vk
		};

		_allocator = new VulkanMemoryAllocator( allocatorCreateInfo );
	}

	public RenderStatus Startup( Window window )
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

		return RenderStatus.Ok;
	}

	public void ImmediateSubmit( Func<CommandBuffer, RenderStatus> func )
	{

	}
}
