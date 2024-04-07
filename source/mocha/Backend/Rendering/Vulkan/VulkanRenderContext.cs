namespace Mocha.Rendering.Vulkan;

internal unsafe class VulkanRenderContext : IRenderContext
{
	public bool HasInitialized { get; private set; }
	public bool RenderingActive { get; private set; }

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

	public RenderStatus Startup()
	{
		if ( HasInitialized )
		{
			Log.Error( $"Can't start up if {nameof( Startup )} was already called!" );
			return RenderStatus.AlreadyInitialized;
		}
		
		HasInitialized = true;
		
		return RenderStatus.Ok;
	}
}
