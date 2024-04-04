using Mocha.Platforms;
using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;

namespace Mocha.Rendering;

internal unsafe class Window : IDisposable
{
	private readonly int _width = 1280;
	private readonly int _height = 720;
	private readonly string _title = "My Window";
	private IWindow? _window;

	internal IVkSurface? VkSurface => _window?.VkSurface;
	internal Vector2Int? FramebufferSize => _window?.FramebufferSize;
	internal Action<double>? Render;
	internal nint NativeHandle => _window?.Handle ?? nint.Zero;
	internal string Title => _window?.Title ?? "No title";

	internal Window( CurrentPlatformInfo platform, string title, int width, int height )
	{
		_title = title;
		_width = width;
		_height = height;

		InitWindow( platform );

		_window!.Render += OnRender;
	}

	internal void OnRendererInit()
	{
		// Hide window until renderer is ready, gives the user the impression
		// that there's zero waiting time - looks snappier
		_window!.IsVisible = true;
	}

	private void OnRender( double dt )
	{
		Render?.Invoke( dt );
	}

	internal void Run()
	{
		MainLoop();
	}

	private void PlatformSpecificInit( CurrentPlatformInfo platform )
	{
		if ( platform.OperatingSystem == OperatingSystems.Windows )
			Windows.InitWindow( this );
	}

	private WindowOptions GetWindowOptions( CurrentPlatformInfo platform )
	{
		if ( platform.Renderer == Renderers.Vulkan )
		{
			return WindowOptions.DefaultVulkan;
		}

		return WindowOptions.Default;
	}

	private string GetWindowTitle( CurrentPlatformInfo platform )
	{
#if DEBUG
		return $"{_title} (DEBUG) - {platform.Renderer}, {platform.OperatingSystem}";
#else
		return _title;
#endif
	}

	private void InitWindow( CurrentPlatformInfo platform )
	{
		var options = GetWindowOptions( platform ) with
		{
			Size = new Vector2Int( _width, _height ),
			Title = GetWindowTitle( platform ),
			IsVisible = false
		};

		_window = Silk.NET.Windowing.Window.Create( options );
		_window.Initialize();

		PlatformSpecificInit( platform );
	}

	private void MainLoop()
	{
		_window!.Run();
	}

	public void Dispose()
	{
		_window?.Dispose();
	}
}
