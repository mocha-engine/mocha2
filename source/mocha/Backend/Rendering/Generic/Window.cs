using Mocha.Platforms;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Mocha.Rendering;

internal unsafe class Window : IDisposable
{
	[DllImport( "user32.dll", SetLastError = true )]
	private static extern nint FindWindow( string lpClassName, string lpWindowName );
	
	private string _title = "My Window";
	
	private readonly int _width = 1280;
	private readonly int _height = 720;
	private IWindow? _window;

	internal IVkSurface? VkSurface => _window?.VkSurface;
	internal Vector2Int? FramebufferSize => _window?.FramebufferSize;
	internal Action<double>? Render;
	internal nint NativeHandle => Process.GetCurrentProcess().MainWindowHandle; // what the FUCK
	internal string Title => _window?.Title ?? "No title";
	
	public event Action? OnResize;

	internal Window( CurrentPlatformInfo platform, string title, int width, int height, string? icon = null )
	{
		_title = title;
		_width = width;
		_height = height;

		InitWindow( platform );

		if ( icon != null )
		{
			using var image = Image.Load<Rgba32>( icon );
			var memoryGroup = image.GetPixelMemoryGroup();
			
			Memory<byte> array = new byte[memoryGroup.TotalLength * sizeof( Rgba32 )];
			var block = MemoryMarshal.Cast<byte, Rgba32>( array.Span );

			foreach ( var memory in memoryGroup )
			{
				memory.Span.CopyTo( block );
				block = block[memory.Length..];
			}

			var iconImage = new RawImage( image.Width, image.Height, array );
			_window!.SetWindowIcon( new ReadOnlySpan<RawImage>( iconImage ) );
		}

		_window!.Render += OnRender;

		PerformanceStats.OnAverageCalculated += () =>
		{
			_window!.Title = $"{_title} - {PerformanceStats.AverageFPS} FPS ({(PerformanceStats.AverageDelta * 1000d):F3}ms)";
		};
	}

	internal void OnRendererInit()
	{
		// Hide window until renderer is ready, gives the user the impression
		// that there's zero waiting time - looks snappier
		_window!.IsVisible = true;
	}

	private float t = 0;
	
	private void OnRender( double dt )
	{
		Render?.Invoke( dt );

		Time.OnFrame( (float)dt );
		PerformanceStats.OnFrame( dt );

		if ( t >= 1f )
		{
			Log.Info( "Tick" );
			t = 0;
		}
	}

	internal void Run()
	{
		MainLoop();
	}

	private void PlatformSpecificInit( CurrentPlatformInfo platform )
	{
		if ( platform.OperatingSystem == OperatingSystems.Windows )
			Windows.InitWindow( NativeHandle );
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
		_title = GetWindowTitle( platform );
		
		var options = GetWindowOptions( platform ) with
		{
			Size = new Vector2Int( _width, _height ),
			Title = _title,
			IsVisible = false
		};

		_window = Silk.NET.Windowing.Window.Create( options );
		_window.Initialize();

		_window.Resize += _ =>
		{
			OnResize?.Invoke();
		};

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
