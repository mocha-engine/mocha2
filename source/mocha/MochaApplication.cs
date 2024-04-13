using Mocha.Rendering;
using Mocha.Rendering.Vulkan;
using Veldrid;

namespace Mocha;

public class MochaApplication : IDisposable
{
	private int _width = 1280;
	private int _height = 720;
	private string _name = "My Mocha App";

	public string Icon;

	public Action? PreBootstrap;
	public Action? Bootstrap;

	[Obsolete( "Currently unused" )]
	public Action? MainLoop;

	[Obsolete( "Currently unused" )]
	public Action? Shutdown;

	public MochaApplication( string name )
	{
		_name = name;
	}

	public void Dispose()
	{
		Run();
	}

	private void Run()
	{
		//if ( !Veldrid.RenderDoc.Load( "./renderdoc.dll", out var renderDoc ) )
		//{
		//	Log.Error( "Failed to initialize renderdoc" );
		//}

		PreBootstrap?.Invoke();

		var platformInfo = CurrentPlatformInfo.Current();

		using var window = new Window( platformInfo, _name, _width, _height, Icon );
		using var renderer = new VulkanBackend( window, null );

		// Set up resource factory
		Factory = new ResourceFactory<VulkanBackend>();

		Bootstrap?.Invoke();

		window.Run();
	}
}
