using Apparatus.Core.Rendering;

namespace Mocha;

public class MochaApplication : IDisposable
{
	private int _width = 1920;
	private int _height = 1080;

	protected string Name = "My Mocha App";
	public string Icon = "";

	public void Dispose()
	{
		Run();
	}

	public void Run()
	{
		Environment.GetCommandLineArgs().ToList().ForEach( arg =>
		{
			if ( arg == "--renderdoc" )
			{
				if ( !Veldrid.RenderDoc.Load( out var renderDoc ) )
				{
					Log.Error( "Failed to initialize renderdoc" );
				}
			}
		} );

		OnPreBootstrap();

		var platformInfo = CurrentPlatformInfo.Current();
		IRenderContext.CreateVulkanRenderContext();

		using var window = new Window( platformInfo, Name, _width, _height );
		Render.Startup( window );

		OnBootstrap();

		window.Render += ( double dt ) =>
		{
			OnRender();
		};

		window.Run();
		Render.Shutdown();

		OnShutdown();
	}

	protected virtual void OnRender() { }
	protected virtual void OnPreBootstrap() { }
	protected virtual void OnBootstrap() { }
	protected virtual void OnShutdown() { }
}
