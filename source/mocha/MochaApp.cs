using Mocha.Rendering;
using Mocha.Rendering.Vulkan;

namespace Mocha;

public class MochaApp
{
	private int _width = 1280;
	private int _height = 720;
	private string _name = "My Mocha App";

	public MochaApp( string name )
	{
		_name = name;
	}

	public void Run()
	{
		var platformInfo = CurrentPlatformInfo.Current();

		using var window = new Window( platformInfo, _name, _width, _height );
		using var renderer = new VulkanBackend( window );

		window.Run();
	}
}
